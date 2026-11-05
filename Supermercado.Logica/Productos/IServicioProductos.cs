using Supermercado.Entidades.Productos;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Productos;

public interface IServicioBusquedaProductos
{
    Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(string termino, int limite = 20);
}

public interface IServicioMantenimientoProductos
{
    Task EditarAsync(
        int productoId,
        string nuevoNombre,
        int stockMinimoEstante,
        int categoriaId,
        int proveedorId,
        int impuestoId);
    Task CambiarPrecioAsync(int productoId, long nuevoPrecio);
    Task ActivarAsync(int productoId);
    Task DesactivarAsync(int productoId);
}

public interface IServicioCatalogos
{
    Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync();
    Task<Categoria> CrearCategoriaAsync(string nombre);
    Task EditarCategoriaAsync(int id, string nuevoNombre);
    Task<IReadOnlyList<Proveedor>> ObtenerProveedoresAsync();
    Task<Proveedor> CrearProveedorAsync(string nombre, string nit, string telefono, string correo);
    Task EditarProveedorAsync(int id, string nuevoNombre, string nit, string telefono, string correo);
}

// Interfaz núcleo de productos
public interface IServicioProductos
{
    Task RegistrarAsync(Producto producto);
    Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo);
    Task<InfoProductoExterno?> ConsultarExternoAsync(string codigo, CancellationToken ct = default);
    Task RegistrarIngresoLoteAsync(
        int productoId,
        string numeroLote,
        DateOnly vencimiento,
        int cantidad,
        long costoUnitario);
}
