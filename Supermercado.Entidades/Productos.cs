using Supermercado.Entidades.Base;
using Supermercado.Entidades.Finanzas;

namespace Supermercado.Entidades.Productos
{
public abstract class Producto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public long PrecioVenta { get; set; }
    public int StockMinimoEstante { get; set; }
    public bool Activo { get; set; } = true;

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public int ImpuestoId { get; set; }
    public Impuesto? Impuesto { get; set; }

    public abstract long CalcularSubtotal(int cantidad);

    public virtual string UnidadMedida => "und";
}

public class Categoria : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
}

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

// PrecioVenta es el precio por kilo; la cantidad va en gramos.
public sealed class ProductoPesado : Producto
{
    public const int GramosPorKilo = 1000;

    public override string UnidadMedida => "kg";

    public override long CalcularSubtotal(int cantidadEnGramos)
        => Dinero.DividirRedondeando(PrecioVenta * cantidadEnGramos, GramosPorKilo);
}

public sealed class ProductoUnitario : Producto
{
    public override long CalcularSubtotal(int cantidad)
        => PrecioVenta * cantidad;
}

public class Proveedor : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}
}
