using Supermercado.Entidades.Base;
using Xunit;

namespace Supermercado.Pruebas;

public class DineroTests
{
    [Fact]
    public void Dividir_RedondeaAlEnteroMasCercano()
    {
        Assert.Equal(3L, Dinero.DividirRedondeando(5, 2));
        Assert.Equal(2L, Dinero.DividirRedondeando(5, 3));
        Assert.Equal(1L, Dinero.DividirRedondeando(4, 3));
    }

    [Fact]
    public void Porcentaje_UsaPuntosBasicos()
    {
        Assert.Equal(25L, Dinero.Porcentaje(1000, 250));
    }
}
