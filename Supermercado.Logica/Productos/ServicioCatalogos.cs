using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Productos;

public class ServicioCatalogos(
    IRepositorio<Categoria> categorias,
    IRepositorio<Proveedor> proveedores,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioCatalogos
{
    public Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync()
        => categorias.ObtenerTodosAsync();

    public async Task<Categoria> CrearCategoriaAsync(string nombre)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre de la categoría.");

        var categoria = new Categoria { Nombre = nombre.Trim() };
        await categorias.AgregarAsync(categoria);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CategoriaCreada,
            nameof(Categoria),
            detalle: $"Categoría creada: {categoria.Nombre}");
        await trabajo.GuardarAsync();
        return categoria;
    }

    public async Task EditarCategoriaAsync(int id, string nuevoNombre)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre de la categoría.");

        var categoria = await categorias.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("La categoría no existe.");

        categoria.Nombre = nuevoNombre.Trim();
        categorias.Actualizar(categoria);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CategoriaModificada,
            nameof(Categoria),
            categoria.Id,
            $"Categoría renombrada a {categoria.Nombre}");
        await trabajo.GuardarAsync();
    }

    public Task<IReadOnlyList<Proveedor>> ObtenerProveedoresAsync()
        => proveedores.ObtenerTodosAsync();

    public async Task<Proveedor> CrearProveedorAsync(
        string nombre,
        string nit,
        string telefono,
        string correo)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre del proveedor.");
        if (string.IsNullOrWhiteSpace(nit))
            throw new ValidacionException("Falta el NIT del proveedor.");

        var proveedor = new Proveedor
        {
            Nombre = nombre.Trim(),
            Nit = nit.Trim(),
            Telefono = telefono?.Trim() ?? string.Empty,
            Correo = correo?.Trim() ?? string.Empty
        };

        await proveedores.AgregarAsync(proveedor);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ProveedorCreado,
            nameof(Proveedor),
            detalle: $"Proveedor {proveedor.Nombre} ({proveedor.Nit})");
        await trabajo.GuardarAsync();
        return proveedor;
    }

    public async Task EditarProveedorAsync(
        int id,
        string nuevoNombre,
        string nit,
        string telefono,
        string correo)
    {
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre del proveedor.");
        if (string.IsNullOrWhiteSpace(nit))
            throw new ValidacionException("Falta el NIT del proveedor.");

        var proveedor = await proveedores.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("El proveedor no existe.");

        proveedor.Nombre = nuevoNombre.Trim();
        proveedor.Nit = nit.Trim();
        proveedor.Telefono = telefono?.Trim() ?? string.Empty;
        proveedor.Correo = correo?.Trim() ?? string.Empty;

        proveedores.Actualizar(proveedor);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ProveedorModificado,
            nameof(Proveedor),
            proveedor.Id,
            $"Proveedor actualizado: {proveedor.Nombre}");
        await trabajo.GuardarAsync();
    }
}
