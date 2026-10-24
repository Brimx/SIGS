using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Productos;

// PrecioVenta es el precio por kilo; la cantidad va en gramos.
public sealed class ProductoPesado : Producto
{
    public const int GramosPorKilo = 1000;

    public override string UnidadMedida => "kg";

    public override long CalcularSubtotal(int cantidadEnGramos)
        => Dinero.DividirRedondeando(PrecioVenta * cantidadEnGramos, GramosPorKilo);
}
