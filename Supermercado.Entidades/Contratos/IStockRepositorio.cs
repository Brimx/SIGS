using Supermercado.Entidades.Inventario;

namespace Supermercado.Entidades.Contratos;

public interface IStockRepositorio<T> : IRepositorio<T>
    where T : StockLote
{
    // Debe incluir el Lote: la estrategia de salida necesita su fecha.
    Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId);

    Task<T?> ObtenerPorLoteAsync(int loteId);

    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync();
}
