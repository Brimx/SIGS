using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public interface IServicioConsultaAuditoria
{
    Task<ResultadoPaginado<RegistroAuditoria>> ConsultarAsync(FiltroAuditoria filtro);
}

public interface IServicioAuditoria
{
    Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "");
}
