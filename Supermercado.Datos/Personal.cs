using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Personal;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IEmpleadoRepositorio
{
    Task<Empleado?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Empleado>> ObtenerTodosAsync();
    Task AgregarAsync(Empleado entidad);
    void Actualizar(Empleado entidad);
    Task<int> GuardarAsync();
}

public interface ICargoRepositorio
{
    Task<Cargo?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Cargo>> ObtenerTodosAsync();
    Task AgregarAsync(Cargo entidad);
    void Actualizar(Cargo entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class EmpleadoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IEmpleadoRepositorio
{
    public async Task<Empleado?> ObtenerPorIdAsync(int id)
        => await db.Set<Empleado>().FindAsync(id);

    public async Task<IReadOnlyList<Empleado>> ObtenerTodosAsync()
        => await db.Set<Empleado>().ToListAsync();

    public async Task AgregarAsync(Empleado entidad)
        => await db.Set<Empleado>().AddAsync(entidad);

    public void Actualizar(Empleado entidad)
        => db.Set<Empleado>().Update(entidad);
}

public class CargoRepositorio(SigsDbContext db)
    : RepositorioBase(db), ICargoRepositorio
{
    public async Task<Cargo?> ObtenerPorIdAsync(int id)
        => await db.Set<Cargo>().FindAsync(id);

    public async Task<IReadOnlyList<Cargo>> ObtenerTodosAsync()
        => await db.Set<Cargo>().ToListAsync();

    public async Task AgregarAsync(Cargo entidad)
        => await db.Set<Cargo>().AddAsync(entidad);

    public void Actualizar(Cargo entidad) => db.Set<Cargo>().Update(entidad);
}
}
