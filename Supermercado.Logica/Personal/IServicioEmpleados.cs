using Supermercado.Entidades.Personal;

namespace Supermercado.Logica.Personal;

public interface IServicioEmpleados
{
    Task CrearAsync(Empleado empleado);
    Task EditarAsync(Empleado empleado);
    Task CambiarRolAsync(int empleadoId, int rolId);
    Task DesactivarAsync(int empleadoId);

    Task CrearUsuarioAsync(
        int empleadoId,
        string nombreUsuario,
        string claveTemporal);
}
