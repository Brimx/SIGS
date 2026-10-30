namespace Supermercado.Entidades.Seguridad;

public record FiltroAuditoria(
    DateTime? Desde = null,
    DateTime? Hasta = null,
    AccionAuditoria? Accion = null,
    int? EmpleadoId = null,
    int Pagina = 1,
    int TamanoPagina = 20);
