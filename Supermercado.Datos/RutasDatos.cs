namespace Supermercado.Datos;

public static class RutasDatos
{
    private const string Carpeta = "SIGS";
    private const string Archivo = "sigs.db";

    // Carpeta de datos del usuario: sirve igual en Windows y Linux.
    public static string BaseDatosPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            Archivo);
}
