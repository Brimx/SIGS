using Supermercado.Entidades.Dashboards;

namespace Supermercado.Logica.Dashboards;

public interface IServicioDashboard
{
    /// <summary>Widgets que el rol de la sesión puede ver, sin importar su disposición.</summary>
    IReadOnlyList<IWidget> ObtenerDisponibles();

    /// <summary>
    /// Disposición efectiva del empleado: widgets visibles en orden. Si el
    /// empleado nunca guardó nada, devuelve todos los disponibles.
    /// </summary>
    Task<IReadOnlyList<WidgetEnPanel>> ObtenerPanelAsync();

    Task AgregarAsync(string widgetId);
    Task QuitarAsync(string widgetId);
    Task ReordenarAsync(IReadOnlyList<string> widgetIds);
    Task CambiarTamanoAsync(string widgetId, TamanoWidget tamano);

    /// <summary>Renombra el widget en el panel; nulo o vacío restaura el título original.</summary>
    Task RenombrarAsync(string widgetId, string? titulo);

    /// <summary>Devuelve el panel a la disposición por defecto del rol.</summary>
    Task RestablecerAsync();

    /// <summary>Serializa la disposición completa (incluidos los widgets ocultos).</summary>
    Task<string> ExportarAsync();

    /// <summary>
    /// Aplica una disposición exportada. Omite los widgets que el rol no puede
    /// ver y devuelve cuáles quedaron fuera.
    /// </summary>
    Task<ResultadoImportacion> ImportarAsync(string json);
}

public sealed record ResultadoImportacion(
    int Aplicados,
    IReadOnlyList<string> Omitidos);
