using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Productos
{
public class ServicioProductos(
    ProductoRepositorio productos,
    LoteRepositorio? lotes = null,
    ServicioInventario? inventario = null,
    IConsultaCodigoBarras? externo = null,
    SesionActual? sesion = null,
    TimeProvider? reloj = null,
    CategoriaRepositorio? categorias = null,
    ProveedorRepositorio? proveedores = null,
    ServicioAuditoria? auditoria = null,
    ImpuestoRepositorio? impuestos = null)
{
    private readonly TimeProvider _reloj = reloj ?? TimeProvider.System;

    // --- Catálogo y Registro ---

    public virtual async Task RegistrarAsync(Producto producto)
    {
        sesion?.Exigir(Permiso.GestionarProductos);
        Validar(producto);

        var existente = await productos.ObtenerPorCodigoBarrasAsync(producto.CodigoBarras);
        if (existente is not null)
            throw new ValidacionException("Ya existe ese código de barras.");

        await productos.AgregarAsync(producto);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.ProductoCreado,
                nameof(Producto),
                producto.Id,
                $"Producto {producto.Nombre} ({producto.CodigoBarras})");

        await productos.GuardarAsync();
    }

    public virtual async Task<Producto> CrearProductoAsync(
        string codigoBarras,
        string nombre,
        int categoriaId,
        long precioVenta,
        int impuestoId,
        int stockMinimoEstante,
        int proveedorId = 0)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(codigoBarras))
            throw new ValidacionException("Falta el código de barras.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre.");
        if (categoriaId <= 0)
            throw new ValidacionException("Falta la categoría.");
        if (precioVenta <= 0)
            throw new ValidacionException("El precio debe ser positivo.");
        if (stockMinimoEstante < 0)
            throw new ValidacionException("El mínimo no puede ser negativo.");
        if (impuestoId <= 0)
            throw new ValidacionException("Falta el IVA.");

        if (proveedorId <= 0 && proveedores is not null)
        {
            var disponibles = await proveedores.ObtenerTodosAsync();
            proveedorId = disponibles.FirstOrDefault()?.Id ?? 0;
        }

        if (proveedorId <= 0)
            throw new ValidacionException(
                "No hay proveedores registrados. Cree uno en Compras y Proveedores antes de dar de alta productos.");

        var producto = new ProductoUnitario
        {
            CodigoBarras = codigoBarras.Trim(),
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId,
            PrecioVenta = precioVenta,
            ImpuestoId = impuestoId,
            StockMinimoEstante = stockMinimoEstante,
            ProveedorId = proveedorId,
            Activo = true
        };

        await RegistrarAsync(producto);
        return producto;
    }

    public virtual Task<IReadOnlyList<Producto>> ObtenerProductosAsync()
        => productos.ObtenerTodosAsync();

    public virtual async Task<IReadOnlyList<Producto>> ObtenerProductosActivosAsync()
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        var todos = await productos.ObtenerTodosAsync();
        return todos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();
    }

    public virtual Task<IReadOnlyList<Impuesto>> ObtenerImpuestosAsync()
        => impuestos is not null
            ? impuestos.ObtenerTodosAsync()
            : Task.FromResult<IReadOnlyList<Impuesto>>([]);

    public virtual Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo)
        => productos.ObtenerPorCodigoBarrasAsync(codigo);

    public virtual async Task<InfoProductoExterno?> ConsultarExternoAsync(
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

    public virtual async Task RegistrarIngresoLoteAsync(
        int productoId,
        string numeroLote,
        DateOnly vencimiento,
        int cantidad,
        long costoUnitario)
    {
        sesion?.Exigir(Permiso.GestionarProductos);

        var numero = numeroLote?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(numero))
            throw new ValidacionException("Falta el número de lote.");
        if (cantidad <= 0)
            throw new ValidacionException("La cantidad debe ser positiva.");
        if (costoUnitario < 0)
            throw new ValidacionException("El costo unitario no puede ser negativo.");
        if (vencimiento < _reloj.HoyLocal())
            throw new ValidacionException("La fecha de vencimiento no puede estar en el pasado.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        Lote? lote = null;
        if (lotes is not null)
        {
            var existentes = await lotes.ObtenerTodosAsync();
            lote = existentes.FirstOrDefault(l =>
                l.ProductoId == producto.Id
                && string.Equals(l.NumeroLote, numero, StringComparison.OrdinalIgnoreCase));
        }

        if (lote is null)
        {
            lote = new Lote
            {
                ProductoId = producto.Id,
                NumeroLote = numero,
                FechaIngreso = _reloj.HoyLocal(),
                FechaVencimiento = vencimiento,
                CantidadIngresada = cantidad,
                CostoUnitario = costoUnitario
            };

            if (lotes is not null)
                await lotes.AgregarAsync(lote);
        }
        else
        {
            if (vencimiento != lote.FechaVencimiento)
                throw new ValidacionException("Ese número de lote ya existe con otro vencimiento.");

            lote.CantidadIngresada += cantidad;
            lotes!.Actualizar(lote);
        }

        if (inventario is not null)
            await inventario.RegistrarIngresoAsync(lote, cantidad);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.IngresoBodegaRegistrado,
                nameof(Lote),
                lote.Id,
                $"Entrada a bodega: {producto.Nombre} | Lote {numero} | {cantidad} und | Vence {vencimiento:yyyy-MM-dd}");

        if (lotes is not null)
            await lotes.GuardarAsync();
    }

    // --- Búsqueda de Productos ---

    public virtual async Task<IReadOnlyList<Producto>> BuscarPorNombreAsync(
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

    public virtual async Task EditarAsync(
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

        await productos.GuardarAsync();
    }

    public virtual async Task CambiarPrecioAsync(int productoId, long nuevoPrecio)
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

        await productos.GuardarAsync();
    }

    public virtual async Task ActivarAsync(int productoId)
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

        await productos.GuardarAsync();
    }

    public virtual async Task DesactivarAsync(int productoId)
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

        await productos.GuardarAsync();
    }

    // --- Categorías y Proveedores ---

    public virtual Task<IReadOnlyList<Categoria>> ObtenerCategoriasAsync()
        => categorias is not null
            ? categorias.ObtenerTodosAsync()
            : Task.FromResult<IReadOnlyList<Categoria>>([]);

    public virtual async Task<Categoria> CrearCategoriaAsync(string nombre)
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

        if (categorias is not null)
            await categorias.GuardarAsync();

        return categoria;
    }

    public virtual async Task EditarCategoriaAsync(int id, string nuevoNombre)
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

        if (categorias is not null)
            await categorias.GuardarAsync();
    }

    public virtual Task<IReadOnlyList<Proveedor>> ObtenerProveedoresAsync()
        => proveedores is not null
            ? proveedores.ObtenerTodosAsync()
            : Task.FromResult<IReadOnlyList<Proveedor>>([]);

    public virtual async Task<Proveedor> CrearProveedorAsync(
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

        if (proveedores is not null)
            await proveedores.GuardarAsync();

        return proveedor;
    }

    public virtual async Task EditarProveedorAsync(
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

        if (proveedores is not null)
            await proveedores.GuardarAsync();
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

// Conversor de unidades de compra a unidades de venta para inventario y Kardex.
// Permite ingresar bultos, arrobas o cajas y transformarlos a kilos, gramos o unidades.
public static class ConversorUnidades
{
    public const string Bulto50Kg = "Bulto50Kg";
    public const string Arroba = "Arroba";
    public const string Libra = "Libra";
    public const string Kilo = "Kilo";
    public const string Gramo = "Gramo";
    public const string Caja = "Caja";
    public const string Unidad = "Unidad";

    private static readonly Dictionary<string, decimal> FactoresAGramos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Bulto50Kg] = 50_000m,
            [Arroba] = 12_500m,
            [Kilo] = 1_000m,
            [Libra] = 500m,
            [Gramo] = 1m
        };

    public static int ConvertirAGramos(decimal cantidad, string unidadOrigen)
    {
        if (cantidad <= 0)
            throw new ValidacionException("La cantidad debe ser mayor a cero.");

        if (!FactoresAGramos.TryGetValue(unidadOrigen.Trim(), out var factor))
            throw new ValidacionException(
                $"La unidad '{unidadOrigen}' no tiene factor de conversión a gramos registrado.");

        var gramos = (int)Math.Round(cantidad * factor, MidpointRounding.AwayFromZero);
        return Math.Max(1, gramos);
    }

    public static int ConvertirCajaAUnidades(int cantidadCajas, int unidadesPorCaja)
    {
        if (cantidadCajas <= 0 || unidadesPorCaja <= 0)
            throw new ValidacionException("La cantidad de cajas y unidades por caja deben ser positivas.");

        return cantidadCajas * unidadesPorCaja;
    }

    public static int Bultos50KgAGramos(decimal bultos) => ConvertirAGramos(bultos, Bulto50Kg);

    public static int ArrobasAGramos(decimal arrobas) => ConvertirAGramos(arrobas, Arroba);

    public static int KilosAGramos(decimal kilos) => ConvertirAGramos(kilos, Kilo);

    public static int LibrasAGramos(decimal libras) => ConvertirAGramos(libras, Libra);

    public static int CajasAUnidades(int cajas, int unidadesPorCaja) => ConvertirCajaAUnidades(cajas, unidadesPorCaja);

    public static decimal CalcularCostoPorGramo(long costoTotalCompra, decimal cantidadComprada, string unidadOrigen)
    {
        var gramosTotales = ConvertirAGramos(cantidadComprada, unidadOrigen);
        if (gramosTotales <= 0) return 0m;
        return (decimal)costoTotalCompra / gramosTotales;
    }

    public static long CalcularCostoUnitarioDeCaja(long costoTotalCompra, int cantidadCajas, int unidadesPorCaja)
    {
        var unidadesTotales = ConvertirCajaAUnidades(cantidadCajas, unidadesPorCaja);
        if (unidadesTotales <= 0) return 0;
        return (long)Math.Round((decimal)costoTotalCompra / unidadesTotales, MidpointRounding.AwayFromZero);
    }
}

