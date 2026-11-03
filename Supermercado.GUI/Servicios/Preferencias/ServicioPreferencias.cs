using System.Text.Json;
using Supermercado.GUI.Servicios.Tema;

namespace Supermercado.GUI.Servicios.Preferencias;

/// <summary>
/// Persiste las preferencias en <c>~/.local/share/SIGS/preferencias.json</c>.
/// Lectura tolerante: archivo ausente, JSON corrupto o tema desconocido caen al tema claro.
/// </summary>
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

    // shortcut: duplica la carpeta de RutasDatos; unificar si la ubicación de datos cambia.
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
            // La preferencia no es crítica: si falla, el tema sigue aplicado en esta sesión.
        }
    }

    private sealed class Preferencias
    {
        public string? Tema { get; set; }
    }
}
