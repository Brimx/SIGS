namespace Supermercado.Entidades.Productos;

public sealed class ProductoUnitario : Producto
{
    public override long CalcularSubtotal(int cantidad)
        => PrecioVenta * cantidad;
}
