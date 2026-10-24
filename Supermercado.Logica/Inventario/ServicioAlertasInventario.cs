using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Inventario;

public class ServicioAlertasInventario(
    IProductoRepositorio productos,
    IStockRepositorio<StockEstante> estante,
    ILoteRepositorio lotes,
    IServicioParametros parametros,
    ISesionActual sesion,
    TimeProvider reloj) : IServicioAlertasInventario
{
    public async Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync()
    {
        sesion.Exigir(Permiso.VerInventario);

        var totales = await estante.ObtenerTotalesPorProductoAsync();
        var todos = await productos.ObtenerTodosAsync();

        return todos
            .Where(p => p.Activo)
            .Select(p => Alerta(p, totales))
            .Where(a => a.CantidadEnEstante < a.Producto.StockMinimoEstante)
            .ToList();
    }

    public async Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
    {
        sesion.Exigir(Permiso.VerInventario);

        var dias = await parametros.ObtenerEnteroAsync(
            ClavesParametro.DiasAlertaVencimiento);

        var limite = reloj.HoyLocal().AddDays(dias);
        return await lotes.ObtenerPorVencerAsync(limite);
    }

    private static AlertaStockMinimo Alerta(
        Producto producto,
        IReadOnlyDictionary<int, int> totales)
        => new(producto, totales.GetValueOrDefault(producto.Id));
}
