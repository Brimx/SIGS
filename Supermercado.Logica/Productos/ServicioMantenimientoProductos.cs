using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Productos;

public class ServicioMantenimientoProductos(
    IProductoRepositorio productos,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioMantenimientoProductos
{
    public async Task EditarAsync(
        int productoId,
        string nuevoNombre,
        int stockMinimoEstante,
        int categoriaId,
        int proveedorId,
        int impuestoId)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre.");
        if (stockMinimoEstante < 0)
            throw new ValidacionException("El mínimo no puede ser negativo.");
        if (categoriaId <= 0 || proveedorId <= 0 || impuestoId <= 0)
            throw new ValidacionException("Relaciones inválidas.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        producto.Nombre = nuevoNombre.Trim();
        producto.StockMinimoEstante = stockMinimoEstante;
        producto.CategoriaId = categoriaId;
        producto.ProveedorId = proveedorId;
        producto.ImpuestoId = impuestoId;

        productos.Actualizar(producto);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ProductoModificado,
            nameof(Producto),
            producto.Id,
            $"Datos actualizados de {producto.Nombre}");
        await trabajo.GuardarAsync();
    }

    public async Task CambiarPrecioAsync(int productoId, long nuevoPrecio)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (nuevoPrecio <= 0)
            throw new ValidacionException("El precio debe ser positivo.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        var anterior = producto.PrecioVenta;
        producto.PrecioVenta = nuevoPrecio;

        productos.Actualizar(producto);
        await auditoria.RegistrarAsync(
            AccionAuditoria.PrecioCambiado,
            nameof(Producto),
            producto.Id,
            $"Precio cambiado de {anterior} a {nuevoPrecio}");
        await trabajo.GuardarAsync();
    }

    public async Task ActivarAsync(int productoId)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        producto.Activo = true;
        productos.Actualizar(producto);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ProductoModificado,
            nameof(Producto),
            producto.Id,
            $"Producto {producto.Nombre} activado");
        await trabajo.GuardarAsync();
    }

    public async Task DesactivarAsync(int productoId)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        producto.Activo = false;
        productos.Actualizar(producto);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ProductoModificado,
            nameof(Producto),
            producto.Id,
            $"Producto {producto.Nombre} desactivado");
        await trabajo.GuardarAsync();
    }
}
