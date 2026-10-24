using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface ILoteRepositorio : IRepositorio<Lote>
{
    // Solo lotes que todavía tienen existencias.
    Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta);
}
