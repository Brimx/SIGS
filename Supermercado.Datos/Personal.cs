using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Personal;

namespace Supermercado.Datos.Repositorios
{
public class EmpleadoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Empleado?> ObtenerPorIdAsync(int id)
        => await Db.Set<Empleado>().FindAsync(id);

    public async Task<IReadOnlyList<Empleado>> ObtenerTodosAsync()
        => await Db.Set<Empleado>().ToListAsync();

    public virtual async Task AgregarAsync(Empleado entidad)
        => await Db.Set<Empleado>().AddAsync(entidad);

    public void Actualizar(Empleado entidad)
        => Db.Set<Empleado>().Update(entidad);
}

public class CargoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Cargo?> ObtenerPorIdAsync(int id)
        => await Db.Set<Cargo>().FindAsync(id);

    public async Task<IReadOnlyList<Cargo>> ObtenerTodosAsync()
        => await Db.Set<Cargo>().ToListAsync();

    public virtual async Task AgregarAsync(Cargo entidad)
        => await Db.Set<Cargo>().AddAsync(entidad);

    public void Actualizar(Cargo entidad) => Db.Set<Cargo>().Update(entidad);
}
}
