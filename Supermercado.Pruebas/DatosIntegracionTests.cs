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
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Finanzas;
using Supermercado.Logica.Comunes;
using Microsoft.Data.Sqlite;

namespace Supermercado.Pruebas.Integracion;

public class FlujoPuntaAPuntaSqliteTests
{
    [Fact]
    public async Task GeneradorDatosDemo_ContraSqlite_GeneraCorrectamente()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);
        await inicializador.CrearYSembrarAsync();

        var demo = new Supermercado.Logica.DatosDemo.GeneradorDatosDemo(
            new CategoriaRepositorio(contexto.Db),
            new ProveedorRepositorio(contexto.Db),
            new ImpuestoRepositorio(contexto.Db),
            new ProductoRepositorio(contexto.Db),
            new LoteRepositorio(contexto.Db),
            new StockEstanteRepositorio(contexto.Db),
            new StockBodegaRepositorio(contexto.Db),
            new MovimientoRepositorio(contexto.Db),
            new CargoRepositorio(contexto.Db),
            new RolRepositorio(contexto.Db),
            new EmpleadoRepositorio(contexto.Db),
            new UsuarioRepositorio(contexto.Db),
            new CajaRepositorio(contexto.Db),
            new TurnoCajaRepositorio(contexto.Db),
            new VentaRepositorio(contexto.Db),
            new GastoRepositorio(contexto.Db),
            new HasheadorFalso(),
            contexto.Reloj);

        await demo.GenerarAsync();

        var auth = new ServicioAutenticacion(
            new UsuarioRepositorio(contexto.Db),
            new IntentoAccesoRepositorio(contexto.Db),
            new AuditoriaFalsa(),
            new ParametrosFalsos(),
            new HasheadorFalso(),
            new SesionActual(),
            contexto.Reloj);

        await auth.IniciarSesionAsync("cajero", "Cajero123*");
        await auth.IniciarSesionAsync("admin", "Admin123*");
        await auth.IniciarSesionAsync("gerente", "Gerente123*");
        await auth.IniciarSesionAsync("bodega", "Bodega123*");
    }

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
        var auditoriaRepo = new RegistroAuditoriaRepositorio(contexto.Db);
        var servicioAuditoria = new ServicioAuditoria(
            auditoriaRepo, sesion, contexto.Reloj);

        var paramRepo = new ParametroRepositorio(contexto.Db);
        var servicioParametros = new ServicioParametros(
            paramRepo, servicioAuditoria, sesion);

        var estanteRepo = new StockEstanteRepositorio(contexto.Db);
        var bodegaRepo = new StockBodegaRepositorio(contexto.Db);
        var movRepo = new MovimientoRepositorio(contexto.Db);
        var mermaRepo = new MermaRepositorio(contexto.Db);
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
            contexto.Reloj);

        var turnoRepo = new TurnoCajaRepositorio(contexto.Db);
        var ventaRepo = new VentaRepositorio(contexto.Db);
        var cajaRepo = new CajaRepositorio(contexto.Db);
        var servicioCaja = new ServicioCaja(
            turnoRepo,
            ventaRepo,
            cajaRepo,
            servicioParametros,
            servicioAuditoria,
            sesion,
            contexto.Reloj);

        var servicioVentas = new ServicioVentas(
            productoRepo,
            ventaRepo,
            turnoRepo,
            servicioInventario,
            new ClienteRepositorio(contexto.Db),
            servicioParametros,
            servicioAuditoria,
            sesion,
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
        await contexto.Db.SaveChangesAsync();

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

public class EventoOutboxSqliteTests
{
    [Fact]
    public async Task Venta_ConClaveIdempotencia_SeGuardaYRecupera()
    {
        using var contexto = new ContextoPruebasSqlite();
        await new Supermercado.Datos.InicializadorBaseDatos(contexto.Db).CrearYSembrarAsync();
        var caja = await contexto.Db.Cajas.FirstAsync();
        var rol = await contexto.Db.Roles.FirstAsync();
        var cargo = await contexto.Db.Cargos.FirstAsync();

        var empleado = new Empleado
        {
            Nombres = "Cajero",
            Apellidos = "Uno",
            Documento = "987654321",
            RolId = rol.Id,
            CargoId = cargo.Id
        };
        contexto.Db.Empleados.Add(empleado);
        await contexto.Db.SaveChangesAsync();

        var turno = TurnoCaja.Abrir(caja.Id, empleado.Id, 50000, DateTime.UtcNow);
        contexto.Db.Turnos.Add(turno);
        await contexto.Db.SaveChangesAsync();

        var ventaRepo = new VentaRepositorio(contexto.Db);
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            TurnoCajaId = turno.Id,
            ClaveIdempotencia = "IDEM-TEST-456"
        };

        await ventaRepo.AgregarAsync(venta);
        await contexto.Db.SaveChangesAsync();

        contexto.Db.ChangeTracker.Clear();

        var recuperada = await ventaRepo.ObtenerPorClaveIdempotenciaAsync("IDEM-TEST-456");
        Assert.NotNull(recuperada);
        Assert.Equal(venta.Id, recuperada.Id);
    }
}

