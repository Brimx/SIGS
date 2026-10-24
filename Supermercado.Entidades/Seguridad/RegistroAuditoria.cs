using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Seguridad;

public class RegistroAuditoria : EntidadBase
{
    public DateTime Fecha { get; set; }
    public int? EmpleadoId { get; set; }
    public AccionAuditoria Accion { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string Detalle { get; set; } = string.Empty;
}
