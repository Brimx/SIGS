using Supermercado.Entidades.Sincronizacion;

namespace Supermercado.Entidades.Contratos;

public interface IEventoOutboxRepositorio : IRepositorio<EventoOutbox>
{
    Task<IReadOnlyList<EventoOutbox>> ObtenerPendientesAsync(int limite = 50);
}
