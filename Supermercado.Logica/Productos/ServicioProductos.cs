using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Productos;

public class ServicioProductos(
    IProductoRepositorio productos,
    IRepositorio<Lote>? lotes = null,
    IServicioInventario? inventario = null,
    IConsultaCodigoBarras? externo = null,
    ISesionActual? sesion = null,
    IUnidadDeTrabajo? trabajo = null,
    TimeProvider? reloj = null,
    IRepositorio<Categoria>? categorias = null,
    IRepositorio<Proveedor>? proveedores = null,
    IServicioAuditoria? auditoria = null) : IServicioProductos,
        IServicioBusquedaProductos,
        IServicioMantenimientoProductos,
        IServicioCatalogos
{
    private readonly TimeProvider _reloj = reloj ?? TimeProvider.System;

    // --- Catálogo y Registro ---

    public async Task RegistrarAsync(Producto producto)
    {
        sesion?.Exigir(Permiso.GestionarProductos);
        Validar(producto);

        var existente = await productos.ObtenerPorCodigoBarrasAsync(producto.CodigoBarras);
        if (existente is not null)
            throw new ValidacionException("Ya existe ese código de barras.");

        await productos.AgregarAsync(producto);
        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo)
        => productos.ObtenerPorCodigoBarrasAsync(codigo);

    public async Task<InfoProductoExterno?> ConsultarExternoAsync(
        string codigo,
        CancellationToken ct = default)
    {
        if (externo is null)
            return null;

        try
        {
            return await externo.ConsultarAsync(codigo, ct);
        }
        catch (Exception ex) when (
            !ct.IsCancellationRequested
            && ex is HttpRequestException
                or TaskCanceledException
                or TimeoutException
                or IOException)
        {
            return null;
        }
    }

    public async Task RegistrarIngresoLoteAsync(
        int productoId,
        string numeroLote,
        DateOnly vencimiento,
        int cantidad,
        long costoUnitario)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(numeroLote))
            throw new ValidacionException("Falta el número de lote.");
        if (cantidad <= 0 || costoUnitario < 0)
            throw new ValidacionException("Cantidad o costo inválidos.");
        if (vencimiento < _reloj.HoyLocal())
            throw new ValidacionException("El lote ya está vencido.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        var lote = new Lote
        {
            ProductoId = producto.Id,
            NumeroLote = numeroLote,
            FechaIngreso = _reloj.HoyLocal(),
            FechaVencimiento = vencimiento,
            CantidadIngresada = cantidad,
            CostoUnitario = costoUnitario
        };

        if (lotes is not null)
            await lotes.AgregarAsync(lote);

        if (inventario is not null)
            await inventario.RegistrarIngresoAsync(lote, cantidad);

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    // --- Búsqueda de Productos ---

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
                && p.Nombre.Contains(limpio, StringComparison.OrdinalIgnoreCase))
            .Take(maximo)
            .ToList();
    }

    // --- Mantenimiento de Productos ---

    public async Task EditarAsync(
        int productoId,
        string nuevoNombre,
        int stockMinimoEstante,
        int categoriaId,
        int proveedorId,
        int impuestoId)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

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

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProductoModificado,
                nameof(Producto),
                producto.Id,
                $"Datos actualizados de {producto.Nombre}");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public async Task CambiarPrecioAsync(int productoId, long nuevoPrecio)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (nuevoPrecio <= 0)
            throw new ValidacionException("El precio debe ser positivo.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        var anterior = producto.PrecioVenta;
        producto.PrecioVenta = nuevoPrecio;

        productos.Actualizar(producto);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.PrecioCambiado,
                nameof(Producto),
                producto.Id,
                $"Precio cambiado de {anterior} a {nuevoPrecio}");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public async Task ActivarAsync(int productoId)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        producto.Activo = true;
        productos.Actualizar(producto);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProductoModificado,
                nameof(Producto),
                producto.Id,
                $"Producto {producto.Nombre} activado");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public async Task DesactivarAsync(int productoId)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        producto.Activo = false;
        productos.Actualizar(producto);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProductoModificado,
                nameof(Producto),
                producto.Id,
                $"Producto {producto.Nombre} desactivado");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    // --- Categorías y Proveedores ---

    public Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync()
        => categorias is not null
            ? categorias.ObtenerTodosAsync()
            : Task.FromResult<IReadOnlyList<Categoria>>([]);

    public async Task<Categoria> CrearCategoriaAsync(string nombre)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre de la categoría.");

        if (categorias is null)
            throw new InvalidOperationException("Repositorio de categorías no configurado.");

        var categoria = new Categoria { Nombre = nombre.Trim() };
        await categorias.AgregarAsync(categoria);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.CategoriaCreada,
                nameof(Categoria),
                detalle: $"Categoría creada: {categoria.Nombre}");

        if (trabajo is not null)
            await trabajo.GuardarAsync();

        return categoria;
    }

    public async Task EditarCategoriaAsync(int id, string nuevoNombre)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre de la categoría.");

        if (categorias is null)
            return;

        var categoria = await categorias.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("La categoría no existe.");

        categoria.Nombre = nuevoNombre.Trim();
        categorias.Actualizar(categoria);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.CategoriaModificada,
                nameof(Categoria),
                categoria.Id,
                $"Categoría renombrada a {categoria.Nombre}");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public Task<IReadOnlyList<Proveedor>> ObtenerProveedoresAsync()
        => proveedores is not null
            ? proveedores.ObtenerTodosAsync()
            : Task.FromResult<IReadOnlyList<Proveedor>>([]);

    public async Task<Proveedor> CrearProveedorAsync(
        string nombre,
        string nit,
        string telefono,
        string correo)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre del proveedor.");

        if (proveedores is null)
            throw new InvalidOperationException("Repositorio de proveedores no configurado.");

        var proveedor = new Proveedor
        {
            Nombre = nombre.Trim(),
            Nit = Validador.ExigirNit(nit, "el NIT del proveedor"),
            Telefono = Validador.ExigirTelefonoOpcional(telefono),
            Correo = Validador.ExigirCorreoOpcional(correo)
        };

        await proveedores.AgregarAsync(proveedor);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProveedorCreado,
                nameof(Proveedor),
                detalle: $"Proveedor {proveedor.Nombre} ({proveedor.Nit})");

        if (trabajo is not null)
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
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre del proveedor.");
        var nitValido = Validador.ExigirNit(nit, "el NIT del proveedor");
        var telefonoValido = Validador.ExigirTelefonoOpcional(telefono);
        var correoValido = Validador.ExigirCorreoOpcional(correo);

        if (proveedores is null)
            return;

        var proveedor = await proveedores.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("El proveedor no existe.");

        proveedor.Nombre = nuevoNombre.Trim();
        proveedor.Nit = nitValido;
        proveedor.Telefono = telefonoValido;
        proveedor.Correo = correoValido;

        proveedores.Actualizar(proveedor);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProveedorModificado,
                nameof(Proveedor),
                proveedor.Id,
                $"Proveedor actualizado: {proveedor.Nombre}");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    private static void Validar(Producto producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Nombre))
            throw new ValidacionException("Falta el nombre.");
        if (string.IsNullOrWhiteSpace(producto.CodigoBarras))
            throw new ValidacionException("Falta el código de barras.");
        if (producto.PrecioVenta <= 0)
            throw new ValidacionException("El precio debe ser positivo.");
        if (producto.StockMinimoEstante < 0)
            throw new ValidacionException("El mínimo no puede ser negativo.");
        if (producto.ImpuestoId <= 0)
            throw new ValidacionException("Falta el impuesto.");
    }
}

// Adaptadores para compatibilidad de pruebas existentes
public class ServicioBusquedaProductos(
    IProductoRepositorio productos) : ServicioProductos(productos);

public class ServicioMantenimientoProductos(
    IProductoRepositorio productos,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : ServicioProductos(
        productos,
        auditoria: auditoria,
        sesion: sesion,
        trabajo: trabajo);

public class ServicioCatalogos(
    IRepositorio<Categoria> categorias,
    IRepositorio<Proveedor> proveedores,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : ServicioProductos(
        null!,
        categorias: categorias,
        proveedores: proveedores,
        auditoria: auditoria,
        sesion: sesion,
        trabajo: trabajo);
