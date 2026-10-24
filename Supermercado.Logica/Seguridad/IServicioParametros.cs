namespace Supermercado.Logica.Seguridad;

public interface IServicioParametros
{
    Task<string> ObtenerTextoAsync(string clave);
    Task<int> ObtenerEnteroAsync(string clave);
    Task<long> ObtenerLongAsync(string clave);
    // Cambio administrativo: exige permiso y deja auditoría.
    Task GuardarAsync(string clave, string valor);

    // Respaldo interno (semilla, TRM): sin permiso ni auditoría, atómico.
    Task GuardarRespaldoAsync(IReadOnlyDictionary<string, string> valores);
}
