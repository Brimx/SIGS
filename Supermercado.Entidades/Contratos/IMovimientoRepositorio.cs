using Supermercado.Entidades.Inventario;

namespace Supermercado.Entidades.Contratos;

public interface IMovimientoRepositorio : IRepositorio<MovimientoInventario>
{
    Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(
        int ventaId);
}
