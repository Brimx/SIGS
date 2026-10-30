using System.Globalization;
using System.Net;
using System.Text;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ClientesExternosTests
{
    private static IServicioParametros CrearParametros()
    {
        var p = new ParametrosFalsos();
        p.Valores[ClavesParametro.TimeoutSegundos] = "2";
        p.Valores[ClavesParametro.Reintentos] = "1";
        return p;
    }

    [Fact]
    public async Task TrmDatosGov_ParseaCentavosConCulturaInvariante()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-CO");
            var json = """
                [
                  {
                    "valor": "4150.75",
                    "unidad": "COP",
                    "vigenciadesde": "2026-04-20T00:00:00.000"
                  }
                ]
                """;
            var handler = new HandlerFalso(_ => new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8)
            });
            var opciones = new OpcionesApisExternas
            {
                TrmUrl = "https://trm.test/recurso.json",
                TrmAppToken = "token-123"
            };
            var cliente = new ClienteTrmDatosGov(
                new HttpClient(handler), opciones);

            var trm = await cliente.ObtenerAsync(CancellationToken.None);

            Assert.Equal(415075L, trm.Centavos);
            Assert.Equal(new DateOnly(2026, 4, 20), trm.Fecha);
            Assert.Equal("token-123", handler.UltimoTokenApp);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
        }
    }

    [Fact]
    public async Task TrmDatosGov_SinUrl_LanzaServicioNoConfigurado()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.OK));
        var cliente = new ClienteTrmDatosGov(
            new HttpClient(handler), new OpcionesApisExternas());

        await Assert.ThrowsAsync<ServicioNoConfiguradoException>(
            () => cliente.ObtenerAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CodigoBarrasOff_ProductoEncontrado_MapeaDatos()
    {
        var json = """
            {
              "status": 1,
              "product": {
                "product_name": "Café Molido 500g",
                "brands": "Sello Rojo",
                "categories": "Bebidas, Café"
              }
            }
            """;
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8)
        });
        var opciones = new OpcionesApisExternas
        {
            CodigoBarrasBaseUrl = "https://off.test"
        };
        var cliente = new ClienteCodigoBarrasOff(
            new HttpClient(handler), opciones, CrearParametros());

        var info = await cliente.ConsultarAsync(
            "7701234567890", CancellationToken.None);

        Assert.NotNull(info);
        Assert.Equal("Café Molido 500g", info.Nombre);
        Assert.Equal("Sello Rojo", info.Marca);
        Assert.Equal("Bebidas, Café", info.Categoria);
    }

    [Fact]
    public async Task CodigoBarrasOff_NoEncontradoOFalloHttp_RetornaNull()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.InternalServerError));
        var opciones = new OpcionesApisExternas
        {
            CodigoBarrasBaseUrl = "https://off.test"
        };
        var cliente = new ClienteCodigoBarrasOff(
            new HttpClient(handler), opciones, CrearParametros());

        var info = await cliente.ConsultarAsync(
            "000000", CancellationToken.None);

        Assert.Null(info);
    }

    [Fact]
    public async Task VerificadorCorreo_RespuestasYFailOpen_Funciona()
    {
        var respuestaJson = """{"is_reachable": "invalid"}""";
        var codigoHttp = HttpStatusCode.OK;
        var handler = new HandlerFalso(_ => new HttpResponseMessage(codigoHttp)
        {
            Content = new StringContent(respuestaJson, Encoding.UTF8)
        });
        var opciones = new OpcionesApisExternas
        {
            VerificadorCorreoUrl = "https://mail.test/check"
        };
        var cliente = new ClienteVerificadorCorreoHttp(
            new HttpClient(handler), opciones, CrearParametros());

        Assert.False(await cliente.VerificarAsync("sin-arroba"));
        Assert.False(await cliente.VerificarAsync("falso@dominio.com"));

        respuestaJson = """{"is_reachable": "safe"}""";
        Assert.True(await cliente.VerificarAsync("real@dominio.com"));

        codigoHttp = HttpStatusCode.ServiceUnavailable;
        Assert.True(await cliente.VerificarAsync("real@dominio.com"));
    }

    private sealed class HandlerFalso(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public string? UltimoTokenApp { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-App-Token", out var vals))
                UltimoTokenApp = vals.FirstOrDefault();

            return Task.FromResult(responder(request));
        }
    }
}
