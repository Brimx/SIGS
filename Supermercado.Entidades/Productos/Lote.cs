using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Productos;

public class Lote : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string NumeroLote { get; set; } = string.Empty;
    public DateOnly FechaIngreso { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public int CantidadIngresada { get; set; }
    public long CostoUnitario { get; set; }

    public bool EstaVencido(DateOnly hoy)
        => FechaVencimiento < hoy;

    public int DiasParaVencer(DateOnly hoy)
        => FechaVencimiento.DayNumber - hoy.DayNumber;
}
