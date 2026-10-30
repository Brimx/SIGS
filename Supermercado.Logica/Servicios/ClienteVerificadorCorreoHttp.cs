using System.Net.Http.Json;
using System.Text.Json;
using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

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
