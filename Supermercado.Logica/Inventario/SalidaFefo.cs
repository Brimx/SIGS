using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Inventario;

public class SalidaFefo : IEstrategiaSalida
{
    public IEnumerable<Lote> Ordenar(IEnumerable<Lote> lotes, DateOnly hoy)
        => lotes
            .Where(l => !l.EstaVencido(hoy))
            .OrderBy(l => l.FechaVencimiento);
}
