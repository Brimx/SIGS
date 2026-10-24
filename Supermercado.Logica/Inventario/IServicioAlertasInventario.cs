using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Inventario;

public record AlertaStockMinimo(Producto Producto, int CantidadEnEstante);

public interface IServicioAlertasInventario
{
    Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync();
    Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync();
}
