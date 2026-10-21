using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Xunit;

namespace Supermercado.Pruebas;

public class VentasTests
{
    private static Producto Arroz()
        => new ProductoUnitario { Id = 1, PrecioVenta = 3000 };

    [Fact]
    public void Total_SumaLosSubtotales()
    {
        var venta = new Venta();
        var queso = new ProductoPesado { Id = 2, PrecioVenta = 8000 };

        venta.AgregarProducto(Arroz(), 2);
        venta.AgregarProducto(queso, 500);

        Assert.Equal(10000L, venta.Total);
    }

    [Fact]
    public void PagoMixto_CalculaElCambio()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 10);
        venta.AgregarPago(new PagoTarjeta
        {
            Monto = 20000,
            UltimosDigitos = "1234"
        });
        venta.AgregarPago(new PagoEfectivo { Monto = 15000 });

        venta.Cerrar();

        Assert.Equal(5000L, venta.Cambio);
        Assert.Equal(EstadoVenta.Completada, venta.Estado);
    }

    [Fact]
    public void Cerrar_FallaSiElPagoNoAlcanza()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 1);
        venta.AgregarPago(new PagoEfectivo { Monto = 1000 });

        Assert.Throws<VentaInvalidaException>(() => venta.Cerrar());
    }

    [Fact]
    public void Cerrar_NoDaCambioDeUnaTarjeta()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 1);
        venta.AgregarPago(new PagoTarjeta
        {
            Monto = 5000,
            UltimosDigitos = "1234"
        });

        Assert.Throws<VentaInvalidaException>(() => venta.Cerrar());
    }

    [Fact]
    public void Tarjeta_ExigeLosCuatroDigitos()
    {
        var venta = new Venta();
        var pago = new PagoTarjeta { Monto = 5000, UltimosDigitos = "12" };

        Assert.Throws<VentaInvalidaException>(
            () => venta.AgregarPago(pago));
    }

    [Fact]
    public void Anular_SoloSirveConVentasCerradas()
    {
        var venta = new Venta();

        Assert.Throws<VentaInvalidaException>(() => venta.Anular());
    }
}
