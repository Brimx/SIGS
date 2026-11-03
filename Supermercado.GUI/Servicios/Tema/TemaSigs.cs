using Avalonia;
using Avalonia.Styling;

namespace Supermercado.GUI.Servicios.Tema;

/// <summary>
/// Variantes visuales soportadas por SIGS. El modo claro es cálido (anti-fatiga para turnos
/// largos de caja), el oscuro usa la escala Slate y el alto contraste prioriza legibilidad.
/// </summary>
public static class TemaSigs
{
    public const string ClaveClaro = "Claro";
    public const string ClaveOscuro = "Oscuro";
    public const string ClaveAltoContraste = "AltoContraste";

    /// <summary>
    /// Variante personalizada. Avalonia solo resuelve por TypeConverter las variantes integradas,
    /// por eso se declara aquí y se referencia desde XAML con <c>{x:Static}</c>.
    /// Hereda de Dark para que los controles de Semi sin override usen su paleta oscura.
    /// </summary>
    public static readonly ThemeVariant AltoContraste =
        new(ClaveAltoContraste, ThemeVariant.Dark);

    public static string[] Claves { get; } = [ClaveClaro, ClaveOscuro, ClaveAltoContraste];

    public static ThemeVariant Resolver(string? clave) => clave switch
    {
        ClaveOscuro => ThemeVariant.Dark,
        ClaveAltoContraste => AltoContraste,
        _ => ThemeVariant.Light,
    };

    public static string ClaveDe(ThemeVariant variante)
    {
        if (variante == ThemeVariant.Light) return ClaveClaro;
        if (variante == ThemeVariant.Dark) return ClaveOscuro;
        if (variante == AltoContraste) return ClaveAltoContraste;
        return ClaveClaro;
    }

    /// <summary>Siguiente clave del ciclo Claro → Oscuro → AltoContraste → Claro.</summary>
    public static string Siguiente(string? clave)
    {
        // Clave desconocida → primer tema, igual que Resolver.
        var indice = Array.IndexOf(Claves, clave);
        return Claves[(indice + 1) % Claves.Length];
    }

    public static void Aplicar(string? clave)
        => Aplicar(Resolver(clave));

    public static void Aplicar(ThemeVariant variante)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = variante;
    }
}
