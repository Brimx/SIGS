namespace Supermercado.Entidades.Productos;

public class ProductoUnitario : Producto
{
    public override long CalcularSubtotal(int cantidad)
        => PrecioVenta * cantidad;
}
