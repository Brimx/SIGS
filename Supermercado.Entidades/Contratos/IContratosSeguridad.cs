using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Contratos;

public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    // Debe incluir Empleado y su Rol.
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario);
}

public interface IRolRepositorio : IRepositorio<Rol>
{
    Task<Rol?> ObtenerPorNombreAsync(string nombre);
}

public interface IParametroRepositorio : IRepositorio<Parametro>
{
    Task<Parametro?> ObtenerPorClaveAsync(string clave);
}
