using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface IProductoRepositorio : IRepositorio<Producto>
{
    // Debe incluir el Impuesto: la venta lo necesita para el IVA.
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo);
}
