using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Productos;

public class Lote : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateOnly FechaIngreso { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public int CantidadIngresada { get; set; }
    public int CantidadEnBodega { get; set; }
    public int CantidadEnEstante { get; set; }

    public bool EstaVencido(DateOnly hoy)
        => FechaVencimiento < hoy;

    public int DiasParaVencer(DateOnly hoy)
        => FechaVencimiento.DayNumber - hoy.DayNumber;

    public void MoverAEstante(int cantidad)
    {
        if (cantidad > CantidadEnBodega)
            throw new StockInsuficienteException(NumeroLote);

        CantidadEnBodega -= cantidad;
        CantidadEnEstante += cantidad;
    }

    public void SacarDeEstante(int cantidad)
    {
        if (cantidad > CantidadEnEstante)
            throw new StockInsuficienteException(NumeroLote);

        CantidadEnEstante -= cantidad;
    }
}
