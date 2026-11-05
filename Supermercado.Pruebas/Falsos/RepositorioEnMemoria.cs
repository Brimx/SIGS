using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Pruebas.Falsos;

public class RepositorioEnMemoria<T> : IRepositorio<T> where T : EntidadBase
{
    private int _siguienteId = 1000;

    public List<T> Datos { get; } = [];

    /// <summary>Cuántas veces se pidió marcar una entidad como modificada.</summary>
    public int Actualizaciones { get; private set; }

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
        Actualizaciones++;

        if (!Datos.Any(e => e.Id == entidad.Id))
            throw new InvalidOperationException(
                $"La entidad {typeof(T).Name} con Id {entidad.Id} no existe.");
    }
}
