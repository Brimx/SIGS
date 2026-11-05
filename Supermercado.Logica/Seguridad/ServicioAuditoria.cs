using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Seguridad;

public class ServicioAuditoria(
    IRepositorio<RegistroAuditoria> registros,
    ISesionActual sesion,
    TimeProvider reloj) : IServicioAuditoria, IServicioConsultaAuditoria
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

    public async Task<ResultadoPaginado<RegistroAuditoria>> ConsultarAsync(
        FiltroAuditoria filtro)
    {
        sesion.Exigir(Permiso.VerAuditoria);

        var todos = await registros.ObtenerTodosAsync();
        var consulta = todos.AsEnumerable();

        if (filtro.Desde.HasValue)
            consulta = consulta.Where(r => r.Fecha >= filtro.Desde.Value);

        if (filtro.Hasta.HasValue)
            consulta = consulta.Where(r => r.Fecha <= filtro.Hasta.Value);

        if (filtro.Accion.HasValue)
            consulta = consulta.Where(r => r.Accion == filtro.Accion.Value);

        if (filtro.EmpleadoId.HasValue)
            consulta = consulta.Where(r => r.EmpleadoId == filtro.EmpleadoId.Value);

        var ordenados = consulta
            .OrderByDescending(r => r.Fecha)
            .ToList();

        var total = ordenados.Count;
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Max(1, filtro.TamanoPagina);

        var items = ordenados
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToList();

        return new ResultadoPaginado<RegistroAuditoria>(
            items, total, pagina, tamano);
    }
}

// Adaptador para compatibilidad binaria con pruebas de consulta aislada
public class ServicioConsultaAuditoria(
    IRepositorio<RegistroAuditoria> registros,
    ISesionActual sesion) : ServicioAuditoria(registros, sesion, TimeProvider.System);
