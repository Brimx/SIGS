using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Productos;

public interface IServicioCatalogos
{
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync();

    Task<Categoria> CrearCategoriaAsync(string nombre);

    Task EditarCategoriaAsync(int id, string nuevoNombre);

    Task<IReadOnlyList<Proveedor>> ObtenerProveedoresAsync();

    Task<Proveedor> CrearProveedorAsync(
        string nombre,
        string nit,
        string telefono,
        string correo);

    Task EditarProveedorAsync(
        int id,
        string nuevoNombre,
        string nit,
        string telefono,
        string correo);
}
