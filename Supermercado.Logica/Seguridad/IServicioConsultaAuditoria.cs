using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public interface IServicioConsultaAuditoria
{
    Task<ResultadoPaginado<RegistroAuditoria>> ConsultarAsync(
        FiltroAuditoria filtro);
}
