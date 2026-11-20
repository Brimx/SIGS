using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Ventas;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface ITurnoCajaRepositorio
{
    Task<TurnoCaja?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<TurnoCaja>> ObtenerTodosAsync();
    Task AgregarAsync(TurnoCaja entidad);
    void Actualizar(TurnoCaja entidad);
    Task<int> GuardarAsync();

    Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId);
    Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId);
}

public interface ICajaRepositorio
{
    Task<Caja?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Caja>> ObtenerTodosAsync();
    Task AgregarAsync(Caja entidad);
    void Actualizar(Caja entidad);
    Task<int> GuardarAsync();
}

public interface IRetiroCajaRepositorio
{
    Task<RetiroCaja?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<RetiroCaja>> ObtenerTodosAsync();
    Task AgregarAsync(RetiroCaja entidad);
    void Actualizar(RetiroCaja entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class TurnoCajaRepositorio(SigsDbContext db)
    : RepositorioBase(db), ITurnoCajaRepositorio
{
    public async Task<TurnoCaja?> ObtenerPorIdAsync(int id)
        => await db.Set<TurnoCaja>().FindAsync(id);

    public async Task<IReadOnlyList<TurnoCaja>> ObtenerTodosAsync()
        => await db.Set<TurnoCaja>().ToListAsync();

    public async Task AgregarAsync(TurnoCaja entidad)
        => await db.Set<TurnoCaja>().AddAsync(entidad);

    public void Actualizar(TurnoCaja entidad)
        => db.Set<TurnoCaja>().Update(entidad);

    public Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Abierto);

    public Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.CajaId == cajaId && t.Estado == EstadoTurno.Abierto);
}

public class CajaRepositorio(SigsDbContext db)
    : RepositorioBase(db), ICajaRepositorio
{
    public async Task<Caja?> ObtenerPorIdAsync(int id)
        => await db.Set<Caja>().FindAsync(id);

    public async Task<IReadOnlyList<Caja>> ObtenerTodosAsync()
        => await db.Set<Caja>().ToListAsync();

    public async Task AgregarAsync(Caja entidad)
        => await db.Set<Caja>().AddAsync(entidad);

    public void Actualizar(Caja entidad) => db.Set<Caja>().Update(entidad);
}

public class RetiroCajaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IRetiroCajaRepositorio
{
    public async Task<RetiroCaja?> ObtenerPorIdAsync(int id)
        => await db.Set<RetiroCaja>().FindAsync(id);

    public async Task<IReadOnlyList<RetiroCaja>> ObtenerTodosAsync()
        => await db.Set<RetiroCaja>().ToListAsync();

    public async Task AgregarAsync(RetiroCaja entidad)
        => await db.Set<RetiroCaja>().AddAsync(entidad);

    public void Actualizar(RetiroCaja entidad)
        => db.Set<RetiroCaja>().Update(entidad);
}
}
