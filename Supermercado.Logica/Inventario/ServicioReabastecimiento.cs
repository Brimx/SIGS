using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Inventario;

public class ServicioReabastecimiento(
    IReabastecimientoRepositorio solicitudes,
    IServicioAlertasInventario alertas,
    IServicioInventario inventario,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioReabastecimiento
{
    public async Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync()
    {
        sesion.Exigir(Permiso.Reabastecer);

        var pendientes = await solicitudes.ObtenerPendientesAsync();
        var conSolicitud = pendientes.Select(s => s.ProductoId).ToHashSet();
        var nuevas = new List<Reabastecimiento>();

        foreach (var alerta in await alertas.ObtenerBajoMinimoAsync())
        {
            if (conSolicitud.Contains(alerta.Producto.Id))
                continue;

            nuevas.Add(new Reabastecimiento
            {
                ProductoId = alerta.Producto.Id,
                CantidadSolicitada = alerta.Producto.StockMinimoEstante
                    - alerta.CantidadEnEstante,
                FechaSolicitud = reloj.AhoraUtc()
            });
        }

        foreach (var solicitud in nuevas)
            await solicitudes.AgregarAsync(solicitud);

        await trabajo.GuardarAsync();
        return nuevas;
    }

    public Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
    {
        sesion.Exigir(Permiso.Reabastecer);
        return solicitudes.ObtenerPendientesAsync();
    }

    public async Task AtenderAsync(int reabastecimientoId)
    {
        sesion.Exigir(Permiso.Reabastecer);

        var solicitud = await solicitudes.ObtenerPorIdAsync(reabastecimientoId)
            ?? throw new ValidacionException("La solicitud no existe.");

        await inventario.TrasladarAEstanteAsync(
            solicitud.ProductoId, solicitud.CantidadSolicitada);
        solicitud.Atender(reloj.AhoraUtc());
        solicitudes.Actualizar(solicitud);
        await trabajo.GuardarAsync();
    }
}
