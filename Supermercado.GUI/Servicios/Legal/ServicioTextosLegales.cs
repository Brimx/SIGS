using Avalonia.Platform;

namespace Supermercado.GUI.Servicios.Legal;

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
