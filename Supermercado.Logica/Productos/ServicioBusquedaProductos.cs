using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Productos;

public class ServicioBusquedaProductos(
    IProductoRepositorio productos) : IServicioBusquedaProductos
{
    public async Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(
        string termino,
        int limite = 20)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return [];

        var limpio = termino.Trim();
        var maximo = Math.Max(1, limite);

        var todos = await productos.ObtenerTodosAsync();

        return todos
            .Where(p => p.Activo
                && p.Nombre.Contains(
                    limpio,
                    StringComparison.OrdinalIgnoreCase))
            .Take(maximo)
            .ToList();
    }
}
