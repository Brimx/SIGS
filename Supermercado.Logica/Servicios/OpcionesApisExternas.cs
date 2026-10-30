namespace Supermercado.Logica.Servicios;

public class OpcionesApisExternas
{
    public string? TrmUrl { get; init; }
    public string? TrmAppToken { get; init; }
    public string? CodigoBarrasBaseUrl { get; init; }
    public string? VerificadorCorreoUrl { get; init; }
    public string? VerificadorCorreoToken { get; init; }

    public static OpcionesApisExternas DesdeEntorno() => new()
    {
        TrmUrl = Environment.GetEnvironmentVariable("SIGS_TRM_URL"),
        TrmAppToken = Environment.GetEnvironmentVariable("SIGS_TRM_TOKEN"),
        CodigoBarrasBaseUrl = Environment.GetEnvironmentVariable(
            "SIGS_OFF_URL"),
        VerificadorCorreoUrl = Environment.GetEnvironmentVariable(
            "SIGS_EMAIL_VERIFIER_URL"),
        VerificadorCorreoToken = Environment.GetEnvironmentVariable(
            "SIGS_EMAIL_VERIFIER_TOKEN")
    };
}
