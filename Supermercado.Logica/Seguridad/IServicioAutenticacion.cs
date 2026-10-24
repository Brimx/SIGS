namespace Supermercado.Logica.Seguridad;

public interface IServicioAutenticacion
{
    Task IniciarSesionAsync(string nombreUsuario, string clave);
    Task IniciarSesionConPinAsync(string nombreUsuario, string pin);
    Task CambiarClaveAsync(string claveActual, string claveNueva);
    Task AsignarPinAsync(string pin);
    void CerrarSesion();
}
