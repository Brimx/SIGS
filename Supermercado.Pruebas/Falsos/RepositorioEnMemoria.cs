using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Pruebas.Falsos;

public class RepositorioEnMemoria<T> : IRepositorio<T> where T : EntidadBase
{
    private int _siguienteId = 1000;

    public List<T> Datos { get; } = [];

    public Task<T?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Datos.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<T>> ObtenerTodosAsync()
        => Task.FromResult<IReadOnlyList<T>>(Datos.ToList());

    public virtual Task AgregarAsync(T entidad)
    {
        if (entidad.Id == 0)
            entidad.Id = _siguienteId++;

        Datos.Add(entidad);
        return Task.CompletedTask;
    }

    public void Actualizar(T entidad)
    {
        if (!Datos.Any(e => e.Id == entidad.Id))
            throw new InvalidOperationException(
                $"La entidad {typeof(T).Name} con Id {entidad.Id} no existe.");
    }
}
