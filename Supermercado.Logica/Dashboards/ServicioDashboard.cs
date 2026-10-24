using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Dashboards;

public class ServicioDashboard(
    IEnumerable<IWidget> widgets,
    IConfiguracionWidgetRepositorio configuraciones,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioDashboard
{
    public IReadOnlyList<IWidget> ObtenerDisponibles()
        => widgets.Where(w => sesion.Tiene(w.PermisoRequerido)).ToList();

    public async Task<IReadOnlyList<IWidget>> ObtenerDisposicionAsync()
    {
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var disponibles = ObtenerDisponibles();

        return guardadas
            .Where(c => c.Visible)
            .OrderBy(c => c.Posicion)
            .ThenBy(c => c.WidgetId)
            .Select(c => disponibles.FirstOrDefault(w => w.Id == c.WidgetId))
            .OfType<IWidget>()
            .ToList();
    }

    public async Task AgregarAsync(string widgetId)
    {
        ExigirDisponible(widgetId);

        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var existente = guardadas.FirstOrDefault(c => c.WidgetId == widgetId);

        if (existente is not null)
        {
            existente.Visible = true;
            existente.Posicion = SiguientePosicion(guardadas);
            configuraciones.Actualizar(existente);
        }
        else
            await configuraciones.AgregarAsync(new ConfiguracionWidget
            {
                EmpleadoId = sesion.EmpleadoId,
                WidgetId = widgetId,
                Posicion = SiguientePosicion(guardadas)
            });

        await trabajo.GuardarAsync();
    }

    public async Task QuitarAsync(string widgetId)
    {
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);

        foreach (var c in guardadas.Where(c => c.WidgetId == widgetId))
        {
            c.Visible = false;
            configuraciones.Actualizar(c);
        }

        await trabajo.GuardarAsync();
    }

    public async Task ReordenarAsync(IReadOnlyList<string> widgetIds)
    {
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);

        for (var posicion = 0; posicion < widgetIds.Count; posicion++)
        {
            var id = widgetIds[posicion];
            var c = guardadas.FirstOrDefault(x => x.WidgetId == id);
            if (c is null)
                continue;

            c.Posicion = posicion;
            configuraciones.Actualizar(c);
        }

        await trabajo.GuardarAsync();
    }

    private void ExigirDisponible(string widgetId)
    {
        if (ObtenerDisponibles().All(w => w.Id != widgetId))
            throw new ValidacionException("Ese widget no está disponible.");
    }

    private static int SiguientePosicion(
        IReadOnlyList<ConfiguracionWidget> guardadas)
        => guardadas.Count == 0
            ? 0
            : guardadas.Max(c => c.Posicion) + 1;
}
