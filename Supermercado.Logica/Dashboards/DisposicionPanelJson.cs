using System.Text.Json;
using System.Text.Json.Serialization;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Dashboards;

/// <summary>
/// Formato de intercambio de la disposición del panel. Los enums se guardan
/// como texto para que el archivo se lea y se edite a mano.
/// </summary>
public static class DisposicionPanelJson
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serializar(DisposicionPanel disposicion)
        => JsonSerializer.Serialize(disposicion, Opciones);

    public static DisposicionPanel Deserializar(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ValidacionException("El archivo del panel está vacío.");

        DisposicionPanel? disposicion;
        try
        {
            disposicion = JsonSerializer.Deserialize<DisposicionPanel>(json, Opciones);
        }
        catch (JsonException)
        {
            throw new ValidacionException("El archivo no es una disposición de panel válida.");
        }

        if (disposicion is null)
            throw new ValidacionException("El archivo no es una disposición de panel válida.");

        if (disposicion.Version != DisposicionPanel.VersionActual)
            throw new ValidacionException(
                $"La versión {disposicion.Version} del archivo no es compatible con esta instalación.");

        return disposicion with { Widgets = disposicion.Widgets ?? [] };
    }
}
