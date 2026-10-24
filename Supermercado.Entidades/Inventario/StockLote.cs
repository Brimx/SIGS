using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Inventario;

public abstract class StockLote : EntidadBase
{
    public int ProductoId { get; set; }
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
    public int Cantidad { get; private set; }

    public void Aumentar(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        Cantidad += cantidad;
    }

    public void Disminuir(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        if (cantidad > Cantidad)
            throw new StockInsuficienteException($"lote {LoteId}");

        Cantidad -= cantidad;
    }
}
