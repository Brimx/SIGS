using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Xunit;

namespace Supermercado.Pruebas;

public class CajaTests
{
    private static readonly DateTime Ahora
        = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    private static Venta VentaEnEfectivo(long recibido)
    {
        var arroz = new ProductoUnitario
        {
            Id = 1,
            PrecioVenta = 3000,
            Impuesto = new Iva()
        };
        var venta = new Venta();
        venta.AgregarProducto(arroz, 10);
        venta.AgregarPago(new PagoEfectivo { Monto = recibido });
        venta.Cerrar();
        return venta;
    }

    [Fact]
    public void Abrir_RechazaUnEfectivoInicialNegativo()
    {
        Assert.Throws<TurnoInvalidoException>(
            () => TurnoCaja.Abrir(1, 1, -1, Ahora));
    }

    [Fact]
    public void Esperado_SumaElEfectivoNetoYDescuentaLasAnuladas()
    {
        var turno = TurnoCaja.Abrir(1, 1, 50000, Ahora);
        var buena = VentaEnEfectivo(35000);
        var anulada = VentaEnEfectivo(30000);
        anulada.Anular("error del cajero");

        var esperado = turno.CalcularEfectivoEsperado([buena, anulada]);

        Assert.Equal(50000L, esperado);
    }

    [Fact]
    public void Cerrar_GuardaLaDiferenciaDelArqueo()
    {
        var turno = TurnoCaja.Abrir(1, 1, 50000, Ahora);

        turno.Cerrar(Ahora, 80000, 79000);

        Assert.Equal(-1000L, turno.Diferencia);
        Assert.Equal(EstadoTurno.Cerrado, turno.Estado);
    }

    [Fact]
    public void Cerrar_NoSePuedeDosVeces()
    {
        var turno = TurnoCaja.Abrir(1, 1, 0, Ahora);
        turno.Cerrar(Ahora, 0, 0);

        Assert.Throws<TurnoInvalidoException>(
            () => turno.Cerrar(Ahora, 0, 0));
    }
}
