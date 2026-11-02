namespace Supermercado.Logica.Servicios;

public class OpcionesApisExternas
{
    // Fuentes abiertas y gratuitas, activas por defecto (ver ADR-019).
    public const string TrmUrlPorDefecto =
        "https://www.datos.gov.co/resource/32sa-8pi3.json"
        + "?$limit=1&$order=vigenciadesde%20DESC";
    public const string CodigoBarrasUrlPorDefecto =
        "https://world.openfoodfacts.org";

    public string? TrmUrl { get; init; }
    public string? TrmAppToken { get; init; }
    public string? CodigoBarrasBaseUrl { get; init; }
    public string? VerificadorCorreoUrl { get; init; }
    public string? VerificadorCorreoToken { get; init; }
    public string? SupabaseUrl { get; init; }
    public string? SupabaseKey { get; init; }
    public string? SupabaseTabla { get; init; } = "eventos_outbox";

    // Las APIs sin credenciales vienen activas. Cada variable puede
    // reemplazar la URL; SIGS_APIS_HABILITADAS=0 las apaga todas y
    // SIGS_TRM_HABILITADA=0 / SIGS_OFF_HABILITADA=0 apagan una sola.
    // La verificación de correo no tiene servicio público: solo se activa
    // si se define SIGS_EMAIL_VERIFIER_URL.
    public static OpcionesApisExternas DesdeEntorno()
    {
        var todas = Habilitada("SIGS_APIS_HABILITADAS");
        var trm = todas && Habilitada("SIGS_TRM_HABILITADA");
        var off = todas && Habilitada("SIGS_OFF_HABILITADA");

        return new OpcionesApisExternas
        {
            TrmUrl = trm
                ? Valor("SIGS_TRM_URL") ?? TrmUrlPorDefecto
                : null,
            TrmAppToken = Valor("SIGS_TRM_TOKEN"),
            CodigoBarrasBaseUrl = off
                ? Valor("SIGS_OFF_URL") ?? CodigoBarrasUrlPorDefecto
                : null,
            VerificadorCorreoUrl = todas
                ? Valor("SIGS_EMAIL_VERIFIER_URL")
                : null,
            VerificadorCorreoToken = Valor("SIGS_EMAIL_VERIFIER_TOKEN"),
            SupabaseUrl = Valor("SIGS_SUPABASE_URL"),
            SupabaseKey = Valor("SIGS_SUPABASE_KEY"),
            SupabaseTabla = Valor("SIGS_SUPABASE_TABLA") ?? "eventos_outbox"
        };
    }

    private static string? Valor(string variable)
    {
        var valor = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static bool Habilitada(string variable)
        => Valor(variable)?.ToLowerInvariant() is not ("0" or "false" or "no");
}
