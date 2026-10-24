using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Contratos;

// Las consultas por turno y rango incluyen Detalles y Pagos.
public interface IVentaRepositorio : IRepositorio<Venta>
{
    Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId);

    Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta);
}
