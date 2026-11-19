using System.Net;
using System.Text;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas;

public sealed class AsistenteTests
{
    [Theory]
    [InlineData("/stock", HerramientaAsistente.StockBajo)]
    [InlineData("/ventas", HerramientaAsistente.VentasDelDia)]
    [InlineData("/caja", HerramientaAsistente.EstadoCaja)]
    [InlineData("/resumen", HerramientaAsistente.ResumenDiario)]
    [InlineData("/stock@sigsbot", HerramientaAsistente.StockBajo)]
    public async Task ComandoFijo_NoConsultaGemini(
        string texto,
        HerramientaAsistente esperada)
    {
        var gemini = new GeminiFalso();
        var asistente = new Asistente(gemini);

        var solicitud = await asistente.InterpretarAsync(texto);

        Assert.Equal(esperada, solicitud?.Herramienta);
        Assert.Equal(0, gemini.Llamadas);
    }

    [Fact]
    public async Task LenguajeNatural_SeEnvíaAGemini()
    {
        var gemini = new GeminiFalso
        {
            Resultado = new SolicitudHerramienta(HerramientaAsistente.StockBajo)
        };
        var asistente = new Asistente(gemini);

        var solicitud = await asistente.InterpretarAsync("¿Qué productos están por agotarse?");

        Assert.Equal(HerramientaAsistente.StockBajo, solicitud?.Herramienta);
        Assert.Equal(1, gemini.Llamadas);
    }

    [Fact]
    public async Task ComandoDesconocido_NoSeEnvíaAGemini()
    {
        var gemini = new GeminiFalso();
        var asistente = new Asistente(gemini);

        var solicitud = await asistente.InterpretarAsync("/admin");

        Assert.Null(solicitud);
        Assert.Equal(0, gemini.Llamadas);
    }

    [Fact]
    public async Task ClienteGemini_IgnoraHerramientaNoPermitida()
    {
        const string respuesta = """
            {"candidates":[{"content":{"parts":[{"functionCall":{"name":"consultar_base_datos","args":{}}}]}}]}
            """;
        var handler = new HandlerFalso(respuesta);
        var cliente = new ClienteGeminiHttp(
            new HttpClient(handler), "clave-prueba", "gemini-prueba");

        var resultado = await cliente.ElegirHerramientaAsync("consulta inválida");

        Assert.Null(resultado);
        Assert.Contains("stock_bajo", handler.Cuerpo!);
        Assert.Contains("systemInstruction", handler.Cuerpo!);
        Assert.Contains("functionDeclarations", handler.Cuerpo!);
        Assert.Contains("functionCallingConfig", handler.Cuerpo!);
        Assert.DoesNotContain("consultar_base_datos", handler.Cuerpo!);
        Assert.Equal("clave-prueba", handler.ClaveApi);
        Assert.DoesNotContain("clave-prueba", handler.Url!.ToString());
    }

    [Fact]
    public async Task ClienteGemini_CuotaAgotadaDaMensajeUsable()
    {
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso("{}", HttpStatusCode.TooManyRequests)),
            "clave-prueba",
            "gemini-prueba");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("cuota", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/stock", error.Message);
    }

    private sealed class GeminiFalso : IClienteGemini
    {
        public int Llamadas { get; private set; }
        public SolicitudHerramienta? Resultado { get; init; }

        public Task<SolicitudHerramienta?> ElegirHerramientaAsync(
            string mensaje,
            CancellationToken cancelacion = default)
        {
            Llamadas++;
            return Task.FromResult(Resultado);
        }
    }

    private sealed class HandlerFalso(
        string contenido,
        HttpStatusCode codigo = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Cuerpo { get; private set; }
        public Uri? Url { get; private set; }
        public string? ClaveApi { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Cuerpo = await request.Content!.ReadAsStringAsync(cancellationToken);
            Url = request.RequestUri;
            ClaveApi = request.Headers.GetValues("x-goog-api-key").SingleOrDefault();
            return new HttpResponseMessage(codigo)
            {
                Content = new StringContent(contenido, Encoding.UTF8, "application/json")
            };
        }
    }
}
