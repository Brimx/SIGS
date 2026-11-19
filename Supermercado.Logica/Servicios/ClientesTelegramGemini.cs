using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Supermercado.Logica.Servicios;


public sealed class ClienteGeminiHttp(
    HttpClient http,
    string? clave = null,
    string? modelo = null,
    ILogger<ClienteGeminiHttp>? logger = null)
    : IClienteGemini
{
    public const string ModeloPorDefecto = "gemini-3.8-flash";
    private const string Rojo = "[redactado]";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> CamposSensibles = new(StringComparer.OrdinalIgnoreCase)
    {
        "apiKey", "authorization", "args", "chatId", "chat_id", "clave", "contents",
        "cookie", "documento", "email", "functionCall", "functionResponse", "headers",
        "id", "motivo", "password", "phone", "prompt", "secret", "systemInstruction",
        "telefono", "text", "token", "usuarioId", "userId", "x-goog-api-key"
    };
    private readonly string? _clave = (clave ?? Environment.GetEnvironmentVariable("SIGS_GEMINI_API_KEY"))?.Trim();
    private readonly string _modelo = LeerModelo(modelo);
    private readonly ILogger<ClienteGeminiHttp>? _logger = logger;

    private static string LeerModelo(string? modelo)

    {
        var configurado = string.IsNullOrWhiteSpace(modelo)
            ? Environment.GetEnvironmentVariable("SIGS_GEMINI_MODEL")
            : modelo;
        return string.IsNullOrWhiteSpace(configurado)
            ? ModeloPorDefecto
            : configurado.Trim();
    }

    private async Task<HttpResponseMessage> EnviarGeminiAsync(string uri, object cuerpo, CancellationToken cancelacion)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, uri);
        solicitud.Headers.TryAddWithoutValidation("x-goog-api-key", _clave);
        solicitud.Content = JsonContent.Create(cuerpo, options: Json);
        try
        {
            return await http.SendAsync(solicitud, cancelacion);
        }
        catch (HttpRequestException)
        {
            throw new GeminiNoDisponibleException("Gemini no está disponible por un problema de red. Los comandos fijos siguen disponibles.");
        }
        catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            throw new GeminiNoDisponibleException("Gemini agotó el tiempo de espera. Los comandos fijos siguen disponibles.");
        }
    }

    private static string MensajeError(HttpStatusCode estado)
        => estado switch
        {
            HttpStatusCode.TooManyRequests =>
                "Gemini alcanzó el límite de cuota. Intenta más tarde o usa /stock, /ventas, /caja o /resumen.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Gemini rechazó la clave o no tiene cuota disponible. Los comandos /stock, /ventas, /caja y /resumen siguen disponibles.",
            _ when (int)estado is >= 400 and < 500 =>
                "Gemini rechazó la solicitud por configuración o formato. Avisa al administrador. Los comandos /stock, /ventas, /caja y /resumen siguen disponibles.",
            _ => "Gemini no está disponible temporalmente. Los comandos /stock, /ventas, /caja y /resumen siguen disponibles."
        };

    private async Task RegistrarErrorAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancelacion)
    {
        var cuerpo = await LeerCuerpoErrorAsync(respuesta, cancelacion);
        _logger?.LogWarning(
            "Gemini respondió {StatusCode}. Cuerpo saneado: {CuerpoSaneado}",
            (int)respuesta.StatusCode,
            cuerpo);
    }

    private async Task<string> LeerCuerpoErrorAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancelacion)
    {
        string cuerpo;
        try
        {
            cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);
        }
        catch (TaskCanceledException) when (cancelacion.IsCancellationRequested)
        {
            return "(sin cuerpo)";
        }
        catch (HttpRequestException)
        {
            return "(sin cuerpo)";
        }

        return string.IsNullOrWhiteSpace(cuerpo)
            ? "(sin cuerpo)"
            : SanitizarCuerpo(cuerpo);
    }

    private string SanitizarCuerpo(string cuerpo)
    {
        try
        {
            using var json = JsonDocument.Parse(cuerpo);
            using var flujo = new MemoryStream();
            using (var escritor = new Utf8JsonWriter(flujo))
            {
                EscribirSanitizado(json.RootElement, escritor);
            }

            return Recortar(Encoding.UTF8.GetString(flujo.ToArray()));
        }
        catch (JsonException)
        {
            return Recortar(SanitizarTexto(cuerpo, _clave));
        }
    }

    private void EscribirSanitizado(JsonElement elemento, Utf8JsonWriter escritor)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                escritor.WriteStartObject();
                foreach (var propiedad in elemento.EnumerateObject())
                {
                    escritor.WritePropertyName(propiedad.Name);
                    if (CamposSensibles.Contains(propiedad.Name))
                        escritor.WriteStringValue(Rojo);
                    else
                        EscribirSanitizado(propiedad.Value, escritor);
                }
                escritor.WriteEndObject();
                break;
            case JsonValueKind.Array:
                escritor.WriteStartArray();
                foreach (var hijo in elemento.EnumerateArray())
                    EscribirSanitizado(hijo, escritor);
                escritor.WriteEndArray();
                break;
            case JsonValueKind.String:
                escritor.WriteStringValue(
                    SanitizarTexto(elemento.GetString() ?? string.Empty, _clave));
                break;
            default:
                elemento.WriteTo(escritor);
                break;
        }
    }

    private static string SanitizarTexto(string texto, string? clave)
    {
        var resultado = string.IsNullOrWhiteSpace(clave)
            ? texto
            : texto.Replace(clave, Rojo, StringComparison.OrdinalIgnoreCase);
        resultado = Regex.Replace(resultado, @"AIza[0-9A-Za-z_-]{20,}", Rojo);
        resultado = Regex.Replace(
            resultado,
            @"(?i)\b(?:bearer|api[_-]?key|token)\s*[:=]?\s*[A-Za-z0-9_-]{16,}",
            Rojo);
        resultado = Regex.Replace(resultado, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", "[correo]");
        resultado = Regex.Replace(resultado, @"\+?\d[\d\s().-]{6,}\d", "[numero]");
        return resultado;
    }

    private static string Recortar(string texto)
        => texto.Length <= 512 ? texto : texto[..512] + "…";

    public async Task<SolicitudHerramienta?> ElegirHerramientaAsync(
        string mensaje,

        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(_clave))
            throw new GeminiNoDisponibleException("Gemini no está configurado. Los comandos /stock, /ventas, /caja y /resumen siguen disponibles.");

        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_modelo)}:generateContent";
        var cuerpo = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = "Eres un asistente de un sistema de supermercado. Solo seleccionas una herramienta declarada. Nunca inventes datos ni afirmes que ejecutaste una acción. Si la solicitud no corresponde a una herramienta, responde sin llamar herramientas." }
                }
            },
            contents = new[] { new { role = "user", parts = new[] { new { text = mensaje } } } },
            tools = new[]
            {
                new
                {
                    functionDeclarations = new object[]
                    {
                        new { name = "stock_bajo", description = "Consultar productos con stock bajo." },
                        new { name = "ventas_del_dia", description = "Consultar ventas del día." },
                        new { name = "estado_caja", description = "Consultar el estado de caja del usuario vinculado." },
                        new { name = "resumen_diario", description = "Consultar el resumen operativo del día." },
                        new { name = "solicitar_anulacion", description = "Solicitar aprobación de anulación de una venta.", parameters = new { type = "OBJECT", properties = new { id = new { type = "INTEGER" }, motivo = new { type = "STRING" } }, required = new[] { "id", "motivo" } } },
                        new { name = "generar_reabastecimiento", description = "Proponer solicitudes de reabastecimiento sujetas a confirmación." }
                    }
                }
            },
            toolConfig = new { functionCallingConfig = new { mode = "AUTO" } }
        };

        using var respuesta = await EnviarGeminiAsync(uri, cuerpo, cancelacion);
        if (!respuesta.IsSuccessStatusCode)
        {
            await RegistrarErrorAsync(respuesta, cancelacion);
            throw new GeminiNoDisponibleException(MensajeError(respuesta.StatusCode));
        }

        await using var flujo = await respuesta.Content.ReadAsStreamAsync(cancelacion);
        using var json = await JsonDocument.ParseAsync(flujo, cancellationToken: cancelacion);
        if (!json.RootElement.TryGetProperty("candidates", out var candidatos)

            || candidatos.GetArrayLength() == 0)
            return null;

        var partes = candidatos[0].GetProperty("content").GetProperty("parts");
        foreach (var parte in partes.EnumerateArray())
        {
            if (!parte.TryGetProperty("functionCall", out var llamada)
                || !llamada.TryGetProperty("name", out var nombreElemento))
                continue;

            var nombre = nombreElemento.GetString();
            if (!MapeoHerramientas.TryGetValue(nombre ?? string.Empty, out var herramienta))
                return null;

            var args = llamada.TryGetProperty("args", out var argumentos)
                ? argumentos
                : default;
            var id = args.ValueKind == JsonValueKind.Object
                && args.TryGetProperty("id", out var idElemento)
                && idElemento.TryGetInt32(out var valorId)
                    ? valorId
                    : (int?)null;
            var motivo = args.ValueKind == JsonValueKind.Object
                && args.TryGetProperty("motivo", out var motivoElemento)
                    ? motivoElemento.GetString()
                    : null;
            if (herramienta == HerramientaAsistente.SolicitarAnulacion
                && (id is null or <= 0 || string.IsNullOrWhiteSpace(motivo)))
                return null;

            return new SolicitudHerramienta(herramienta, id, motivo?.Trim());
        }

        return null;
    }

    private static readonly IReadOnlyDictionary<string, HerramientaAsistente> MapeoHerramientas =
        new Dictionary<string, HerramientaAsistente>(StringComparer.Ordinal)
        {
            ["stock_bajo"] = HerramientaAsistente.StockBajo,
            ["ventas_del_dia"] = HerramientaAsistente.VentasDelDia,
            ["estado_caja"] = HerramientaAsistente.EstadoCaja,
            ["resumen_diario"] = HerramientaAsistente.ResumenDiario,
            ["solicitar_anulacion"] = HerramientaAsistente.SolicitarAnulacion,
            ["generar_reabastecimiento"] = HerramientaAsistente.GenerarReabastecimiento
        };
}

