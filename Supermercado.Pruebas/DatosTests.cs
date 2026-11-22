using Supermercado.Datos;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.DatosDemo;
using Supermercado.Pruebas.Falsos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Clientes;

namespace Supermercado.Pruebas
{
public class GeneradorDatosDemoTests
{
    private class EscenarioDemo
    {
        public CategoriasEnMemoria Categorias { get; } = new();
        public ProveedoresEnMemoria Proveedores { get; } = new();
        public ImpuestosEnMemoria Impuestos { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public LotesEnMemoria Lotes { get; } = new();
        public StockEstanteEnMemoria Estantes { get; } = new();
        public StockBodegaEnMemoria Bodegas { get; } = new();
        public MovimientosEnMemoria Movimientos { get; } = new();
        public CargosEnMemoria Cargos { get; } = new();
        public RolesEnMemoria Roles { get; } = new();
        public EmpleadosEnMemoria Empleados { get; } = new();
        public UsuariosEnMemoria Usuarios { get; } = new();
        public CajasEnMemoria Cajas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public GastosEnMemoria Gastos { get; } = new();
        public HasheadorFalso Hasheador { get; } = new();
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public GeneradorDatosDemo Generador { get; }

        public EscenarioDemo()
        {
            foreach (var rol in ValoresIniciales.Roles())
                Roles.Datos.Add(rol);
            foreach (var cargo in ValoresIniciales.Cargos())
                Cargos.Datos.Add(cargo);
            foreach (var imp in ValoresIniciales.Impuestos())
                Impuestos.Datos.Add(imp);

            Generador = new GeneradorDatosDemo(
                Categorias,
                Proveedores,
                Impuestos,
                Productos,
                Lotes,
                Estantes,
                Bodegas,
                Movimientos,
                Cargos,
                Roles,
                Empleados,
                Usuarios,
                Cajas,
                Turnos,
                Ventas,
                Gastos,
                Hasheador,
                Reloj);
        }
    }

    [Fact]
    public async Task GenerarAsync_PueblaEntidadesYGuarda()
    {
        var e = new EscenarioDemo();

        await e.Generador.GenerarAsync();

        Assert.NotEmpty(e.Productos.Datos);
        Assert.NotEmpty(e.Lotes.Datos);
        Assert.NotEmpty(e.Ventas.Datos);
        Assert.NotEmpty(e.Gastos.Datos);
        Assert.NotEmpty(e.Usuarios.Datos);
        Assert.True(e.Productos.Guardados >= 1);
    }

    [Fact]
    public async Task GenerarAsync_MantieneCoherenciaMatematicaDeStock()
    {
        var e = new EscenarioDemo();
        await e.Generador.GenerarAsync();

        // Para cada lote generado, el stock físico (estante + bodega)
        // debe igualar exactamente: Ingresos - VentasEfectivas
        foreach (var lote in e.Lotes.Datos)
        {
            var cantBodega = e.Bodegas.Datos
                .Where(s => s.LoteId == lote.Id)
                .Sum(s => s.Cantidad);
            var cantEstante = e.Estantes.Datos
                .Where(s => s.LoteId == lote.Id)
                .Sum(s => s.Cantidad);

            var totalFisico = cantBodega + cantEstante;

            var ingresos = e.Movimientos.Datos
                .Where(m => m.LoteId == lote.Id
                    && m.Tipo == TipoMovimiento.Ingreso)
                .Sum(m => m.Cantidad);

            var ventasCompletadas = e.Movimientos.Datos
                .Where(m => m.LoteId == lote.Id
                    && m.Tipo == TipoMovimiento.Venta
                    && m.Venta?.Estado == EstadoVenta.Completada)
                .Sum(m => m.Cantidad);

            Assert.Equal(ingresos - ventasCompletadas, totalFisico);
        }
    }

