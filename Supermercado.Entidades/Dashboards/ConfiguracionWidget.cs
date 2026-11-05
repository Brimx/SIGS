using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Dashboards;

/// <summary>
/// Tamaño con el que se pinta un widget en el panel. El widget propone uno por
/// defecto y cada usuario puede cambiarlo en su disposición.
/// </summary>
public enum TamanoWidget
{
    Pequeno,
    Mediano,
    Grande
}

/// <summary>
/// Disposición de un widget para un empleado: si está visible, en qué orden,
/// con qué tamaño y con qué título. Es preferencia personal, no configuración
/// de negocio, así que no pasa por auditoría.
/// </summary>
public class ConfiguracionWidget : EntidadBase
{
    public int EmpleadoId { get; set; }
    public string WidgetId { get; set; } = string.Empty;
    public int Posicion { get; set; }
    public bool Visible { get; set; } = true;
    public TamanoWidget Tamano { get; set; } = TamanoWidget.Mediano;

    /// <summary>Nulo o vacío significa «usa el título del widget».</summary>
    public string? TituloPersonalizado { get; set; }
}

/// <summary>
/// Documento portable con la disposición del panel de un usuario. Se exporta e
/// importa como JSON para replicar el tablero en otro equipo o cuenta.
/// </summary>
public sealed record DisposicionPanel(
    int Version,
    IReadOnlyList<EntradaDisposicion> Widgets)
{
    /// <summary>Versión del formato que entiende esta compilación.</summary>
    public const int VersionActual = 1;
}

/// <summary>Un widget dentro de una disposición exportada.</summary>
public sealed record EntradaDisposicion(
    string WidgetId,
    int Posicion,
    bool Visible,
    TamanoWidget Tamano,
    string? Titulo);
