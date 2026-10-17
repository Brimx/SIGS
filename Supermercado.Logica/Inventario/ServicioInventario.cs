using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Inventario;

public class ServicioInventario(
    ILoteRepositorio lotes,
    IUnidadDeTrabajo trabajo,
    IEstrategiaSalida estrategia,
    TimeProvider reloj) : IServicioInventario
{
    public Task VenderDeEstanteAsync(int productoId, int cantidad)
        => EjecutarAsync(
            productoId, cantidad,
            l => l.CantidadEnEstante,
            (l, n) => l.SacarDeEstante(n));

    public Task ReponerEstanteAsync(int productoId, int cantidad)
        => EjecutarAsync(
            productoId, cantidad,
            l => l.CantidadEnBodega,
            (l, n) => l.MoverAEstante(n));

    private async Task EjecutarAsync(
        int productoId,
        int cantidad,
        Func<Lote, int> disponible,
        Action<Lote, int> aplicar)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var todos = await lotes.ObtenerPorProductoAsync(productoId);
        var ordenados = estrategia.Ordenar(todos, hoy).ToList();

        if (ordenados.Sum(disponible) < cantidad)
            throw new StockInsuficienteException($"producto {productoId}");

        var pendiente = cantidad;
        foreach (var lote in ordenados)
        {
            var tomar = Math.Min(pendiente, disponible(lote));
            aplicar(lote, tomar);
            pendiente -= tomar;
        }

        await trabajo.GuardarAsync();
    }
}
