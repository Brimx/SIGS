using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface ILoteRepositorio : IRepositorio<Lote>
{
    Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta);
    Task<IReadOnlyList<Lote>> ObtenerPorProductoAsync(int productoId);
}
