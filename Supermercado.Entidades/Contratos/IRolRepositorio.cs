using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Contratos;

public interface IRolRepositorio : IRepositorio<Rol>
{
    Task<Rol?> ObtenerPorNombreAsync(string nombre);
}
