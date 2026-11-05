using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Dashboards;

/// <summary>Grupos del catálogo de widgets (§12 del documento de proyecto).</summary>
public static class GruposWidget
{
    public const string Ventas = "Ventas";
    public const string Inventario = "Inventario";
    public const string Caja = "Caja";
    public const string Personal = "Personal";
    public const string Finanzas = "Finanzas";
    public const string Externo = "Externo";

    /// <summary>Orden en el que se presentan los grupos.</summary>
    public static readonly IReadOnlyList<string> Orden =
    [
        Ventas, Inventario, Caja, Personal, Finanzas, Externo
    ];
}

/// <summary>Cómo se lee el valor de un punto de dato al presentarlo.</summary>
public enum FormatoDato
{
    Numero,
    Moneda,
    MonedaEnCentavos
}

public record PuntoDato(
    string Etiqueta,
    long Valor,
    FormatoDato Formato = FormatoDato.Numero);

public record DatoWidget(IReadOnlyList<PuntoDato> Puntos);

/// <summary>
/// Contrato común de los dashboards modulares: identidad, título, grupo,
/// tamaño por defecto, frecuencia de actualización, permiso y fuente de datos.
/// </summary>
public interface IWidget
{
    string Id { get; }
    string Titulo { get; }
    string Grupo { get; }
    TamanoWidget Tamano { get; }
    TimeSpan FrecuenciaActualizacion { get; }
    Permiso PermisoRequerido { get; }

    Task<DatoWidget> ObtenerDatosAsync();
}

/// <summary>Widget listo para pintar: el contrato más la personalización del usuario.</summary>
public sealed record WidgetEnPanel(IWidget Widget, TamanoWidget Tamano, string Titulo)
{
    public string Id => Widget.Id;
}
