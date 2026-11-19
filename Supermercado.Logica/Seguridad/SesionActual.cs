using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public interface ISesionActual
{
    Usuario? Usuario { get; }
    bool EstaAutenticada { get; }
    bool DebeCambiarClave { get; }
    int EmpleadoId { get; }
    bool Tiene(Permiso permiso);
    void Exigir(Permiso permiso);
    void Iniciar(Usuario usuario);
    IDisposable UsarUsuario(Usuario usuario);
    void Cerrar();
}

public class SesionActual : ISesionActual
{
    private readonly AsyncLocal<Usuario?> _usuarioTemporal = new();
    private Usuario? _usuarioActual;

    public Usuario? Usuario => _usuarioTemporal.Value ?? _usuarioActual;
    public bool EstaAutenticada => Usuario is not null;
    public bool DebeCambiarClave => Usuario?.DebeCambiarClave ?? false;
    public int EmpleadoId => Usuario?.EmpleadoId ?? throw new SesionNoIniciadaException();

    public bool Tiene(Permiso permiso)
    {
        if (Usuario?.Empleado?.Activo != true)
        {
            Cerrar();
            return false;
        }

        return Usuario.Empleado.Rol?.Tiene(permiso) ?? false;
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

    public void Iniciar(Usuario usuario) => _usuarioActual = usuario;

    public IDisposable UsarUsuario(Usuario usuario)
    {
        var anterior = _usuarioTemporal.Value;
        _usuarioTemporal.Value = usuario;
        return new Restaurador(() => _usuarioTemporal.Value = anterior);
    }

    public void Cerrar() => _usuarioActual = null;

    private sealed class Restaurador(Action restaurar) : IDisposable
    {
        private Action? _restaurar = restaurar;
        public void Dispose() => Interlocked.Exchange(ref _restaurar, null)?.Invoke();
    }
}