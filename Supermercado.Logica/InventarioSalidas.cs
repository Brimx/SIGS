using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Inventario
{
public record AlertaStockMinimo(Producto Producto, int CantidadEnEstante);

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

public abstract class EstrategiaSalida
{
    public abstract IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        where T : StockLote;
}

public class SalidaFefo : EstrategiaSalida
{
    public override IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        => stocks
            .Where(s => !s.Lote.EstaVencido(hoy))
            .OrderBy(s => s.Lote.FechaVencimiento)
            .ThenBy(s => s.LoteId);
}
}
