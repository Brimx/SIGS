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

    public static string CarpetaRespaldosPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            "respaldos");

    public static string ArchivoLogPorDefecto()
        => Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Carpeta,
            "logs",
            "errores.log");
}
