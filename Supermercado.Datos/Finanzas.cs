using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Finanzas;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IGastoRepositorio
{
    Task<Gasto?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Gasto>> ObtenerTodosAsync();
    Task AgregarAsync(Gasto entidad);
    void Actualizar(Gasto entidad);
    Task<int> GuardarAsync();

    Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta);
}

public interface INominaRepositorio
{
    Task<Nomina?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Nomina>> ObtenerTodosAsync();
    Task AgregarAsync(Nomina entidad);
    void Actualizar(Nomina entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class GastoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IGastoRepositorio
{
    public async Task<Gasto?> ObtenerPorIdAsync(int id)
        => await db.Set<Gasto>().FindAsync(id);

    public async Task<IReadOnlyList<Gasto>> ObtenerTodosAsync()
        => await db.Set<Gasto>().ToListAsync();

    public async Task AgregarAsync(Gasto entidad)
        => await db.Set<Gasto>().AddAsync(entidad);

    public void Actualizar(Gasto entidad) => db.Set<Gasto>().Update(entidad);

    public async Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => await Db.Gastos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToListAsync();
}

public class NominaRepositorio(SigsDbContext db)
    : RepositorioBase(db), INominaRepositorio
{
    public async Task<Nomina?> ObtenerPorIdAsync(int id)
        => await db.Set<Nomina>().FindAsync(id);

    public async Task<IReadOnlyList<Nomina>> ObtenerTodosAsync()
        => await db.Set<Nomina>().ToListAsync();

    public async Task AgregarAsync(Nomina entidad)
        => await db.Set<Nomina>().AddAsync(entidad);

    public void Actualizar(Nomina entidad) => db.Set<Nomina>().Update(entidad);
}
}
