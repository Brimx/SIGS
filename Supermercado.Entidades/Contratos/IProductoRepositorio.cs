using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface IProductoRepositorio : IRepositorio<Producto>
{
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo);
}
