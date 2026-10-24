namespace Supermercado.Logica.Servicios;

public class VerificadorCorreoNulo : IVerificadorCorreo
{
    public Task<bool> VerificarAsync(
        string correo,
        CancellationToken ct = default)
        => Task.FromResult(false);
}
