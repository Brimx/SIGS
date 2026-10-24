using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

public interface IServicioReabastecimiento
{
    Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync();
    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
    Task AtenderAsync(int reabastecimientoId);
}
