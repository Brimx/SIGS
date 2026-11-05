using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Contratos;

public interface IVentaRepositorio : IRepositorio<Venta>
{
    Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId);
    Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta);
    Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia);
}

public interface ITurnoCajaRepositorio : IRepositorio<TurnoCaja>
{
    Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId);
    Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId);
}
