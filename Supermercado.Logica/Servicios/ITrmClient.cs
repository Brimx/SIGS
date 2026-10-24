namespace Supermercado.Logica.Servicios;

public interface ITrmClient
{
    Task<Trm> ObtenerAsync(CancellationToken ct);
}
