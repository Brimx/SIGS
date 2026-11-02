using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ApisPorDefectoTests
{
    private static readonly string[] Variables =
    [
        "SIGS_APIS_HABILITADAS", "SIGS_TRM_HABILITADA", "SIGS_OFF_HABILITADA",
        "SIGS_TRM_URL", "SIGS_OFF_URL", "SIGS_EMAIL_VERIFIER_URL"
    ];

    private static OpcionesApisExternas ConEntorno(
        params (string Variable, string Valor)[] valores)
    {
        var anteriores = Variables.ToDictionary(
            v => v, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var v in Variables)
                Environment.SetEnvironmentVariable(v, null);
            foreach (var (variable, valor) in valores)
                Environment.SetEnvironmentVariable(variable, valor);

            return OpcionesApisExternas.DesdeEntorno();
        }
        finally
        {
            foreach (var (v, valor) in anteriores)
                Environment.SetEnvironmentVariable(v, valor);
        }
    }

    [Fact]
    public void SinVariables_TrmYCodigoBarrasVienenActivas()
    {
        var o = ConEntorno();

        Assert.Equal(OpcionesApisExternas.TrmUrlPorDefecto, o.TrmUrl);
        Assert.Equal(
            OpcionesApisExternas.CodigoBarrasUrlPorDefecto,
            o.CodigoBarrasBaseUrl);
        Assert.Null(o.VerificadorCorreoUrl);
    }

    [Fact]
    public void InterruptorGeneral_ApagaTodas()
    {
        var o = ConEntorno(("SIGS_APIS_HABILITADAS", "0"));

        Assert.Null(o.TrmUrl);
        Assert.Null(o.CodigoBarrasBaseUrl);
    }

    [Fact]
    public void InterruptorIndividual_ApagaSoloUna()
    {
        var o = ConEntorno(("SIGS_OFF_HABILITADA", "false"));

        Assert.NotNull(o.TrmUrl);
        Assert.Null(o.CodigoBarrasBaseUrl);
    }

    [Fact]
    public void LaUrlSePuedeReemplazar()
    {
        var o = ConEntorno(("SIGS_TRM_URL", "https://trm.local/x"));

        Assert.Equal("https://trm.local/x", o.TrmUrl);
    }

    [Fact]
    public async Task ParametroApagado_ElClienteDeCodigosNoSale()
    {
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.CodigoBarrasHabilitado] = "0";
        parametros.Valores[ClavesParametro.TimeoutSegundos] = "5";
        var llamadas = 0;
        var http = new HttpClient(new ManejadorContador(() => llamadas++));
        var cliente = new ClienteCodigoBarrasOff(
            http,
            new OpcionesApisExternas { CodigoBarrasBaseUrl = "https://off.test" },
            parametros);

        var info = await cliente.ConsultarAsync("123", CancellationToken.None);

        Assert.Null(info);
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task ObtenerBool_SinParametro_UsaElValorPorDefecto()
    {
        var parametros = new ParametrosFalsos();

        Assert.True(await parametros.ObtenerBoolAsync("x.y"));
        Assert.False(await parametros.ObtenerBoolAsync("x.y", false));
    }

    private class ManejadorContador(Action alLlamar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            alLlamar();
            return Task.FromResult(new HttpResponseMessage());
        }
    }
}
