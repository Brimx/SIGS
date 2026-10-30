using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Productos;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class MantenimientoProductosTests
{
    private readonly ProductosEnMemoria productos = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioMantenimientoProductos CrearServicio(
        Permiso permisos = Permiso.GestionarProductos)
        => new(
            productos,
            auditoria,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public async Task EditarAsync_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz Antiguo",
            CodigoBarras = "7701",
            PrecioVenta = 3000,
            StockMinimoEstante = 5,
            CategoriaId = 1,
            ProveedorId = 1,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.EditarAsync(
            1, "Arroz Premium", 10, 2, 3, 2);

        Assert.Equal("Arroz Premium", producto.Nombre);
        Assert.Equal(10, producto.StockMinimoEstante);
        Assert.Equal(2, producto.CategoriaId);
        Assert.Equal(3, producto.ProveedorId);
        Assert.Equal(2, producto.ImpuestoId);
        Assert.Contains(
            AccionAuditoria.ProductoModificado,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task EditarAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.EditarAsync(1, "Nuevo", 5, 1, 1, 1));
    }

    [Fact]
    public async Task EditarAsync_ProductoInexistente_LanzaValidacionException()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.EditarAsync(999, "Nuevo", 5, 1, 1, 1));
    }

    [Fact]
    public async Task CambiarPrecioAsync_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Café",
            CodigoBarras = "7702",
            PrecioVenta = 5000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.CambiarPrecioAsync(1, 5500);

        Assert.Equal(5500, producto.PrecioVenta);
        Assert.Contains(
            AccionAuditoria.PrecioCambiado,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CambiarPrecioAsync_PrecioInvalido_LanzaError()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Café",
            CodigoBarras = "7702",
            PrecioVenta = 5000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarPrecioAsync(1, 0));
        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarPrecioAsync(1, -100));
    }

    [Fact]
    public async Task ActivarYDesactivar_CambianEstadoYAuditan()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Galletas",
            CodigoBarras = "7703",
            Activo = true,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.DesactivarAsync(1);
        Assert.False(producto.Activo);
        Assert.Equal(1, trabajo.Guardados);

        await servicio.ActivarAsync(1);
        Assert.True(producto.Activo);
        Assert.Equal(2, trabajo.Guardados);
        Assert.Equal(2, auditoria.Acciones.Count(
            a => a == AccionAuditoria.ProductoModificado));
    }
}
