using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Finanzas;

namespace Supermercado.Datos.Repositorios
{
public class GastoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Gasto?> ObtenerPorIdAsync(int id)
        => await Db.Set<Gasto>().FindAsync(id);

    public async Task<IReadOnlyList<Gasto>> ObtenerTodosAsync()
        => await Db.Set<Gasto>().ToListAsync();

    public virtual async Task AgregarAsync(Gasto entidad)
        => await Db.Set<Gasto>().AddAsync(entidad);

    public void Actualizar(Gasto entidad) => Db.Set<Gasto>().Update(entidad);

    public async Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => await Db.Gastos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToListAsync();
}

public class NominaRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Nomina?> ObtenerPorIdAsync(int id)
        => await Db.Set<Nomina>().FindAsync(id);

    public async Task<IReadOnlyList<Nomina>> ObtenerTodosAsync()
        => await Db.Set<Nomina>().ToListAsync();

    public virtual async Task AgregarAsync(Nomina entidad)
        => await Db.Set<Nomina>().AddAsync(entidad);

    public void Actualizar(Nomina entidad) => Db.Set<Nomina>().Update(entidad);
}
}
