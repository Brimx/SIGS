using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Ventas;

public class DetalleVenta : EntidadBase
{
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public long PrecioUnitario { get; set; }
    public long Subtotal { get; set; }
    public int PuntosBasicosIva { get; set; }
    public long Iva { get; set; }

    public static DetalleVenta Crear(Producto producto, int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        var impuesto = producto.Impuesto
            ?? throw new ValidacionException(
                "El producto no trae su impuesto cargado.");
        var subtotal = producto.CalcularSubtotal(cantidad);

        return new DetalleVenta
        {
            ProductoId = producto.Id,
            Producto = producto,
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Subtotal = subtotal,
            PuntosBasicosIva = impuesto.PuntosBasicos,
            Iva = impuesto.Calcular(subtotal)
        };
    }
}
