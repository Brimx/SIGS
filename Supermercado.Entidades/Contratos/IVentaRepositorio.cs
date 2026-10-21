using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Contratos;

public interface IVentaRepositorio : IRepositorio<Venta>
{
    Task<IReadOnlyList<Venta>> ObtenerDelDiaAsync(DateOnly dia);
}
