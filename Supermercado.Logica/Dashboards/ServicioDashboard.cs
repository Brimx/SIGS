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
    private const int LongitudMaximaTitulo = 60;

    public IReadOnlyList<IWidget> ObtenerDisponibles()
        => widgets
            .Where(w => sesion.Tiene(w.PermisoRequerido))
            .OrderBy(w => IndiceDeGrupo(w.Grupo))
            .ThenBy(w => w.Id, StringComparer.Ordinal)
            .ToList();

    public async Task<IReadOnlyList<WidgetEnPanel>> ObtenerPanelAsync()
    {
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var disponibles = ObtenerDisponibles();

        if (guardadas.Count == 0)
            return disponibles
                .Select(w => new WidgetEnPanel(w, w.Tamano, w.Titulo))
                .ToList();

        return guardadas
            .Where(c => c.Visible)
            .OrderBy(c => c.Posicion)
            .ThenBy(c => c.WidgetId, StringComparer.Ordinal)
            .Select(c => (Config: c, Widget: disponibles.FirstOrDefault(w => w.Id == c.WidgetId)))
            .Where(par => par.Widget is not null)
            .Select(par => new WidgetEnPanel(
                par.Widget!, par.Config.Tamano, TituloEfectivo(par.Widget!, par.Config)))
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

    public async Task CambiarTamanoAsync(string widgetId, TamanoWidget tamano)
    {
        ExigirDisponible(widgetId);

        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var (config, esNueva) = await ObtenerOCrearAsync(widgetId, guardadas);

        config.Tamano = tamano;
        if (!esNueva)
            configuraciones.Actualizar(config);

        await trabajo.GuardarAsync();
    }

    public async Task RenombrarAsync(string widgetId, string? titulo)
    {
        ExigirDisponible(widgetId);

        var widget = ObtenerDisponibles().First(w => w.Id == widgetId);
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var (config, esNueva) = await ObtenerOCrearAsync(widgetId, guardadas);

        config.TituloPersonalizado = NormalizarTitulo(titulo, widget.Titulo);
        if (!esNueva)
            configuraciones.Actualizar(config);

        await trabajo.GuardarAsync();
    }

    public async Task RestablecerAsync()
    {
        var disponibles = ObtenerDisponibles();
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);

        for (var posicion = 0; posicion < disponibles.Count; posicion++)
        {
            var widget = disponibles[posicion];
            var config = guardadas.FirstOrDefault(c => c.WidgetId == widget.Id);

            if (config is null)
            {
                await configuraciones.AgregarAsync(new ConfiguracionWidget
                {
                    EmpleadoId = sesion.EmpleadoId,
                    WidgetId = widget.Id,
                    Posicion = posicion,
                    Visible = true,
                    Tamano = widget.Tamano
                });
                continue;
            }

            config.Posicion = posicion;
            config.Visible = true;
            config.Tamano = widget.Tamano;
            config.TituloPersonalizado = null;
            configuraciones.Actualizar(config);
        }

        await trabajo.GuardarAsync();
    }

    public async Task<string> ExportarAsync()
    {
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);
        var disponibles = ObtenerDisponibles();

        var entradas = guardadas
            .Where(c => disponibles.Any(w => w.Id == c.WidgetId))
            .OrderBy(c => c.Posicion)
            .ThenBy(c => c.WidgetId, StringComparer.Ordinal)
            .Select(c => new EntradaDisposicion(
                c.WidgetId, c.Posicion, c.Visible, c.Tamano, c.TituloPersonalizado))
            .ToList();

        if (entradas.Count == 0)
            entradas = disponibles
                .Select((w, posicion) => new EntradaDisposicion(
                    w.Id, posicion, true, w.Tamano, null))
                .ToList();

        return DisposicionPanelJson.Serializar(
            new DisposicionPanel(DisposicionPanel.VersionActual, entradas));
    }

    public async Task<ResultadoImportacion> ImportarAsync(string json)
    {
        var disposicion = DisposicionPanelJson.Deserializar(json);
        var disponibles = ObtenerDisponibles().ToDictionary(w => w.Id);
        var guardadas = await configuraciones
            .ObtenerPorEmpleadoAsync(sesion.EmpleadoId);

        var omitidos = new List<string>();
        var posicion = 0;

        foreach (var entrada in disposicion.Widgets
            .OrderBy(e => e.Posicion)
            .ThenBy(e => e.WidgetId, StringComparer.Ordinal))
        {
            if (!disponibles.TryGetValue(entrada.WidgetId, out var widget))
            {
                omitidos.Add(entrada.WidgetId);
                continue;
            }

            var config = guardadas.FirstOrDefault(c => c.WidgetId == entrada.WidgetId);

            if (config is null)
            {
                await configuraciones.AgregarAsync(new ConfiguracionWidget
                {
                    EmpleadoId = sesion.EmpleadoId,
                    WidgetId = entrada.WidgetId,
                    Posicion = posicion++,
                    Visible = entrada.Visible,
                    Tamano = entrada.Tamano,
                    TituloPersonalizado = NormalizarTitulo(entrada.Titulo, widget.Titulo)
                });
                continue;
            }

            config.Posicion = posicion++;
            config.Visible = entrada.Visible;
            config.Tamano = entrada.Tamano;
            config.TituloPersonalizado = NormalizarTitulo(entrada.Titulo, widget.Titulo);
            configuraciones.Actualizar(config);
        }

        // Lo que no viene en el archivo deja de mostrarse: importar reemplaza el panel.
        var importados = disposicion.Widgets.Select(e => e.WidgetId).ToHashSet(StringComparer.Ordinal);
        foreach (var c in guardadas.Where(c => c.Visible && !importados.Contains(c.WidgetId)))
        {
            c.Visible = false;
            configuraciones.Actualizar(c);
        }

        await trabajo.GuardarAsync();

        return new ResultadoImportacion(
            disposicion.Widgets.Count - omitidos.Count, omitidos);
    }

    // Devuelve además si la configuración es nueva: una entidad recién creada
    // ya está en estado Added y el repositorio no debe marcarla como Modified
    // (EF rechaza actualizar una entidad con clave temporal).
    private async Task<(ConfiguracionWidget Config, bool EsNueva)> ObtenerOCrearAsync(
        string widgetId,
        IReadOnlyList<ConfiguracionWidget> guardadas)
    {
        var existente = guardadas.FirstOrDefault(c => c.WidgetId == widgetId);
        if (existente is not null)
            return (existente, false);

        var widget = ObtenerDisponibles().First(w => w.Id == widgetId);
        var nueva = new ConfiguracionWidget
        {
            EmpleadoId = sesion.EmpleadoId,
            WidgetId = widgetId,
            Posicion = SiguientePosicion(guardadas),
            Visible = true,
            Tamano = widget.Tamano
        };
        await configuraciones.AgregarAsync(nueva);
        return (nueva, true);
    }

    private void ExigirDisponible(string widgetId)
    {
        if (ObtenerDisponibles().All(w => w.Id != widgetId))
            throw new ValidacionException("Ese widget no está disponible.");
    }

    private static string TituloEfectivo(IWidget widget, ConfiguracionWidget config)
        => string.IsNullOrWhiteSpace(config.TituloPersonalizado)
            ? widget.Titulo
            : config.TituloPersonalizado;

    private static string? NormalizarTitulo(string? titulo, string tituloOriginal)
    {
        var limpio = titulo?.Trim();
        if (string.IsNullOrEmpty(limpio) || limpio == tituloOriginal)
            return null;

        return limpio.Length > LongitudMaximaTitulo
            ? limpio[..LongitudMaximaTitulo]
            : limpio;
    }

    private static int IndiceDeGrupo(string grupo)
    {
        var indice = GruposWidget.Orden
            .ToList()
            .FindIndex(g => string.Equals(g, grupo, StringComparison.OrdinalIgnoreCase));
        return indice < 0 ? int.MaxValue : indice;
    }

    private static int SiguientePosicion(
        IReadOnlyList<ConfiguracionWidget> guardadas)
        => guardadas.Count == 0
            ? 0
            : guardadas.Max(c => c.Posicion) + 1;
}
