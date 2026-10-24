namespace Supermercado.Logica.Servicios;

public interface IVerificadorCorreo
{
    Task<bool> VerificarAsync(string correo, CancellationToken ct = default);
}
