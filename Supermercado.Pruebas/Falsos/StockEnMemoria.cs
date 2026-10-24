using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;

namespace Supermercado.Pruebas.Falsos;

public class StockEnMemoria<T> : RepositorioEnMemoria<T>, IStockRepositorio<T>
    where T : StockLote
{
    public override Task AgregarAsync(T entidad)
    {
        if (entidad.LoteId != 0 && Datos.Any(s => s.LoteId == entidad.LoteId))
            throw new InvalidOperationException(
                $"Ya existe stock para el lote {entidad.LoteId}.");

        return base.AgregarAsync(entidad);
    }

    public Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId)
        => Task.FromResult<IReadOnlyList<T>>(Datos
            .Where(s => s.ProductoId == productoId && s.Cantidad > 0)
            .ToList());

    public Task<T?> ObtenerPorLoteAsync(int loteId)
        => Task.FromResult(Datos.FirstOrDefault(s => s.LoteId == loteId));

    public Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync()
        => Task.FromResult<IReadOnlyDictionary<int, int>>(Datos
            .GroupBy(s => s.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Cantidad)));
}
