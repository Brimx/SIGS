using System.Text.Json;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Supermercado.GUI.Servicios.Tema
{
    /// <summary>
    /// Variantes visuales soportadas por SIGS. El modo claro es cálido (anti-fatiga para turnos
    /// largos de caja), el oscuro usa la escala Slate y el alto contraste prioriza legibilidad.
    /// </summary>
    public static class TemaSigs
    {
        public const string ClaveClaro = "Claro";
        public const string ClaveOscuro = "Oscuro";
        public const string ClaveAltoContraste = "AltoContraste";

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

        public static string Siguiente(string? clave)
        {
            var indice = Array.IndexOf(Claves, clave);
            return Claves[(indice + 1) % Claves.Length];
        }

        public static void Aplicar(string? clave) => Aplicar(Resolver(clave));

        public static void Aplicar(ThemeVariant variante)
        {
            if (Application.Current is { } app)
                app.RequestedThemeVariant = variante;
        }
    }
}

namespace Supermercado.GUI.Servicios.Preferencias
{
    using Tema;

    public interface IServicioPreferencias
    {
        string LeerTema();
        void GuardarTema(string clave);
    }

    public sealed class ServicioPreferencias : IServicioPreferencias
    {
        private static readonly JsonSerializerOptions Opciones = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly string _ruta;

        public ServicioPreferencias(string? ruta = null)
        {
            _ruta = ruta ?? RutaPorDefecto();
        }

        public static string RutaPorDefecto() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIGS",
            "preferencias.json");

        public string LeerTema()
        {
            try
            {
                if (!File.Exists(_ruta))
                    return TemaSigs.ClaveClaro;

                var preferencias = JsonSerializer.Deserialize<Preferencias>(File.ReadAllText(_ruta), Opciones);
                return preferencias?.Tema is { } tema && TemaSigs.Claves.Contains(tema)
                    ? tema
                    : TemaSigs.ClaveClaro;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return TemaSigs.ClaveClaro;
            }
        }

        public void GuardarTema(string clave)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                var temporal = _ruta + ".tmp";
                File.WriteAllText(temporal, JsonSerializer.Serialize(new Preferencias { Tema = clave }, Opciones));
                File.Move(temporal, _ruta, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        private sealed class Preferencias
        {
            public string? Tema { get; set; }
        }
    }
}

namespace Supermercado.GUI.Servicios.Legal
{
    public interface IServicioTextosLegales
    {
        string PoliticaPrivacidad();
        string TerminosServicio();
    }

    public sealed class ServicioTextosLegales : IServicioTextosLegales
    {
        private const string Base = "avares://Supermercado.GUI/Assets/Legal/";

        public string PoliticaPrivacidad() => Leer("politica-privacidad.txt");

        public string TerminosServicio() => Leer("terminos-servicio.txt");

        private static string Leer(string archivo)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(Base + archivo));
                using var lector = new StreamReader(stream);
                return lector.ReadToEnd();
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or UriFormatException)
            {
                return "No fue posible cargar el texto legal. Consulte con el administrador del sistema.";
            }
        }
    }
}
