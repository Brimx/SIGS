namespace Supermercado.Logica.Servicios;

public interface IServicioRespaldo
{
    Task<string> CrearRespaldoAsync(string? rutaOrigen = null);
}
