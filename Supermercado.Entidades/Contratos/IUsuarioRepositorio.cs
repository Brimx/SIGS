using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Contratos;

public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    // Debe incluir Empleado y su Rol.
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario);
}
