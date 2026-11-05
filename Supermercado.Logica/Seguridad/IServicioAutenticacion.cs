using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public interface IServicioInicializacion
{
    Task InicializarAsync(string claveAdminInicial);
}

// Servicio unificado de autenticación e inicialización de seguridad
public interface IServicioAutenticacion : IServicioInicializacion
{
    Task IniciarSesionAsync(string nombreUsuario, string clave);
    Task IniciarSesionConPinAsync(string nombreUsuario, string pin);
    Task CambiarClaveAsync(string claveActual, string claveNueva);
    Task AsignarPinAsync(string pin);
    void CerrarSesion();
    Task<Empleado> ValidarSupervisorAsync(string nombreUsuario, string pinOClave, Permiso permisoExigido);
}
