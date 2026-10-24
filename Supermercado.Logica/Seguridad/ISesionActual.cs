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
    void Cerrar();
}
