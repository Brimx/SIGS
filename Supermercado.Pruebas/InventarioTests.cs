using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;
using Xunit;

namespace Supermercado.Pruebas;

public class InventarioTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 17);

    private static StockBodega Bodega(int loteId, int dias, int cantidad)
    {
        var stock = new StockBodega
        {
            LoteId = loteId,
            Lote = new Lote
            {
                Id = loteId,
                FechaVencimiento = Hoy.AddDays(dias)
            }
        };
        stock.Aumentar(cantidad);
        return stock;
    }

    [Fact]
    public void Fefo_SacaPrimeroElQueVenceAntesYSaltaLosVencidos()
    {
        var filas = new[]
        {
            Bodega(1, 20, 5),
            Bodega(2, 5, 5),
            Bodega(3, -1, 5),
        };

        var orden = new SalidaFefo().Ordenar(filas, Hoy);

        Assert.Equal(new[] { 2, 1 }, orden.Select(s => s.LoteId));
    }

    [Fact]
    public void Plan_RepartePrimeroDelLoteQueVenceAntes()
    {
        var filas = new List<StockBodega>
        {
            Bodega(1, 5, 3),
            Bodega(2, 20, 10),
        };

        var plan = PlanDeSalida.Repartir(filas, 5);

        Assert.Equal(new[] { 3, 2 }, plan.Select(p => p.Tomar));
    }

    [Fact]
    public void Plan_FallaSiNoAlcanzaElStock()
    {
        var filas = new List<StockBodega> { Bodega(1, 5, 3) };

        Assert.Throws<StockInsuficienteException>(
            () => PlanDeSalida.Repartir(filas, 4));
    }

    [Fact]
    public void Stock_NoQuedaEnNegativo()
    {
        var stock = new StockEstante { LoteId = 1 };
        stock.Aumentar(2);

        Assert.Throws<StockInsuficienteException>(
            () => stock.Disminuir(3));
    }

    [Fact]
    public void Reabastecimiento_SoloSeAtiendeUnaVez()
    {
        var solicitud = new Reabastecimiento { CantidadSolicitada = 10 };
        var ahora = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        solicitud.Atender(ahora);

        Assert.Equal(EstadoReabastecimiento.Atendido, solicitud.Estado);
        Assert.Equal(ahora, solicitud.FechaAtencion);
        Assert.Throws<ValidacionException>(
            () => solicitud.Atender(ahora));
    }

    [Fact]
    public void Reabastecimiento_Cancelado_NoSePuedeAtender()
    {
        var solicitud = new Reabastecimiento { CantidadSolicitada = 10 };

        solicitud.Cancelar();

        Assert.Equal(EstadoReabastecimiento.Cancelado, solicitud.Estado);
        Assert.Throws<ValidacionException>(
            () => solicitud.Atender(DateTime.UtcNow));
    }
}
