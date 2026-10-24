namespace Supermercado.Logica.Dashboards;

public interface IServicioDashboard
{
    IReadOnlyList<IWidget> ObtenerDisponibles();
    Task<IReadOnlyList<IWidget>> ObtenerDisposicionAsync();
    Task AgregarAsync(string widgetId);
    Task QuitarAsync(string widgetId);
    Task ReordenarAsync(IReadOnlyList<string> widgetIds);
}
