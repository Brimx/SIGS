using Supermercado.Entidades.Base;
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

    public static DetalleVenta Crear(Producto producto, int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        return new DetalleVenta
        {
            ProductoId = producto.Id,
            Producto = producto,
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Subtotal = producto.CalcularSubtotal(cantidad)
        };
    }
}
