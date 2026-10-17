using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;
using Xunit;

namespace Supermercado.Pruebas;

public class InventarioTests
{
    private static Lote CrearLote(string numero, DateOnly vence)
        => new() { NumeroLote = numero, FechaVencimiento = vence };

    [Fact]
    public void Fefo_SacaPrimeroElQueVenceAntes()
    {
        var hoy = new DateOnly(2026, 10, 17);
        var lotes = new[]
        {
            CrearLote("B", hoy.AddDays(20)),
            CrearLote("A", hoy.AddDays(5)),
            CrearLote("C", hoy.AddDays(-1)),
        };

        var orden = new SalidaFefo().Ordenar(lotes, hoy).ToList();

        Assert.Equal(new[] { "A", "B" }, orden.Select(l => l.NumeroLote));
    }

    [Fact]
    public void Lote_MueveDeBodegaAEstante()
    {
        var lote = new Lote { CantidadEnBodega = 10 };

        lote.MoverAEstante(4);

        Assert.Equal(6, lote.CantidadEnBodega);
        Assert.Equal(4, lote.CantidadEnEstante);
    }

    [Fact]
    public void Lote_NoMueveMasDeLoQueHayEnBodega()
    {
        var lote = new Lote { CantidadEnBodega = 5 };

        Assert.Throws<StockInsuficienteException>(
            () => lote.MoverAEstante(6));
    }
}
