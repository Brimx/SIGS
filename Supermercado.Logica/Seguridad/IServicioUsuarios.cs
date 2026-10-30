using Supermercado.Entidades.Personal;

namespace Supermercado.Logica.Seguridad;

public interface IServicioUsuarios
{
    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync();

    Task DesbloquearAsync(int usuarioId);

    Task<string> RestablecerClaveTemporalAsync(int usuarioId);
}
