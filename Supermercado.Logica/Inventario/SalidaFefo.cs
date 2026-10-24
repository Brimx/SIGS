using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

public class SalidaFefo : IEstrategiaSalida
{
    public IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        where T : StockLote
        => stocks
            .Where(s => !s.Lote.EstaVencido(hoy))
            .OrderBy(s => s.Lote.FechaVencimiento)
            .ThenBy(s => s.LoteId);
}