    [Fact]
    public async Task GenerarAsync_NoDuplicaSiYaExistenProductos()
    {
        var e = new EscenarioDemo();
        await e.Generador.GenerarAsync();
        var conteoInicial = e.Productos.Datos.Count;

        await e.Generador.GenerarAsync();
        Assert.Equal(conteoInicial, e.Productos.Datos.Count);
    }
}

public class RespaldoYErroresTests
{
    [Fact]
    public void FabricaSigsDbContext_IncluyeMigracionInicial()
    {
        var tempDb = Path.Combine(
            Path.GetTempPath(), $"sigs-test-{Guid.NewGuid():N}.db");
        try
        {
            var fabrica = new FabricaSigsDbContext();
            using var db = fabrica.CreateDbContext([tempDb]);
            var migraciones = db.Database.GetMigrations().ToList();

            Assert.Contains("20260420000000_Inicial", migraciones);
        }
        finally
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    [Fact]
    public async Task ServicioRespaldo_CopiaArchivoYRegistraAuditoria()
    {
        var dirTemp = Path.Combine(
            Path.GetTempPath(), $"sigs-bk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirTemp);

        var origen = Path.Combine(dirTemp, "sigs.db");
        var carpetaDestino = Path.Combine(dirTemp, "backups");
        await File.WriteAllBytesAsync(origen, [1, 2, 3, 4, 5]);

        try
        {
            var parametros = new ParametrosFalsos();
            parametros.Valores[ClavesParametro.CarpetaRespaldos] =
                carpetaDestino;
            var sesion = Sesiones.Con(Permiso.GestionarRoles);
            var auditoria = new AuditoriaFalsa();
            var reloj = new RelojFijo(
                new DateTime(2026, 4, 20, 14, 30, 0, DateTimeKind.Utc));
            var registros = new RegistrosAuditoriaEnMemoria();

            var servicio = new ServicioRespaldo(
                parametros, sesion, auditoria, registros, reloj);

            var rutaCopia = await servicio.CrearRespaldoAsync(origen);

            Assert.True(File.Exists(rutaCopia));
            Assert.Equal(
                [1, 2, 3, 4, 5], await File.ReadAllBytesAsync(rutaCopia));
            Assert.Contains(
                AccionAuditoria.RespaldoBaseDatos,
                auditoria.Acciones);
            Assert.Equal(1, registros.Guardados);
        }
        finally
        {
            if (Directory.Exists(dirTemp))
                Directory.Delete(dirTemp, recursive: true);
        }
    }

    [Fact]
    public async Task ServicioRespaldo_SinPermisoOSinArchivo_Falla()
    {
        var parametros = new ParametrosFalsos();
        var sesionSinPermiso = Sesiones.Con(Permiso.Vender);
        var auditoria = new AuditoriaFalsa();
        var reloj = new RelojFijo(DateTime.UtcNow);
        var registros = new RegistrosAuditoriaEnMemoria();

        var servicioSinPermiso = new ServicioRespaldo(
            parametros, sesionSinPermiso, auditoria, registros, reloj);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicioSinPermiso.CrearRespaldoAsync("/no/existe.db"));

        var sesionAdmin = Sesiones.Con(Permiso.GestionarRoles);
        var servicioAdmin = new ServicioRespaldo(
            parametros, sesionAdmin, auditoria, registros, reloj);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicioAdmin.CrearRespaldoAsync("/no/existe/sigs.db"));
    }

    [Fact]
    public void TraductorExcepciones_DistingueDominioDeInesperadasYLlevaLog()
    {
        var archivoLog = Path.Combine(
            Path.GetTempPath(), $"sigs-err-{Guid.NewGuid():N}.log");
        try
        {
            var reloj = new RelojFijo(
                new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc));
            using var proveedor = new ProveedorLogArchivo(archivoLog, reloj);
            using var factory = LoggerFactory.Create(
                b => b.AddProvider(proveedor));
            var logger = factory.CreateLogger<TraductorExcepciones>();
            var traductor = new TraductorExcepciones(logger);

            var msgDominio = traductor.Traducir(
                new ValidacionException("El precio debe ser positivo."));
            Assert.Equal("El precio debe ser positivo.", msgDominio);
            Assert.False(File.Exists(archivoLog));

            var msgInesperado = traductor.Traducir(
                new InvalidOperationException("Fallo interno SQL."));
            Assert.Contains("error inesperado", msgInesperado);
            Assert.DoesNotContain("SQL", msgInesperado);

            Assert.True(File.Exists(archivoLog));
            var contenido = File.ReadAllText(archivoLog);
            Assert.Contains("Fallo interno SQL.", contenido);
            Assert.Contains("[Error]", contenido);
        }
        finally
        {
            if (File.Exists(archivoLog))
                File.Delete(archivoLog);
        }
    }
}
}

namespace Supermercado.Pruebas.Integracion
{
public class InicializadorBaseDatosTests
{
    [Fact]
    public async Task CrearYSembrarAsync_EsIdempotente()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);

