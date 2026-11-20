using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos
{
public class SigsDbContext(
    DbContextOptions<SigsDbContext> opciones,
    TimeProvider reloj) : DbContext(opciones)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<StockEstante> StockEstante => Set<StockEstante>();
    public DbSet<StockBodega> StockBodega => Set<StockBodega>();
    public DbSet<MovimientoInventario> Movimientos
        => Set<MovimientoInventario>();
    public DbSet<Merma> Mermas => Set<Merma>();
    public DbSet<Reabastecimiento> Reabastecimientos
        => Set<Reabastecimiento>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaEnEspera> VentasEnEspera => Set<VentaEnEspera>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<TurnoCaja> Turnos => Set<TurnoCaja>();
    public DbSet<RetiroCaja> RetirosCaja => Set<RetiroCaja>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Impuesto> Impuestos => Set<Impuesto>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<Nomina> Nominas => Set<Nomina>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();
    public DbSet<IntentoAcceso> Intentos => Set<IntentoAcceso>();
    public DbSet<ConfiguracionWidget> Widgets => Set<ConfiguracionWidget>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelo)
        => ConfiguracionModelo.Aplicar(modelo);

    public override int SaveChanges()
    {
        MarcarFechaCreacion();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken ct = default)
    {
        MarcarFechaCreacion();
        return await base.SaveChangesAsync(ct);
    }

    private void MarcarFechaCreacion()
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;

        foreach (var entrada in ChangeTracker.Entries<EntidadBase>()
                     .Where(e => e.State == EntityState.Added))
            entrada.Entity.FechaCreacion = ahora;
    }
}

public static class ConfiguracionModelo
{
    public static void Aplicar(ModelBuilder modelo)
    {
        RegistrarJerarquias(modelo);
        DefinirIndicesUnicos(modelo);
        EvitarBorradoEnCascada(modelo);
    }

    // Las jerarquías usan una sola tabla con columna discriminadora.
    // StockLote no se registra: cada stock tiene su propia tabla.
    private static void RegistrarJerarquias(ModelBuilder modelo)
    {
        modelo.Entity<Producto>();
        modelo.Entity<ProductoUnitario>();
        modelo.Entity<ProductoPesado>();
        modelo.Entity<Pago>();
        modelo.Entity<PagoEfectivo>();
        modelo.Entity<PagoTarjeta>();
        modelo.Entity<PagoTransferencia>();
        modelo.Entity<Impuesto>();
        modelo.Entity<Iva>();
        modelo.Entity<Retencion>();
        modelo.Entity<StockEstante>();
        modelo.Entity<StockBodega>();
    }

    private static void DefinirIndicesUnicos(ModelBuilder modelo)
    {
        modelo.Entity<Producto>().HasIndex(p => p.CodigoBarras).IsUnique();
        modelo.Entity<Lote>()
            .HasIndex(l => new { l.ProductoId, l.NumeroLote }).IsUnique();
        modelo.Entity<StockEstante>().HasIndex(s => s.LoteId).IsUnique();
        modelo.Entity<StockBodega>().HasIndex(s => s.LoteId).IsUnique();
        modelo.Entity<Empleado>().HasIndex(e => e.Documento).IsUnique();
        modelo.Entity<Cliente>().HasIndex(c => c.Documento).IsUnique();
        modelo.Entity<Venta>()
            .HasIndex(v => v.ClaveIdempotencia).IsUnique();
        modelo.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        modelo.Entity<Rol>().HasIndex(r => r.Nombre).IsUnique();
        modelo.Entity<Parametro>().HasIndex(p => p.Clave).IsUnique();
        modelo.Entity<ConfiguracionWidget>()
            .HasIndex(c => new { c.EmpleadoId, c.WidgetId }).IsUnique();
    }

    // Nada se borra en cascada: las ventas se anulan, no se eliminan.
    private static void EvitarBorradoEnCascada(ModelBuilder modelo)
    {
        var relaciones = modelo.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys());

