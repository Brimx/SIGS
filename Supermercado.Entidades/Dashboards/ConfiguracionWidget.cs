using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Dashboards;

public class ConfiguracionWidget : EntidadBase
{
    public int EmpleadoId { get; set; }
    public string WidgetId { get; set; } = string.Empty;
    public int Posicion { get; set; }
    public bool Visible { get; set; } = true;
}
