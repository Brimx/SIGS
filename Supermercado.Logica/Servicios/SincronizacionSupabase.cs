using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

public interface IServicioSincronizacionRemota
{
    Task<int> SincronizarPendientesAsync(CancellationToken ct = default);
}

public class ClienteSincronizacionSupabase(
    HttpClient http,
    IEventoOutboxRepositorio outbox,
    IServicioParametros parametros,
    OpcionesApisExternas opciones,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioSincronizacionRemota
{
    public async Task<int> SincronizarPendientesAsync(
        CancellationToken ct = default)
    {
        if (!await parametros.ObtenerBoolAsync(
                ClavesParametro.SupabaseHabilitado))
            return 0;

        var supabaseUrl = await parametros.ObtenerTextoOpcionalAsync(
            ClavesParametro.SupabaseUrl, opciones.SupabaseUrl);
        var supabaseKey = await parametros.ObtenerTextoOpcionalAsync(
            ClavesParametro.SupabaseKey, opciones.SupabaseKey);

        if (string.IsNullOrWhiteSpace(supabaseUrl)
            || string.IsNullOrWhiteSpace(supabaseKey))
            return 0;

        var pendientes = await outbox.ObtenerPendientesAsync(50);
        if (pendientes.Count == 0)
            return 0;

        var procesados = 0;
        var tabla = string.IsNullOrWhiteSpace(opciones.SupabaseTabla)
            ? "eventos_outbox"
            : opciones.SupabaseTabla.Trim();
        var url = $"{supabaseUrl.TrimEnd('/')}/rest/v1/{tabla}";

        foreach (var evento in pendientes)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.TryAddWithoutValidation("apikey", supabaseKey);
                req.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", supabaseKey);
                req.Headers.TryAddWithoutValidation(
                    "Prefer", "resolution=merge-duplicates");

                var payloadObj = JsonSerializer.Deserialize<JsonElement>(
                    evento.PayloadJson);
                var body = new
                {
                    id_local = evento.Id,
                    tipo_evento = evento.TipoEvento,
                    entidad_id = evento.EntidadId,
                    payload = payloadObj,
                    creado_en = evento.FechaCreacion
                };

                req.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");

                using var resp = await http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    evento.MarcarProcesado(reloj.AhoraUtc());
                    procesados++;
                }
                else
                {
                    var error = await resp.Content.ReadAsStringAsync(ct);
                    evento.RegistrarFallo(
                        $"HTTP {(int)resp.StatusCode}: {error}");
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                evento.RegistrarFallo(ex.Message);
            }
        }

        await trabajo.GuardarAsync();
        return procesados;
    }
}
