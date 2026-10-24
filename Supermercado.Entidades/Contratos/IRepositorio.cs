using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Contratos;

public interface IRepositorio<T> where T : EntidadBase
{
    Task<T?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<T>> ObtenerTodosAsync();
    Task AgregarAsync(T entidad);
    void Actualizar(T entidad);
}
