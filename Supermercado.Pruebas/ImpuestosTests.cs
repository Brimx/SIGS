using Supermercado.Entidades.Finanzas;
using Xunit;

namespace Supermercado.Pruebas;

public class ImpuestosTests
{
    [Fact]
    public void Iva_SeExtraeDelPrecioQueYaLoIncluye()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(1900L, iva.Calcular(11900));
    }

    [Fact]
    public void Iva_RedondeaAlPesoMasCercano()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(479L, iva.Calcular(3000));
    }

    [Fact]
    public void Iva_CincoPorCiento()
    {
        var iva = new Iva { PuntosBasicos = 500 };

        Assert.Equal(500L, iva.Calcular(10500));
    }

    [Fact]
    public void Iva_ExentoNoCobraNada()
    {
        var iva = new Iva { PuntosBasicos = 0 };

        Assert.Equal(0L, iva.Calcular(10000));
    }

    [Fact]
    public void Retencion_NoAplicaPorDebajoDeLaBaseMinima()
    {
        var retencion = new Retencion
        {
            PuntosBasicos = 250,
            BaseMinima = 1_000_000
        };

        Assert.Equal(0L, retencion.Calcular(999_999));
    }

    [Fact]
    public void Retencion_AplicaLaTarifaSobreLaBase()
    {
        var retencion = new Retencion
        {
            PuntosBasicos = 250,
            BaseMinima = 1_000_000
        };

        Assert.Equal(25000L, retencion.Calcular(1_000_000));
    }

    [Fact]
    public void Porcentaje_SeMuestraDesdeLosPuntosBasicos()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(19m, iva.Porcentaje);
    }
}
