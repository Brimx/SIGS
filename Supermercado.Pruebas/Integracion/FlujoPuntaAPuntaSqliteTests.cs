using Microsoft.EntityFrameworkCore;
using Supermercado.Datos;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;

namespace Supermercado.Pruebas.Integracion;

public class FlujoPuntaAPuntaSqliteTests
{
    [Fact]
    public async Task FlujoCompleto_InicializarTurnoVentaYAnulacion()
    {
        using var contexto = new ContextoPruebasSqlite();

        // 1. Inicializar base de datos y sembrar
        var inicializador = new InicializadorBaseDatos(contexto.Db);
        await inicializador.CrearYSembrarAsync();

        var rolAdmin = await contexto.Db.Roles
            .FirstAsync(r => r.Nombre == NombresRol.Admin);
        var cargoAdmin = await contexto.Db.Cargos
            .FirstAsync(c => c.Nombre == NombresRol.Admin);
        var cajaSembrada = await contexto.Db.Cajas.FirstAsync();
        var impuestoIva = await contexto.Db.Impuestos.FirstAsync();

        // 2. Crear admin y usuario
        var empleado = new Empleado
        {
            Nombres = "Administrador",
            Apellidos = "Principal",
            Documento = "10000001",
            Correo = "admin@supermercado.local",
            RolId = rolAdmin.Id,
            CargoId = cargoAdmin.Id,
            Activo = true
        };
        contexto.Db.Empleados.Add(empleado);
        await contexto.Db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "admin",
            HashClave = "hash-seguro",
            EmpleadoId = empleado.Id,
            Empleado = empleado
        };
        contexto.Db.Usuarios.Add(usuario);
        await contexto.Db.SaveChangesAsync();

        // 3. Iniciar sesión
        var sesion = new SesionActual();
        sesion.Iniciar(usuario);

        // 4. Instanciar servicios de dominio con repositorios EF Core
        var trabajo = new UnidadDeTrabajo(contexto.Db);
        var auditoriaRepo = new Repositorio<RegistroAuditoria>(contexto.Db);
        var servicioAuditoria = new ServicioAuditoria(
            auditoriaRepo, sesion, contexto.Reloj);

        var paramRepo = new ParametroRepositorio(contexto.Db);
        var servicioParametros = new ServicioParametros(
            paramRepo, servicioAuditoria, sesion, trabajo);

        var estanteRepo = new StockRepositorio<StockEstante>(contexto.Db);
        var bodegaRepo = new StockRepositorio<StockBodega>(contexto.Db);
        var movRepo = new MovimientoRepositorio(contexto.Db);
        var mermaRepo = new Repositorio<Merma>(contexto.Db);
        var estrategia = new SalidaFefo();
        var servicioInventario = new ServicioInventario(
            estanteRepo,
            bodegaRepo,
            movRepo,
            mermaRepo,
            estrategia,
            contexto.Reloj);

        var productoRepo = new ProductoRepositorio(contexto.Db);
        var loteRepo = new LoteRepositorio(contexto.Db);
        var servicioProductos = new ServicioProductos(
            productoRepo,
            loteRepo,
            servicioInventario,
            new ConsultaCodigoBarrasNula(),
            sesion,
            trabajo,
            contexto.Reloj);

        var turnoRepo = new TurnoCajaRepositorio(contexto.Db);
        var ventaRepo = new VentaRepositorio(contexto.Db);
        var cajaRepo = new Repositorio<Caja>(contexto.Db);
        var servicioCaja = new ServicioCaja(
            turnoRepo,
            ventaRepo,
            cajaRepo,
            servicioParametros,
            servicioAuditoria,
            sesion,
            trabajo,
            contexto.Reloj);

        var servicioVentas = new ServicioVentas(
            productoRepo,
            ventaRepo,
            turnoRepo,
            servicioInventario,
            servicioAuditoria,
            sesion,
            trabajo,
            contexto.Reloj);

        // 5. Abrir turno
        var turno = await servicioCaja.AbrirTurnoAsync(
            cajaSembrada.Id,
            efectivoInicial: 100_000);
        Assert.Equal(EstadoTurno.Abierto, turno.Estado);

        // 6. Registrar producto y lote
        var categoria = new Categoria { Nombre = "Granos" };
        var proveedor = new Proveedor
        {
            Nombre = "Distribuidora Diana",
            Nit = "800111222",
            Telefono = "3100000000"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);
        await contexto.Db.SaveChangesAsync();

        var producto = new ProductoUnitario
        {
            Nombre = "Arroz Diana 1kg",
            CodigoBarras = "7701234567890",
            PrecioVenta = 4_500,
            StockMinimoEstante = 10,
            ImpuestoId = impuestoIva.Id,
            CategoriaId = categoria.Id,
            ProveedorId = proveedor.Id
        };
        await servicioProductos.RegistrarAsync(producto);

        var vencimiento = new DateOnly(2027, 6, 30);
        await servicioProductos.RegistrarIngresoLoteAsync(
            producto.Id,
            numeroLote: "LOTE-2026-X",
            vencimiento: vencimiento,
            cantidad: 50,
            costoUnitario: 3_200);

        // Trasladar 20 unidades de bodega a estante
        await servicioInventario.TrasladarAEstanteAsync(producto.Id, 20);
        await trabajo.GuardarAsync();

        var totalesEstanteAntes = await estanteRepo
            .ObtenerTotalesPorProductoAsync();
        Assert.Equal(20, totalesEstanteAntes[producto.Id]);

        // 7. Vender 3 unidades
        var solicitud = new SolicitudVenta(
            [new LineaVenta("7701234567890", 3)],
            [new PagoEfectivo { Monto = 13_500 }]);

        var venta = await servicioVentas.RegistrarVentaAsync(solicitud);
        Assert.Equal(EstadoVenta.Completada, venta.Estado);

        var totalesEstanteDespuesVenta = await estanteRepo
            .ObtenerTotalesPorProductoAsync();
        Assert.Equal(17, totalesEstanteDespuesVenta[producto.Id]);

        // 8. Anular venta
        await servicioVentas.AnularVentaAsync(
            venta.Id,
            "Devolución solicitada por cliente");

        var ventaAnulada = await ventaRepo.ObtenerPorIdAsync(venta.Id);
        Assert.NotNull(ventaAnulada);
        Assert.Equal(EstadoVenta.Anulada, ventaAnulada.Estado);

        var totalesEstanteReintegrado = await estanteRepo
            .ObtenerTotalesPorProductoAsync();
        Assert.Equal(20, totalesEstanteReintegrado[producto.Id]);

        // 9. Comprobar auditoría
        var registrosAuditoria = await contexto.Db.Auditoria
            .Where(a => a.Accion == AccionAuditoria.VentaAnulada)
            .ToListAsync();
        Assert.Single(registrosAuditoria);
    }
}
