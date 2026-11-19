using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task ClienteGemini_EnvíaCuerpoConConfiguraciónEstructurada()
    {
        const string respuesta = """{"candidates":[]}""";
        var handler = new HandlerFalso(respuesta);
        var cliente = new ClienteGeminiHttp(
            new HttpClient(handler), "clave-prueba", "gemini-prueba");

        await cliente.ElegirHerramientaAsync("consulta natural");

        using var json = JsonDocument.Parse(handler.Cuerpo!);
        var raiz = json.RootElement;
        Assert.Equal(
            "LOW",
            raiz.GetProperty("generationConfig")
                .GetProperty("thinkingConfig")
                .GetProperty("thinkingLevel")
                .GetString());
        var texto = raiz.GetProperty("generationConfig")
            .GetProperty("responseFormat")
            .GetProperty("text");
        Assert.Equal("APPLICATION_JSON", texto.GetProperty("mimeType").GetString());
        Assert.Equal("OBJECT", texto.GetProperty("schema").GetProperty("type").GetString());
        Assert.True(texto.GetProperty("schema")
            .GetProperty("properties")
            .TryGetProperty("herramienta", out _));
        Assert.Equal(
            "VALIDATED",
            raiz.GetProperty("toolConfig")
                .GetProperty("functionCallingConfig")
                .GetProperty("mode")
                .GetString());
        var declaraciones = raiz.GetProperty("tools")[0]
            .GetProperty("functionDeclarations");
        foreach (var declaracion in declaraciones.EnumerateArray())
        {
            Assert.Equal(
                "OBJECT",
                declaracion.GetProperty("parameters").GetProperty("type").GetString());
        }
    }

    [Fact]
    public async Task ClienteGemini_AceptaRespuestaEstructuradaSinFunctionCall()
    {
        const string respuesta = """
            {"candidates":[{"content":{"parts":[{"text":"{\"herramienta\":\"stock_bajo\"}"}]}}]}
            """;
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso(respuesta)),
            "clave-prueba",
            "gemini-prueba");

        var resultado = await cliente.ElegirHerramientaAsync("consulta natural");

        Assert.Equal(HerramientaAsistente.StockBajo, resultado?.Herramienta);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ClienteGemini_ErrorDeConfiguracionDaMensajeEspecifico(
        HttpStatusCode codigo)
    {
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso("{}", codigo)),
            "clave-prueba",
            "gemini-prueba");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("configuración", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/stock", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ClienteGemini_ClaveRechazadaDaMensajeEspecifico(
        HttpStatusCode codigo)
    {
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso("{}", codigo)),
            "clave-prueba",
            "gemini-prueba");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("clave", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClienteGemini_ErrorTemporalDaMensajeUsable()
    {
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso("{}", HttpStatusCode.InternalServerError)),
            "clave-prueba",
            "gemini-prueba");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("temporalmente", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/stock", error.Message);
    }

    [Fact]
    public async Task ClienteGemini_ReintentaConModeloDeRespaldoCuandoServicioNoDisponible()
    {
        const string respuesta = """
            {"candidates":[{"content":{"parts":[{"functionCall":{"name":"stock_bajo","args":{}}}]}}]}
            """;
        var handler = new HandlerFalso(
            ("{}", HttpStatusCode.ServiceUnavailable),
            (respuesta, HttpStatusCode.OK));
        var cliente = new ClienteGeminiHttp(
            new HttpClient(handler),
            "clave-prueba",
            "gemini-prueba",
            "gemini-respaldo");

        var resultado = await cliente.ElegirHerramientaAsync("consulta natural");

        Assert.Equal(HerramientaAsistente.StockBajo, resultado?.Herramienta);
        Assert.Equal(2, handler.Urls.Count);
        Assert.Contains("gemini-prueba", handler.Urls[0].ToString());
        Assert.Contains("gemini-respaldo", handler.Urls[1].ToString());
    }

    [Fact]
    public async Task ClienteGemini_SinRespaldoServiceUnavailableDaMensajeDeSaturacion()
    {
        var handler = new HandlerFalso("{}", HttpStatusCode.ServiceUnavailable);
        var cliente = new ClienteGeminiHttp(
            new HttpClient(handler),
            "clave-prueba",
            "gemini-prueba",
            modeloRespaldo: "");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("saturado", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(handler.Urls);
    }

    [Fact]
    public async Task ClienteGemini_RespaldoAgotadoDaMensajeDeSaturacion()
    {
        var handler = new HandlerFalso(
            ("{}", HttpStatusCode.ServiceUnavailable),
            ("{}", HttpStatusCode.ServiceUnavailable));
        var cliente = new ClienteGeminiHttp(
            new HttpClient(handler),
            "clave-prueba",
            "gemini-prueba",
            "gemini-respaldo");

        var error = await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        Assert.Contains("saturado", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, handler.Urls.Count);
    }

    [Fact]
    public async Task ClienteGemini_RegistraErrorSinExponerClaveNiDatosSensibles()
    {
        const string respuesta = """
            {"error":{"code":400,"status":"INVALID_ARGUMENT","message":"clave-prueba rechazada para cliente@example.com","prompt":"dato sensible"}}
            """;
        var registro = new RegistroFalso();
        var cliente = new ClienteGeminiHttp(
            new HttpClient(new HandlerFalso(respuesta, HttpStatusCode.BadRequest)),
            "clave-prueba",
            "gemini-prueba",
            logger: registro);

        await Assert.ThrowsAsync<GeminiNoDisponibleException>(
            () => cliente.ElegirHerramientaAsync("consulta natural"));

        var mensaje = Assert.Single(registro.Mensajes);
        Assert.Contains("400", mensaje);
        Assert.Contains("INVALID_ARGUMENT", mensaje);
        Assert.Contains(Rojo, mensaje);
        Assert.DoesNotContain("clave-prueba", mensaje);
        Assert.DoesNotContain("cliente@example.com", mensaje);
        Assert.DoesNotContain("dato sensible", mensaje);
    }

    private const string Rojo = "[redactado]";

    private sealed class RegistroFalso : ILogger<ClienteGeminiHttp>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => new AlcanceFalso();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Mensajes.Add(formatter(state, exception));
        }

        private sealed class AlcanceFalso : IDisposable
        {
            public void Dispose()
            {
            }
        }
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

    private sealed class HandlerFalso : HttpMessageHandler
    {
        private readonly Queue<(string Contenido, HttpStatusCode Codigo)>? _respuestas;
        private readonly string? _contenido;
        private readonly HttpStatusCode _codigo;

        public HandlerFalso(string contenido, HttpStatusCode codigo = HttpStatusCode.OK)
        {
            _contenido = contenido;
            _codigo = codigo;
        }

        public HandlerFalso(params (string Contenido, HttpStatusCode Codigo)[] respuestas)
        {
            _respuestas = new Queue<(string, HttpStatusCode)>(respuestas);
        }

        public string? Cuerpo { get; private set; }
        public Uri? Url { get; private set; }
        public string? ClaveApi { get; private set; }
        public List<Uri> Urls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Cuerpo = await request.Content!.ReadAsStringAsync(cancellationToken);
            Url = request.RequestUri;
            Urls.Add(Url!);
            ClaveApi = request.Headers.GetValues("x-goog-api-key").SingleOrDefault();
            if (_respuestas is null)
                return NuevaRespuesta(_contenido!, _codigo);

            var siguiente = _respuestas.Dequeue();
            return NuevaRespuesta(siguiente.Contenido, siguiente.Codigo);
        }

        private static HttpResponseMessage NuevaRespuesta(
            string contenido,
            HttpStatusCode codigo)
            => new(codigo)
            {
                Content = new StringContent(contenido, Encoding.UTF8, "application/json")
            };
    }
}

