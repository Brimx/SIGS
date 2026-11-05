using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;

namespace Supermercado.Datos.Repositorios;

public class StockRepositorio<T>(SigsDbContext db)
    : Repositorio<T>(db), IStockRepositorio<T>
    where T : StockLote
{
    public async Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId)
        => await Db.Set<T>()
            .Include(s => s.Lote)
            .Where(s => s.ProductoId == productoId && s.Cantidad > 0)
            .ToListAsync();

    public Task<T?> ObtenerPorLoteAsync(int loteId)
        => Db.Set<T>().FirstOrDefaultAsync(s => s.LoteId == loteId);

    public async Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync()
        => await Db.Set<T>()
            .GroupBy(s => s.ProductoId)
            .Select(g => new { Id = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
}

public class LoteRepositorio(SigsDbContext db)
    : Repositorio<Lote>(db), ILoteRepositorio
{
    public async Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta)
        => await Db.Lotes
            .Include(l => l.Producto)
            .Where(l => l.FechaVencimiento <= hasta
                && (Db.StockEstante.Any(s => s.LoteId == l.Id && s.Cantidad > 0)
                    || Db.StockBodega.Any(s => s.LoteId == l.Id && s.Cantidad > 0)))
            .OrderBy(l => l.FechaVencimiento)
            .ToListAsync();
}

public class MovimientoRepositorio(SigsDbContext db)
    : Repositorio<MovimientoInventario>(db), IMovimientoRepositorio
{
    public async Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(int ventaId)
        => await Db.Movimientos
            .Where(m => m.VentaId == ventaId && m.Tipo == TipoMovimiento.Venta)
            .ToListAsync();
}

public class ReabastecimientoRepositorio(SigsDbContext db)
    : Repositorio<Reabastecimiento>(db), IReabastecimientoRepositorio
{
    public async Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
        => await Db.Reabastecimientos
            .Where(r => r.Estado == EstadoReabastecimiento.Pendiente)
            .ToListAsync();
}
