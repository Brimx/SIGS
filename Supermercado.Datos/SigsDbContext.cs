using Microsoft.EntityFrameworkCore;
using Supermercado.Datos.Configuraciones;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos;

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
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<TurnoCaja> Turnos => Set<TurnoCaja>();
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
