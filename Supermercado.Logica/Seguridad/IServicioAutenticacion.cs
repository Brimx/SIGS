using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public interface IServicioAutenticacion
{
    Task IniciarSesionAsync(string nombreUsuario, string clave);
    Task IniciarSesionConPinAsync(string nombreUsuario, string pin);
    Task CambiarClaveAsync(string claveActual, string claveNueva);
    Task AsignarPinAsync(string pin);
    void CerrarSesion();

    /// <summary>
    /// Valida puntualmente la autorización de un supervisor por PIN o clave para elevar permisos
    /// de una operación puntual (ej. anulación de venta/ítem) sin cerrar ni alterar la sesión del cajero.
    /// </summary>
    Task<Empleado> ValidarSupervisorAsync(string nombreUsuario, string pinOClave, Permiso permisoExigido);
}
