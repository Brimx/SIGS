using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Productos;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class CatalogosTests
{
    private readonly RepositorioEnMemoria<Categoria> categorias = new();
    private readonly RepositorioEnMemoria<Proveedor> proveedores = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioCatalogos CrearServicio(
        Permiso permisos = Permiso.GestionarProductos)
        => new(
            categorias,
            proveedores,
            auditoria,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public async Task CrearCategoria_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio();

        var creada = await servicio.CrearCategoriaAsync("Bebidas");

        Assert.Equal("Bebidas", creada.Nombre);
        Assert.Single(categorias.Datos);
        Assert.Contains(AccionAuditoria.CategoriaCreada, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearCategoria_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearCategoriaAsync("Lácteos"));
    }

    [Fact]
    public async Task EditarCategoria_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var cat = new Categoria { Id = 1, Nombre = "Vieja" };
        await categorias.AgregarAsync(cat);

        await servicio.EditarCategoriaAsync(1, "Nueva");

        Assert.Equal("Nueva", cat.Nombre);
        Assert.Contains(
            AccionAuditoria.CategoriaModificada,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearProveedor_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio();

        var prov = await servicio.CrearProveedorAsync(
            "Distribuidora ABC",
            "900123456-1",
            "3001112233",
            "contacto@abc.com");

        Assert.Equal("Distribuidora ABC", prov.Nombre);
        Assert.Equal("900123456-1", prov.Nit);
        Assert.Single(proveedores.Datos);
        Assert.Contains(AccionAuditoria.ProveedorCreado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearProveedor_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Ninguno);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearProveedorAsync("X", "123", "", ""));
    }

    [Fact]
    public async Task EditarProveedor_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var prov = new Proveedor
        {
            Id = 1,
            Nombre = "Antiguo",
            Nit = "111",
            Telefono = "123",
            Correo = "a@a.com"
        };
        await proveedores.AgregarAsync(prov);

        await servicio.EditarProveedorAsync(
            1, "Actualizado", "222", "456", "b@b.com");

        Assert.Equal("Actualizado", prov.Nombre);
        Assert.Equal("222", prov.Nit);
        Assert.Contains(
            AccionAuditoria.ProveedorModificado,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }
}
