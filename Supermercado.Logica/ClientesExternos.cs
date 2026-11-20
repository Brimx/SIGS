using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

public class OpcionesApisExternas
{
    // Fuentes abiertas y gratuitas, activas por defecto (ver ADR-019).
    public const string TrmUrlPorDefecto =
        "https://www.datos.gov.co/resource/32sa-8pi3.json"
        + "?$limit=1&$order=vigenciadesde%20DESC";
    public const string CodigoBarrasUrlPorDefecto =
        "https://world.openfoodfacts.org";

    public string? TrmUrl { get; init; }
    public string? TrmAppToken { get; init; }
    public string? CodigoBarrasBaseUrl { get; init; }
    public string? VerificadorCorreoUrl { get; init; }
    public string? VerificadorCorreoToken { get; init; }

    public static OpcionesApisExternas DesdeEntorno()
    {
        var todas = Habilitada("SIGS_APIS_HABILITADAS");
        var trm = todas && Habilitada("SIGS_TRM_HABILITADA");
        var off = todas && Habilitada("SIGS_OFF_HABILITADA");

        return new OpcionesApisExternas
        {
            TrmUrl = trm
                ? Valor("SIGS_TRM_URL") ?? TrmUrlPorDefecto
                : null,
            TrmAppToken = Valor("SIGS_TRM_TOKEN"),
            CodigoBarrasBaseUrl = off
                ? Valor("SIGS_OFF_URL") ?? CodigoBarrasUrlPorDefecto
                : null,
            VerificadorCorreoUrl = todas
                ? Valor("SIGS_EMAIL_VERIFIER_URL")
                : null,
            VerificadorCorreoToken = Valor("SIGS_EMAIL_VERIFIER_TOKEN")
        };
    }

    private static string? Valor(string variable)
    {
        var valor = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static bool Habilitada(string variable)
        => Valor(variable)?.ToLowerInvariant() is not ("0" or "false" or "no");
}

// ==========================================
// TRM (Tasa Representativa del Mercado)
// ==========================================

public record Trm(long Centavos, DateOnly Fecha);

public interface ITrmClient
{
    Task<Trm> ObtenerAsync(CancellationToken ct);
}

public interface IServicioTrm
{
    // Devuelve el último valor guardado si la consulta falla.
    Task<Trm?> ObtenerAsync(CancellationToken ct = default);
}

public class ClienteTrmNoConfigurado : ITrmClient
{
    public Task<Trm> ObtenerAsync(CancellationToken ct)
        => throw new ServicioNoConfiguradoException("TRM");
}

public class ClienteTrmDatosGov(
    HttpClient http,
    OpcionesApisExternas opciones) : ITrmClient
{
    public async Task<Trm> ObtenerAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opciones.TrmUrl))
            throw new ServicioNoConfiguradoException("TRM");

        using var req = new HttpRequestMessage(HttpMethod.Get, opciones.TrmUrl);
        if (!string.IsNullOrWhiteSpace(opciones.TrmAppToken))
            req.Headers.TryAddWithoutValidation(
                "X-App-Token", opciones.TrmAppToken);

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Array
            || doc.RootElement.GetArrayLength() == 0)
            throw new InvalidOperationException("Respuesta TRM vacía.");

        var item = doc.RootElement[0];
        var valorStr = item.GetProperty("valor").GetString()
            ?? throw new InvalidOperationException("TRM sin valor.");
        var fechaStr = item.GetProperty("vigenciadesde").GetString()
            ?? throw new InvalidOperationException("TRM sin fecha.");

        var centavos = ParsearCentavosInvariante(valorStr);
        var fecha = DateOnly.ParseExact(
            fechaStr[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new Trm(centavos, fecha);
    }

    private static long ParsearCentavosInvariante(string texto)
    {
        var limpio = texto.Trim();
        var partes = limpio.Split('.');
        var enteros = long.Parse(partes[0], CultureInfo.InvariantCulture);
        if (partes.Length == 1)
            return enteros * 100;

        var frac = partes[1].PadRight(3, '0');
        var centavos = long.Parse(frac[..2], CultureInfo.InvariantCulture);
        if (frac[2] >= '5')
            centavos++;

        return (enteros * 100) + centavos;
    }
}

