using Supermercado.Entidades.Productos;
using Xunit;

namespace Supermercado.Pruebas;

public class ProductoTests
{
    [Fact]
    public void Unitario_MultiplicaPrecioPorCantidad()
    {
        var producto = new ProductoUnitario { PrecioVenta = 2500 };
        Assert.Equal(7500L, producto.CalcularSubtotal(3));
    }

    [Fact]
    public void Pesado_CobraPorGramos()
    {
        var producto = new ProductoPesado { PrecioVenta = 8000 };
        Assert.Equal(4000L, producto.CalcularSubtotal(500));
    }

    [Fact]
    public void Lote_VencidoSiLaFechaYaPaso()
    {
        var lote = new Lote
        {
            FechaVencimiento = new DateOnly(2026, 10, 1)
        };
        var hoy = new DateOnly(2026, 10, 17);
        Assert.True(lote.EstaVencido(hoy));
    }
}