public class MapeoModeloTests
{
    [Fact]
    public void Modelo_SeCreaSinErrores()
    {
        using var contexto = new ContextoPruebasSqlite();
        Assert.NotNull(contexto.Db.Model);
    }

    [Fact]
    public void Personas_TablasLlavesEIndicesNoCambian()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        var empleado = modelo.FindEntityType(typeof(Empleado))!;
        var cliente = modelo.FindEntityType(typeof(Cliente))!;

        Assert.Equal("Empleados", empleado.GetTableName());
        Assert.Equal("Clientes", cliente.GetTableName());
        Assert.Equal("Id", empleado.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal("Id", cliente.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Contains(
            empleado.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Documento"));
        Assert.Contains(
            cliente.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Documento"));

        var fkUsuario = modelo.FindEntityType(typeof(Usuario))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Empleado));
        var fkVenta = modelo.FindEntityType(typeof(Venta))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Cliente));

        Assert.Equal("Empleados", fkUsuario.PrincipalEntityType.GetTableName());
        Assert.Equal("Clientes", fkVenta.PrincipalEntityType.GetTableName());
    }

    [Fact]
    public void StockLote_NoSeMapea_Y_EstanteYBodegaTienenTablasSeparadas()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        var tipoStockLote = modelo.FindEntityType(typeof(StockLote));
        Assert.Null(tipoStockLote);

        var tipoEstante = modelo.FindEntityType(typeof(StockEstante));
        var tipoBodega = modelo.FindEntityType(typeof(StockBodega));

        Assert.NotNull(tipoEstante);
        Assert.NotNull(tipoBodega);
        Assert.NotEqual(tipoEstante.GetTableName(), tipoBodega.GetTableName());
    }

    [Fact]
    public void Jerarquias_UsaDiscriminadorEnMismaTabla()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        var tipoProductoUnitario = modelo.FindEntityType(
            typeof(ProductoUnitario));
        var tipoProductoPesado = modelo.FindEntityType(
            typeof(ProductoPesado));
        Assert.NotNull(tipoProductoUnitario);
        Assert.NotNull(tipoProductoPesado);
        Assert.Equal(
            tipoProductoUnitario.GetTableName(),
            tipoProductoPesado.GetTableName());

        var tipoPagoEfectivo = modelo.FindEntityType(typeof(PagoEfectivo));
        var tipoPagoTarjeta = modelo.FindEntityType(typeof(PagoTarjeta));
        Assert.NotNull(tipoPagoEfectivo);
        Assert.NotNull(tipoPagoTarjeta);
        Assert.Equal(
            tipoPagoEfectivo.GetTableName(),
            tipoPagoTarjeta.GetTableName());

        var tipoIva = modelo.FindEntityType(typeof(Iva));
        var tipoRetencion = modelo.FindEntityType(typeof(Retencion));
        Assert.NotNull(tipoIva);
        Assert.NotNull(tipoRetencion);
        Assert.Equal(tipoIva.GetTableName(), tipoRetencion.GetTableName());
    }

    [Fact]
    public void IndicesUnicos_EstanConfiguradosEnMetadatos()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        Assert.Contains(
            modelo.FindEntityType(typeof(Producto))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "CodigoBarras"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Empleado))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Documento"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Usuario))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(
                p => p.Name == "NombreUsuario"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Rol))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Nombre"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Parametro))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Clave"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Venta))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(
                p => p.Name == "ClaveIdempotencia"));
    }

    [Fact]
    public async Task IndiceUnico_LanzaExcepcionAlDuplicar()
    {
        using var contexto = new ContextoPruebasSqlite();

        contexto.Db.Roles.Add(new Rol
        {
            Nombre = "Supervisor",
            Permisos = Permiso.VerInventario
        });
        await contexto.Db.SaveChangesAsync();

        contexto.Db.Roles.Add(new Rol
        {
            Nombre = "Supervisor",
            Permisos = Permiso.Vender
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Persistencia_TiposEspeciales_DateOnlyEnumsYPermiso()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 19", PuntosBasicos = 1900 };
        contexto.Db.Impuestos.Add(impuesto);

        var categoria = new Categoria { Nombre = "Abarrotes" };
        var proveedor = new Proveedor
        {
            Nombre = "Molino Central",
            Nit = "900123456",
            Telefono = "3001234567"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var rol = new Rol
        {
            Nombre = "Auditor",
            Permisos = Permiso.VerAuditoria | Permiso.VerReportes
        };
        contexto.Db.Roles.Add(rol);

        var producto = new ProductoUnitario
        {
            Nombre = "Arroz",
            CodigoBarras = "7701234567890",
            PrecioVenta = 3500,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.Add(producto);

        var vencimiento = new DateOnly(2027, 5, 20);
        var lote = new Lote
        {
            Producto = producto,
            NumeroLote = "LOT-001",
            FechaVencimiento = vencimiento,
            CostoUnitario = 2000
        };
        contexto.Db.Lotes.Add(lote);

        var movimiento = new MovimientoInventario
        {
            ProductoId = producto.Id,
            Lote = lote,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 50,
            Fecha = contexto.Reloj.AhoraUtc()
        };
        contexto.Db.Movimientos.Add(movimiento);

        await contexto.Db.SaveChangesAsync();

        contexto.Db.ChangeTracker.Clear();

        var loteLeido = await contexto.Db.Lotes
            .FirstAsync(l => l.NumeroLote == "LOT-001");
        var rolLeido = await contexto.Db.Roles
            .FirstAsync(r => r.Nombre == "Auditor");
        var movLeido = await contexto.Db.Movimientos
            .FirstAsync(m => m.Cantidad == 50);

        Assert.Equal(vencimiento, loteLeido.FechaVencimiento);
        Assert.Equal(
            Permiso.VerAuditoria | Permiso.VerReportes,
            rolLeido.Permisos);
        Assert.Equal(TipoMovimiento.Ingreso, movLeido.Tipo);
    }

    [Fact]
    public async Task FechaCreacion_SeAsignaAutomaticamenteAlGuardar()
    {
        using var contexto = new ContextoPruebasSqlite();

        var parametro = new Parametro
        {
            Clave = "test.clave",
            Valor = "123",
            Descripcion = "Prueba"
        };

        contexto.Db.Parametros.Add(parametro);
        await contexto.Db.SaveChangesAsync();

        Assert.Equal(contexto.Reloj.AhoraUtc(), parametro.FechaCreacion);
    }
}
