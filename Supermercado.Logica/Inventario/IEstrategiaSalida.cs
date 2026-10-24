using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

public interface IEstrategiaSalida
{
    IEnumerable<T> Ordenar<T>(IEnumerable<T> stocks, DateOnly hoy)
        where T : StockLote;
}
