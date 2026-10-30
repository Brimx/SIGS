using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

public interface IServicioConsultaInventario
{
    Task<IReadOnlyList<StockConsolidado>> ObtenerStockConsolidadoAsync(
        int? productoId = null);

    Task<IReadOnlyList<KardexMovimiento>> ObtenerKardexAsync(
        int productoId,
        DateTime desde,
        DateTime hasta);
}
