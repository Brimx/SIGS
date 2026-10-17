using Supermercado.Entidades.Productos;

namespace Supermercado.Logica.Inventario;

public interface IEstrategiaSalida
{
    IEnumerable<Lote> Ordenar(IEnumerable<Lote> lotes, DateOnly hoy);
}
