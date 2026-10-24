using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

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
