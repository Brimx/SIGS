using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Productos;

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
