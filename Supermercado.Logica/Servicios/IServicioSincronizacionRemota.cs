namespace Supermercado.Logica.Servicios;

public interface IServicioSincronizacionRemota
{
    Task<int> SincronizarPendientesAsync(CancellationToken ct = default);
}
