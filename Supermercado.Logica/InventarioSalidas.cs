using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Inventario
{
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
    Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync(string? origen = null);
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

public static class PlanDeSalida
{
    public static List<(T Fila, int Tomar)> Repartir<T>(
        List<T> filas,
        int cantidad) where T : StockLote
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        var disponible = filas.Sum(f => f.Cantidad);

        if (disponible < cantidad)
            throw new StockInsuficienteException(
                $"se pidieron {cantidad} y hay {disponible}");

        var plan = new List<(T Fila, int Tomar)>();
        var pendiente = cantidad;

        foreach (var fila in filas)
        {
            var tomar = Math.Min(pendiente, fila.Cantidad);
            if (tomar > 0)
                plan.Add((fila, tomar));
            pendiente -= tomar;
        }

        return plan;
    }
}

public class SalidaFefo : IEstrategiaSalida
{
    public IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        where T : StockLote
        => stocks
            .Where(s => !s.Lote.EstaVencido(hoy))
            .OrderBy(s => s.Lote.FechaVencimiento)
            .ThenBy(s => s.LoteId);
}

public interface IEstrategiaSalida
{
    IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        where T : StockLote;
}
}
