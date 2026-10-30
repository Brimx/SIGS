using System.Text.Json;
using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

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
