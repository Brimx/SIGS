using Supermercado.Entidades.Inventario;

namespace Supermercado.Entidades.Contratos;

public interface IReabastecimientoRepositorio : IRepositorio<Reabastecimiento>
{
    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
}
