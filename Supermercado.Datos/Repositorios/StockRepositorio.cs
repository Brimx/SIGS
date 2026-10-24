using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;

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

    public async Task<IReadOnlyDictionary<int, int>>
        ObtenerTotalesPorProductoAsync()
        => await Db.Set<T>()
            .GroupBy(s => s.ProductoId)
            .Select(g => new { Id = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
}
