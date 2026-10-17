namespace Supermercado.Entidades.Productos;

// PrecioVenta es el precio por kilo; la cantidad va en gramos.
public class ProductoPesado : Producto
{
    public override string UnidadMedida => "kg";

    public override long CalcularSubtotal(int cantidadEnGramos)
        => PrecioVenta * cantidadEnGramos / 1000;
}
