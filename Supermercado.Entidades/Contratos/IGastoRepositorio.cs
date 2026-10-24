using Supermercado.Entidades.Finanzas;

namespace Supermercado.Entidades.Contratos;

public interface IGastoRepositorio : IRepositorio<Gasto>
{
    Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta);
}
