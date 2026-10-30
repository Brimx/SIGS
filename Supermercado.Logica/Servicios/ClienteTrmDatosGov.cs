using System.Globalization;
using System.Text.Json;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Servicios;

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