public class ServicioTrm(
    ITrmClient cliente,
    IServicioParametros parametros,
    TimeProvider reloj) : IServicioTrm
{
    private const string FormatoFecha = "yyyy-MM-dd";
    private static readonly TimeSpan EsperaBase =
        TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(2);

    public async Task<Trm?> ObtenerAsync(CancellationToken ct = default)
    {
        if (!await parametros.ObtenerBoolAsync(ClavesParametro.TrmHabilitada))
            return await LeerRespaldoAsync();

        var reintentos = Math.Max(0, await parametros.ObtenerEnteroAsync(
            ClavesParametro.Reintentos));
        var timeout = await parametros.ObtenerEnteroAsync(
            ClavesParametro.TimeoutSegundos);

        for (var intento = 0; intento <= reintentos; intento++)
        {
            if (intento > 0)
                await EsperarAsync(intento, ct);

            var trm = await IntentarAsync(timeout, ct);

            if (trm is null)
                continue;

            await GuardarRespaldoAsync(trm);
            return trm;
        }

        return await LeerRespaldoAsync();
    }

    private Task EsperarAsync(int intento, CancellationToken ct)
    {
        var milisegundos = Math.Min(
            EsperaBase.TotalMilliseconds * Math.Pow(2, intento - 1),
            EsperaMaxima.TotalMilliseconds);

        return Task.Delay(
            TimeSpan.FromMilliseconds(milisegundos), reloj, ct);
    }

    private async Task<Trm?> IntentarAsync(int segundos, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(segundos));

        try
        {
            return await cliente.ObtenerAsync(limite.Token);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private Task GuardarRespaldoAsync(Trm trm)
        => parametros.GuardarRespaldoAsync(new Dictionary<string, string>
        {
            [ClavesParametro.TrmUltimaCentavos] =
                trm.Centavos.ToString(CultureInfo.InvariantCulture),
            [ClavesParametro.TrmUltimaFecha] =
                trm.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture)
        });

    private async Task<Trm?> LeerRespaldoAsync()
    {
        var centavos = await parametros.ObtenerLongAsync(
            ClavesParametro.TrmUltimaCentavos);

        if (centavos <= 0)
            return null;

        var texto = await parametros.ObtenerTextoAsync(
            ClavesParametro.TrmUltimaFecha);
        var fecha = DateOnly.ParseExact(
            texto, FormatoFecha, CultureInfo.InvariantCulture);

        return new Trm(centavos, fecha);
    }
}

// ==========================================
// Código de Barras (Open Food Facts)
// ==========================================

public record InfoProductoExterno(
    string Nombre,
    string? Marca,
    string? Categoria);

public interface IConsultaCodigoBarras
{
    Task<InfoProductoExterno?> ConsultarAsync(
        string codigo,
        CancellationToken ct);
}

public class ConsultaCodigoBarrasNula : IConsultaCodigoBarras
{
    public Task<InfoProductoExterno?> ConsultarAsync(
        string codigo,
        CancellationToken ct)
        => Task.FromResult<InfoProductoExterno?>(null);
}

public class ClienteCodigoBarrasOff(
    HttpClient http,
    OpcionesApisExternas opciones,
    IServicioParametros parametros) : IConsultaCodigoBarras
{
    public async Task<InfoProductoExterno?> ConsultarAsync(
        string codigo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opciones.CodigoBarrasBaseUrl)
            || string.IsNullOrWhiteSpace(codigo))
            return null;

        if (!await parametros.ObtenerBoolAsync(
                ClavesParametro.CodigoBarrasHabilitado))
            return null;

        var timeout = await parametros.ObtenerEnteroAsync(
            ClavesParametro.TimeoutSegundos);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeout)));

        var baseUrl = opciones.CodigoBarrasBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/api/v2/product/{Uri.EscapeDataString(codigo)}"
            + "?fields=product_name,brands,categories";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "SIGS-UPC/1.0");

            using var resp = await http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("status", out var status)
                || status.GetInt32() != 1
                || !root.TryGetProperty("product", out var prod))
                return null;

            var nombre = ObtenerTexto(prod, "product_name");
            if (string.IsNullOrWhiteSpace(nombre))
                return null;

            var marca = ObtenerTexto(prod, "brands");
            var categoria = ObtenerTexto(prod, "categories");

            return new InfoProductoExterno(nombre, marca, categoria);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private static string? ObtenerTexto(JsonElement elem, string prop)
    {
        if (!elem.TryGetProperty(prop, out var val)
            || val.ValueKind != JsonValueKind.String)
            return null;

        var texto = val.GetString()?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }
}

// ==========================================
// Verificación de Correo Electrónico
// ==========================================

public interface IVerificadorCorreo
{
    Task<bool> VerificarAsync(string correo, CancellationToken ct = default);
}

public class VerificadorCorreoNulo : IVerificadorCorreo
{
    public Task<bool> VerificarAsync(
        string correo,
        CancellationToken ct = default)
        => Task.FromResult(false);
}

public class ClienteVerificadorCorreoHttp(
    HttpClient http,
    OpcionesApisExternas opciones,
    IServicioParametros parametros) : IVerificadorCorreo
{
    public async Task<bool> VerificarAsync(
        string correo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
            return false;

        if (string.IsNullOrWhiteSpace(opciones.VerificadorCorreoUrl))
            return true;

        var timeout = await parametros.ObtenerEnteroAsync(
            ClavesParametro.TimeoutSegundos);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeout)));

        try
        {
            var cuerpo = new { to_email = correo, email = correo };
            using var req = new HttpRequestMessage(
                HttpMethod.Post, opciones.VerificadorCorreoUrl)
            {
                Content = JsonContent.Create(cuerpo)
            };

            if (!string.IsNullOrWhiteSpace(opciones.VerificadorCorreoToken))
                req.Headers.TryAddWithoutValidation(
                    "Authorization", opciones.VerificadorCorreoToken);

            using var resp = await http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode)
                return true;

            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("is_reachable", out var reach)
                && reach.ValueKind == JsonValueKind.String)
                return !string.Equals(
                    reach.GetString(),
                    "invalid",
                    StringComparison.OrdinalIgnoreCase);

            if (root.TryGetProperty("valid", out var valid)
                && (valid.ValueKind is JsonValueKind.True
                    or JsonValueKind.False))
                return valid.GetBoolean();

            return true;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return true;
        }
    }
}