// Parser para códigos de barras EAN-13 emitidos por balanzas de fruver,
// carnicería y charcutería con prefijo de medida variable GS1 (20 a 29).
// Formato estándar retail: PP [PLU: 5 dígitos] [GRAMOS: 5 dígitos] [CHECK: 1 dígito]
public static class ParserCodigoBalanza
{
    public static ResultadoCodigoBalanza Parsear(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return new ResultadoCodigoBalanza(false, "", "", 0, codigo ?? "");

        var limpio = codigo.Trim();
        if (limpio.Length != 13 || !limpio.All(char.IsDigit))
            return new ResultadoCodigoBalanza(false, "", "", 0, limpio);

        var prefijoNum = int.Parse(limpio[..2]);
        if (prefijoNum is < 20 or > 29)
            return new ResultadoCodigoBalanza(false, "", "", 0, limpio);

        var prefijo = limpio[..2];
        var plu = limpio[2..7];
        var gramos = int.Parse(limpio[7..12]);
        var digitoControl = limpio[12] - '0';

        return new ResultadoCodigoBalanza(true, prefijo, plu, gramos, limpio, digitoControl);
    }
}

public record ResultadoCodigoBalanza(
    bool EsCodigoBalanza,
    string Prefijo,
    string Plu,
    int Gramos,
    string CodigoOriginal,
    int DigitoControl = 0)
{
    public bool EsValido => EsCodigoBalanza;
    public decimal PesoKg => Gramos / 1000m;
}
}
