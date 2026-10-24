using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Xunit;

namespace Supermercado.Pruebas;

public class NominaTests
{
    private static readonly DateTime Ahora
        = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    // Cifras inventadas solo para la prueba; las reales van en la base.
    private static ParametrosNomina Parametros(long salarioMinimo = 1_000_000)
        => new(salarioMinimo, 150_000, 2, 400, 400);

    [Fact]
    public void Calcula_AuxilioDeduccionesYNeto()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        var nomina = Nomina.Calcular(empleado, 2026, 10, Parametros(), Ahora);

        Assert.Equal(150_000L, nomina.AuxilioTransporte);
        Assert.Equal(60_000L, nomina.DeduccionSalud);
        Assert.Equal(60_000L, nomina.DeduccionPension);
        Assert.Equal(1_530_000L, nomina.Neto);
    }

    [Fact]
    public void SinAuxilioPorEncimaDelTope()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 3_000_000 };

        var nomina = Nomina.Calcular(empleado, 2026, 10, Parametros(), Ahora);

        Assert.Equal(0L, nomina.AuxilioTransporte);
    }

    [Fact]
    public void ExigeQueElSalarioMinimoEsteConfigurado()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ParametroFaltanteException>(
            () => Nomina.Calcular(empleado, 2026, 10, Parametros(0), Ahora));
    }

    [Fact]
    public void RechazaUnMesInvalido()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 2026, 13, Parametros(), Ahora));
    }

    [Fact]
    public void RechazaUnAnioInvalido()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 0, 10, Parametros(), Ahora));
    }

    [Fact]
    public void RechazaParametrosNegativos()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };
        var negativos = new ParametrosNomina(
            1_000_000, -1, 2, 400, 400);

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 2026, 10, negativos, Ahora));
    }
}
