using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Inventario;

public record AlertaStockMinimo(Producto Producto, int CantidadEnEstante);

public interface IServicioConsultaInventario
{
    Task<IReadOnlyList<StockConsolidado>> ObtenerStockConsolidadoAsync(int? productoId = null);
    Task<IReadOnlyList<KardexMovimiento>> ObtenerKardexAsync(int productoId, DateTime desde, DateTime hasta);
}

public interface IServicioAlertasInventario
{
    Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync();
    Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync();
}

public interface IServicioMermas
{
    Task RegistrarAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo);
}

public interface IServicioReabastecimiento
{
    Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync();
    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
    Task AtenderAsync(int reabastecimientoId);
}

// Interfaz núcleo de inventario
public interface IServicioInventario
{
    Task DescontarVentaAsync(int productoId, int cantidad, Venta venta);
    Task ReintegrarVentaAsync(int ventaId);
    Task TrasladarAEstanteAsync(int productoId, int cantidad);
    Task RegistrarIngresoAsync(Lote lote, int cantidad);
    Task RegistrarMermaAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo);
}
