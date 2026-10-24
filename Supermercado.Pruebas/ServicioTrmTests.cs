using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioTrmTests
{
    private static readonly RelojFijo Reloj =
        new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    private static ParametrosFalsos Parametros(int reintentos = 1)
    {
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.Reintentos] = reintentos.ToString();
        parametros.Valores[ClavesParametro.TimeoutSegundos] = "5";
        parametros.Valores[ClavesParametro.TrmUltimaCentavos] = "0";
        parametros.Valores[ClavesParametro.TrmUltimaFecha] = "1970-01-01";
        return parametros;
    }

    [Fact]
    public async Task SiLaApiResponde_GuardaElValorComoRespaldo()
    {
        var cliente = new ClienteTrmFalso
        {
            Respuesta = new Trm(401234, new DateOnly(2026, 10, 17))
        };
        var parametros = Parametros();

        var trm = await new ServicioTrm(cliente, parametros, Reloj)
            .ObtenerAsync();

        Assert.Equal(401234L, trm!.Centavos);
        var guardado = parametros.Valores[ClavesParametro.TrmUltimaCentavos];
        Assert.Equal("401234", guardado);
    }

    [Fact]
    public async Task SiLaApiFalla_ReintentaYUsaElRespaldo()
    {
        var cliente = new ClienteTrmFalso();
        var parametros = Parametros(reintentos: 1);
        parametros.Valores[ClavesParametro.TrmUltimaCentavos] = "400000";
        parametros.Valores[ClavesParametro.TrmUltimaFecha] = "2026-10-16";

        var trm = await new ServicioTrm(cliente, parametros, Reloj)
            .ObtenerAsync();

        Assert.Equal(2, cliente.Llamadas);
        Assert.Equal(400000L, trm!.Centavos);
        Assert.Equal(new DateOnly(2026, 10, 16), trm.Fecha);
    }

    [Fact]
    public async Task SinApiNiRespaldo_DevuelveNullEnVezDeFallar()
    {
        var servicio = new ServicioTrm(
            new ClienteTrmFalso(), Parametros(), Reloj);

        Assert.Null(await servicio.ObtenerAsync());
    }
}
