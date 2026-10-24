namespace Supermercado.Logica.Servicios;

public interface IServicioTrm
{
    // Devuelve el último valor guardado si la consulta falla.
    Task<Trm?> ObtenerAsync(CancellationToken ct = default);
}
