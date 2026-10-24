using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Contratos;

public interface ITurnoCajaRepositorio : IRepositorio<TurnoCaja>
{
    Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId);
    Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId);
}
