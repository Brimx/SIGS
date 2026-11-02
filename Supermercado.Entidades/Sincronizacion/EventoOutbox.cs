using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Sincronizacion;

// Registro en el patrón Outbox. Se inserta en la misma transacción que
// la venta o movimiento local para réplica asíncrona hacia Supabase o Neon.
public class EventoOutbox : EntidadBase
{
    public string TipoEvento { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public bool Procesado { get; set; }
    public DateTime? FechaProcesado { get; set; }
    public int Reintentos { get; set; }
    public string? UltimoError { get; set; }

    public static EventoOutbox Crear(
        string tipoEvento,
        string entidadId,
        string payloadJson) => new()
    {
        TipoEvento = tipoEvento,
        EntidadId = entidadId,
        PayloadJson = payloadJson,
        Procesado = false,
        Reintentos = 0
    };

    public void MarcarProcesado(DateTime ahora)
    {
        Procesado = true;
        FechaProcesado = ahora;
        UltimoError = null;
    }

    public void RegistrarFallo(string error)
    {
        Reintentos++;
        UltimoError = error;
    }
}
