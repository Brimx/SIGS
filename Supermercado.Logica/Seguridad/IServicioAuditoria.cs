using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

// No guarda: se guarda junto con la operación que se audita.
public interface IServicioAuditoria
{
    Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "");
}