        await inicializador.CrearYSembrarAsync();
        var rolesIniciales = await contexto.Db.Roles.CountAsync();
        var parametrosIniciales = await contexto.Db.Parametros.CountAsync();
        var cajasIniciales = await contexto.Db.Cajas.CountAsync();

        // Segunda ejecución consecutiva
        await inicializador.CrearYSembrarAsync();

        Assert.Equal(rolesIniciales, await contexto.Db.Roles.CountAsync());
        Assert.Equal(
            parametrosIniciales,
            await contexto.Db.Parametros.CountAsync());
        Assert.Equal(cajasIniciales, await contexto.Db.Cajas.CountAsync());
    }

    [Fact]
    public async Task CrearYSembrarAsync_NoPisaParametrosEditados()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);

        await inicializador.CrearYSembrarAsync();

        var param = await contexto.Db.Parametros
            .FirstAsync(p => p.Clave == ClavesParametro.MaxIntentosFallidos);
        param.Valor = "99";
        await contexto.Db.SaveChangesAsync();

        // Ejecutar sembrado de nuevo
        await inicializador.CrearYSembrarAsync();
        contexto.Db.ChangeTracker.Clear();

        var paramDespues = await contexto.Db.Parametros
            .FirstAsync(p => p.Clave == ClavesParametro.MaxIntentosFallidos);

        Assert.Equal("99", paramDespues.Valor);
    }

    [Fact]
    public async Task CrearYSembrarAsync_AsignaPermisosFaltantesARolesExistentes()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);

        await inicializador.CrearYSembrarAsync();

        var admin = await contexto.Db.Roles.FirstAsync(r => r.Nombre == NombresRol.Admin);
        var bodeguero = await contexto.Db.Roles.FirstAsync(r => r.Nombre == NombresRol.Bodeguero);
        admin.Permisos &= ~Permiso.GestionarProductos;
        bodeguero.Permisos &= ~Permiso.GestionarProductos;
        bodeguero.Permisos |= Permiso.VerReportes;
        await contexto.Db.SaveChangesAsync();

        await inicializador.CrearYSembrarAsync();
        contexto.Db.ChangeTracker.Clear();

        var adminDespues = await contexto.Db.Roles.FirstAsync(r => r.Nombre == NombresRol.Admin);
        var bodegueroDespues = await contexto.Db.Roles.FirstAsync(r => r.Nombre == NombresRol.Bodeguero);
        var gerenteDespues = await contexto.Db.Roles.FirstAsync(r => r.Nombre == NombresRol.Gerente);

        Assert.True(adminDespues.Permisos.HasFlag(Permiso.GestionarProductos));
        Assert.True(bodegueroDespues.Permisos.HasFlag(Permiso.GestionarProductos));
        Assert.True(bodegueroDespues.Permisos.HasFlag(Permiso.VerReportes));
        Assert.False(gerenteDespues.Permisos.HasFlag(Permiso.GestionarProductos));
    }
}

public class ClientesSqliteTests
{
    [Fact]
    public async Task Cliente_SePersisteYSeBuscaPorDocumento()
    {
        using var contexto = new ContextoPruebasSqlite();
        var repo = new ClienteRepositorio(contexto.Db);
        await repo.AgregarAsync(new Cliente
        {
            Documento = "1020304050",
            Nombres = "Lucía"
        });
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var leido = await repo.ObtenerPorDocumentoAsync("1020304050");

        Assert.Equal("Lucía", leido?.Nombres);
    }

    [Fact]
    public async Task Cliente_DocumentoDuplicado_ViolaElIndiceUnico()
    {
        using var contexto = new ContextoPruebasSqlite();
        contexto.Db.Clientes.Add(new Cliente { Documento = "111111", Nombres = "A" });
        contexto.Db.Clientes.Add(new Cliente { Documento = "111111", Nombres = "B" });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.Db.SaveChangesAsync());
    }
}

