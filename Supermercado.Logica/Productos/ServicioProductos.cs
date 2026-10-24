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
    IRepositorio<Lote> lotes,
    IServicioInventario inventario,
    IConsultaCodigoBarras externo,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioProductos
{
    public async Task RegistrarAsync(Producto producto)
    {
        sesion.Exigir(Permiso.GestionarProductos);
        Validar(producto);

        var existente = await productos
            .ObtenerPorCodigoBarrasAsync(producto.CodigoBarras);

        if (existente is not null)
            throw new ValidacionException("Ya existe ese código de barras.");

        await productos.AgregarAsync(producto);
        await trabajo.GuardarAsync();
    }

    public Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo)
        => productos.ObtenerPorCodigoBarrasAsync(codigo);

    public async Task<InfoProductoExterno?> ConsultarExternoAsync(
        string codigo,
        CancellationToken ct = default)
    {
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
        sesion.Exigir(Permiso.GestionarProductos);

        if (string.IsNullOrWhiteSpace(numeroLote))
            throw new ValidacionException("Falta el número de lote.");
        if (cantidad <= 0 || costoUnitario < 0)
            throw new ValidacionException("Cantidad o costo inválidos.");
        if (vencimiento < reloj.HoyLocal())
            throw new ValidacionException("El lote ya está vencido.");

        var producto = await productos.ObtenerPorIdAsync(productoId)
            ?? throw new ValidacionException("El producto no existe.");

        var lote = new Lote
        {
            ProductoId = producto.Id,
            NumeroLote = numeroLote,
            FechaIngreso = reloj.HoyLocal(),
            FechaVencimiento = vencimiento,
            CantidadIngresada = cantidad,
            CostoUnitario = costoUnitario
        };

        await lotes.AgregarAsync(lote);
        await inventario.RegistrarIngresoAsync(lote, cantidad);
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