        foreach (var relacion in relaciones)
            relacion.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

public class FabricaSigsDbContext : IDesignTimeDbContextFactory<SigsDbContext>
{
    public SigsDbContext CreateDbContext(string[] args)
    {
        var ruta = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : RutasDatos.BaseDatosPorDefecto();

        var carpeta = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        var constructor = new DbContextOptionsBuilder<SigsDbContext>();
        constructor.UseSqlite($"Data Source={ruta}");

        return new SigsDbContext(constructor.Options, TimeProvider.System);
    }
}

public static class RutasDatos
{
    private const string Carpeta = "SIGS";
    private const string Archivo = "sigs.db";

    // Carpeta de datos del usuario: sirve igual en Windows y Linux.
    public static string BaseDatosPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            Archivo);

    public static string CarpetaRespaldosPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            "respaldos");

    public static string ArchivoLogPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            "logs",
            "errores.log");
}

public static class ExtensionesDatos
{
    public static IServiceCollection AgregarDatos(
        this IServiceCollection servicios,
        string rutaBaseDatos)
    {
        servicios.AddSingleton(TimeProvider.System);

        var carpeta = Path.GetDirectoryName(rutaBaseDatos);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        servicios.AddDbContext<SigsDbContext>(opciones =>
            opciones.UseSqlite($"Data Source={rutaBaseDatos}"));

        servicios.AddScoped<InicializadorBaseDatos>();

        servicios.AddScoped<IProductoRepositorio, ProductoRepositorio>();
        servicios.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
        servicios.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        servicios.AddScoped<IImpuestoRepositorio, ImpuestoRepositorio>();
        servicios.AddScoped<ILoteRepositorio, LoteRepositorio>();
        servicios.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();
        servicios.AddScoped<IReabastecimientoRepositorio,
            ReabastecimientoRepositorio>();
        servicios.AddScoped<IStockEstanteRepositorio,
            StockEstanteRepositorio>();
        servicios.AddScoped<IStockBodegaRepositorio, StockBodegaRepositorio>();
        servicios.AddScoped<IMermaRepositorio, MermaRepositorio>();
        servicios.AddScoped<IVentaRepositorio, VentaRepositorio>();
        servicios.AddScoped<IVentaEnEsperaRepositorio,
            VentaEnEsperaRepositorio>();
        servicios.AddScoped<ITurnoCajaRepositorio, TurnoCajaRepositorio>();
        servicios.AddScoped<ICajaRepositorio, CajaRepositorio>();
        servicios.AddScoped<IRetiroCajaRepositorio, RetiroCajaRepositorio>();
        servicios.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        servicios.AddScoped<IGastoRepositorio, GastoRepositorio>();
        servicios.AddScoped<INominaRepositorio, NominaRepositorio>();
        servicios.AddScoped<IConfiguracionWidgetRepositorio,
            ConfiguracionWidgetRepositorio>();
        servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicios.AddScoped<IRolRepositorio, RolRepositorio>();
        servicios.AddScoped<IParametroRepositorio, ParametroRepositorio>();
        servicios.AddScoped<IIntentoAccesoRepositorio,
            IntentoAccesoRepositorio>();
        servicios.AddScoped<IRegistroAuditoriaRepositorio,
            RegistroAuditoriaRepositorio>();
        servicios.AddScoped<IEmpleadoRepositorio, EmpleadoRepositorio>();
        servicios.AddScoped<ICargoRepositorio, CargoRepositorio>();

        return servicios;
    }
}
}

namespace Supermercado.Datos.Repositorios
{
    // SaveChanges ya es una transacción: todo lo pendiente se guarda junto.
    public abstract class RepositorioBase(SigsDbContext db)
    {
        protected SigsDbContext Db => db;

        public Task<int> GuardarAsync() => db.SaveChangesAsync();
    }
}