public class RepositoriosSqliteTests
{
    [Fact]
    public async Task UsuarioRepositorio_ObtenerPorNombre_CargaEmpleadoYRol()
    {
        using var contexto = new ContextoPruebasSqlite();

        var rol = new Rol
        {
            Nombre = "Gerente",
            Permisos = Permiso.VerReportes
        };
        var cargo = new Cargo { Nombre = "Gerente de Tienda" };
        var empleado = new Empleado
        {
            Nombres = "Carlos",
            Apellidos = "Pérez",
            Documento = "12345",
            Correo = "carlos@sigs.com",
            Rol = rol,
            Cargo = cargo,
            Activo = true
        };
        var usuario = new Usuario
        {
            NombreUsuario = "carlos",
            HashClave = "hash",
            Empleado = empleado
        };

        contexto.Db.Usuarios.Add(usuario);
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new UsuarioRepositorio(contexto.Db);
        var obtenido = await repo.ObtenerPorNombreAsync("carlos");

        Assert.NotNull(obtenido);
        Assert.NotNull(obtenido.Empleado);
        Assert.NotNull(obtenido.Empleado.Rol);
        Assert.Equal("Gerente", obtenido.Empleado.Rol.Nombre);
        Assert.Equal(Permiso.VerReportes, obtenido.Empleado.Rol.Permisos);
    }

    [Fact]
    public async Task StockRepositorio_ObtenerTotalesPorProducto_AgrupaYSuma()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var categoria = new Categoria { Nombre = "Alimentos" };
        var proveedor = new Proveedor
        {
            Nombre = "Proveedor General",
            Nit = "800999000",
            Telefono = "3000000000"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var p1 = new ProductoUnitario
        {
            Nombre = "Leche",
            CodigoBarras = "111",
            PrecioVenta = 4000,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        var p2 = new ProductoUnitario
        {
            Nombre = "Pan",
            CodigoBarras = "222",
            PrecioVenta = 2000,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.AddRange(p1, p2);

        var l1 = new Lote
        {
            Producto = p1,
            NumeroLote = "L1",
            FechaVencimiento = new DateOnly(2027, 1, 1),
            CostoUnitario = 3000
        };
        var l2 = new Lote
        {
            Producto = p1,
            NumeroLote = "L2",
            FechaVencimiento = new DateOnly(2027, 2, 1),
            CostoUnitario = 3100
        };
        var l3 = new Lote
        {
            Producto = p2,
            NumeroLote = "L3",
            FechaVencimiento = new DateOnly(2027, 3, 1),
            CostoUnitario = 1500
        };
        contexto.Db.Lotes.AddRange(l1, l2, l3);
        await contexto.Db.SaveChangesAsync();

        var s1 = new StockEstante { ProductoId = p1.Id, LoteId = l1.Id };
        s1.Aumentar(10);
        var s2 = new StockEstante { ProductoId = p1.Id, LoteId = l2.Id };
        s2.Aumentar(15);
        var s3 = new StockEstante { ProductoId = p2.Id, LoteId = l3.Id };
        s3.Aumentar(8);
        contexto.Db.StockEstante.AddRange(s1, s2, s3);

        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new StockEstanteRepositorio(contexto.Db);
        var totales = await repo.ObtenerTotalesPorProductoAsync();

        Assert.Equal(25, totales[p1.Id]);
        Assert.Equal(8, totales[p2.Id]);
    }

    [Fact]
    public async Task LoteRepositorio_ObtenerPorVencer_FiltraPorFechaYStock()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var categoria = new Categoria { Nombre = "Lácteos" };
        var proveedor = new Proveedor
        {
            Nombre = "Lácteos San Juan",
            Nit = "800555444",
            Telefono = "3111111111"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var producto = new ProductoUnitario
        {
            Nombre = "Yogurt",
            CodigoBarras = "333",
            PrecioVenta = 2500,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.Add(producto);

        var loteVenceProntoConStock = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-STOCK",
            FechaVencimiento = new DateOnly(2026, 11, 5),
            CostoUnitario = 1500
        };
        var loteVenceProntoSinStock = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-VACIO",
            FechaVencimiento = new DateOnly(2026, 11, 4),
            CostoUnitario = 1500
        };
        var loteLejano = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-LEJOS",
            FechaVencimiento = new DateOnly(2027, 11, 1),
            CostoUnitario = 1500
        };
        contexto.Db.Lotes.AddRange(
            loteVenceProntoConStock,
            loteVenceProntoSinStock,
            loteLejano);

        var stock = new StockEstante
        {
            ProductoId = producto.Id,
            Lote = loteVenceProntoConStock
        };
        stock.Aumentar(5);
        contexto.Db.StockEstante.Add(stock);

        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new LoteRepositorio(contexto.Db);
        var porVencer = await repo.ObtenerPorVencerAsync(
            new DateOnly(2026, 11, 10));

        var obtenido = Assert.Single(porVencer);
        Assert.Equal("LV-STOCK", obtenido.NumeroLote);
    }
}
}
