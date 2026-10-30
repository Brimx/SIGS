using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Productos;

public interface IServicioBusquedaProductos
{
    Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(
        string termino,
        int limite = 20);
}
