using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Seguridad;

public class ServicioAuditoria(
    IRepositorio<RegistroAuditoria> registros,
    ISesionActual sesion,
    TimeProvider reloj) : IServicioAuditoria
{
    public Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
        => registros.AgregarAsync(new RegistroAuditoria
        {
            Fecha = reloj.AhoraUtc(),
            EmpleadoId = sesion.EstaAutenticada ? sesion.EmpleadoId : null,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle
        });
}
