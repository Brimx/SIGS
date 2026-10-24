using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public class SesionActual : ISesionActual
{
    public Usuario? Usuario { get; private set; }

    public bool EstaAutenticada => Usuario is not null;

    public bool DebeCambiarClave => Usuario?.DebeCambiarClave ?? false;

    public int EmpleadoId
        => Usuario?.EmpleadoId
            ?? throw new SesionNoIniciadaException();

    public bool Tiene(Permiso permiso)
    {
        if (Usuario?.Empleado?.Activo != true)
        {
            Cerrar();
            return false;
        }

        return Usuario?.Empleado?.Rol?.Tiene(permiso) ?? false;
    }

    public void Exigir(Permiso permiso)
    {
        if (Usuario?.Empleado?.Activo != true)
        {
            Cerrar();
            throw new AccesoDenegadoException(permiso);
        }

        if (DebeCambiarClave)
            throw new DebeCambiarClaveException();

        if (!Tiene(permiso))
            throw new AccesoDenegadoException(permiso);
    }

    public void Iniciar(Usuario usuario) => Usuario = usuario;

    public void Cerrar() => Usuario = null;
}
