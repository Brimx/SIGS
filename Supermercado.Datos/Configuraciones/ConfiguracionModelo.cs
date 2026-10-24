using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos.Configuraciones;

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
