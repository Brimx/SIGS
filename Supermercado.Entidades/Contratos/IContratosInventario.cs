using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface IStockRepositorio<T> : IRepositorio<T>
    where T : StockLote
{
    // Debe incluir el Lote: la estrategia de salida necesita su fecha.
    Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId);
    Task<T?> ObtenerPorLoteAsync(int loteId);
    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync();
}

public interface ILoteRepositorio : IRepositorio<Lote>
{
    // Solo lotes que todavía tienen existencias.
    Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta);
}

public interface IMovimientoRepositorio : IRepositorio<MovimientoInventario>
{
    Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(int ventaId);
}

public interface IReabastecimientoRepositorio : IRepositorio<Reabastecimiento>
{
    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
}