public sealed class GeminiNoDisponibleException(string mensaje) : Exception(mensaje);

public sealed record BotonTelegram(string Texto, string CallbackData);
public sealed record ActualizacionTelegram(
    long Id,
    long? ChatId,
    long? UsuarioId,
    string? Texto,
    string? CallbackId,
    string? CallbackData,
    bool ChatPrivado);

public interface IClienteTelegram
{
    Task<IReadOnlyList<ActualizacionTelegram>> ObtenerActualizacionesAsync(
        long offset,
        CancellationToken cancelacion);
    Task EnviarMensajeAsync(
        long chatId,
        string texto,
        IReadOnlyList<BotonTelegram>? botones = null,
        CancellationToken cancelacion = default);
    Task ResponderCallbackAsync(
        string callbackId,
        string texto,
        CancellationToken cancelacion = default);
}

public sealed class ClienteTelegramHttp(HttpClient http, string? token = null)
    : IClienteTelegram
{
    private readonly string? _token = token
        ?? Environment.GetEnvironmentVariable("SIGS_TELEGRAM_BOT_TOKEN");

    public async Task<IReadOnlyList<ActualizacionTelegram>> ObtenerActualizacionesAsync(
        long offset,
        CancellationToken cancelacion)
    {
        using var respuesta = await EjecutarAsync(
            "getUpdates",
            new { offset, timeout = 20, allowed_updates = new[] { "message", "callback_query" } },
            cancelacion);
        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync(cancelacion));
        var actualizaciones = new List<ActualizacionTelegram>();

        foreach (var fila in documento.RootElement.GetProperty("result").EnumerateArray())
        {
            var id = fila.GetProperty("update_id").GetInt64();
            if (fila.TryGetProperty("message", out var mensaje))
            {
                actualizaciones.Add(new ActualizacionTelegram(
                    id,
                    mensaje.GetProperty("chat").GetProperty("id").GetInt64(),
                    mensaje.TryGetProperty("from", out var remitente)
                        ? remitente.GetProperty("id").GetInt64()
                        : null,
                    mensaje.TryGetProperty("text", out var texto) ? texto.GetString() : null,
                    null,
                    null,
                    mensaje.GetProperty("chat").GetProperty("type").GetString() == "private"));
            }
            else if (fila.TryGetProperty("callback_query", out var callback))
            {
                actualizaciones.Add(new ActualizacionTelegram(
                    id,
                    callback.GetProperty("message").GetProperty("chat").GetProperty("id").GetInt64(),
                    callback.GetProperty("from").GetProperty("id").GetInt64(),
                    null,
                    callback.GetProperty("id").GetString(),
                    callback.TryGetProperty("data", out var dato) ? dato.GetString() : null,
                    callback.GetProperty("message").GetProperty("chat").GetProperty("type").GetString() == "private"));
            }
        }

        return actualizaciones;
    }

    public async Task EnviarMensajeAsync(
        long chatId,
        string texto,
        IReadOnlyList<BotonTelegram>? botones = null,
        CancellationToken cancelacion = default)
    {
        object cuerpo = botones is null || botones.Count == 0
            ? new { chat_id = chatId, text = texto }
            : new
            {
                chat_id = chatId,
                text = texto,
                reply_markup = new
                {
                    inline_keyboard = botones
                        .Select(b => new[] { new { text = b.Texto, callback_data = b.CallbackData } })
                        .ToArray()
                }
            };
        using var respuesta = await EjecutarAsync("sendMessage", cuerpo, cancelacion);
    }

    public async Task ResponderCallbackAsync(
        string callbackId,
        string texto,
        CancellationToken cancelacion = default)
    {
        using var respuesta = await EjecutarAsync(
            "answerCallbackQuery",
            new { callback_query_id = callbackId, text = texto, show_alert = false },
            cancelacion);
    }

    private async Task<HttpResponseMessage> EjecutarAsync(
        string metodo,
        object cuerpo,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(_token))
            throw new InvalidOperationException("Telegram no está configurado.");

        var uri = $"https://api.telegram.org/bot{_token}/{metodo}";
        var respuesta = await http.PostAsJsonAsync(uri, cuerpo, cancelacion);
        if (!respuesta.IsSuccessStatusCode)
        {
            var codigo = respuesta.StatusCode;
            respuesta.Dispose();
            throw new HttpRequestException($"Telegram respondió {(int)codigo}.", null, codigo);
        }
        return respuesta;
    }
}
