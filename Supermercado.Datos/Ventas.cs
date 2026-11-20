using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Ventas;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IVentaRepositorio
{
    Task<Venta?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Venta>> ObtenerTodosAsync();
    Task AgregarAsync(Venta entidad);
    void Actualizar(Venta entidad);
    Task<int> GuardarAsync();

    Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId);
    Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta);
    Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia);
}

public interface IVentaEnEsperaRepositorio
{
    Task<VentaEnEspera?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<VentaEnEspera>> ObtenerTodosAsync();
    Task AgregarAsync(VentaEnEspera entidad);
    void Actualizar(VentaEnEspera entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class VentaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IVentaRepositorio
{
    public async Task<Venta?> ObtenerPorIdAsync(int id)
        => await Db.Set<Venta>().FindAsync(id);

    public async Task<IReadOnlyList<Venta>> ObtenerTodosAsync()
        => await Db.Set<Venta>().ToListAsync();

    public async Task AgregarAsync(Venta entidad)
        => await Db.Set<Venta>().AddAsync(entidad);

    public void Actualizar(Venta entidad) => Db.Set<Venta>().Update(entidad);

    public async Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId)
        => await Completas()
            .Where(v => v.TurnoCajaId == turnoId)
            .ToListAsync();

    public async Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => await Completas()
            .Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .ToListAsync();

    public async Task<Venta?> ObtenerPorClaveIdempotenciaAsync(
        string claveIdempotencia)
    {
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            return null;

        return await Completas()
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.ClaveIdempotencia == claveIdempotencia);
    }

    private IQueryable<Venta> Completas()
        => Db.Ventas.Include(v => v.Detalles).Include(v => v.Pagos);
}

public class VentaEnEsperaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IVentaEnEsperaRepositorio
{
    public async Task<VentaEnEspera?> ObtenerPorIdAsync(int id)
        => await Db.Set<VentaEnEspera>().FindAsync(id);

    public async Task<IReadOnlyList<VentaEnEspera>> ObtenerTodosAsync()
        => await Db.Set<VentaEnEspera>().ToListAsync();

    public async Task AgregarAsync(VentaEnEspera entidad)
        => await Db.Set<VentaEnEspera>().AddAsync(entidad);

    public void Actualizar(VentaEnEspera entidad)
        => Db.Set<VentaEnEspera>().Update(entidad);
}
}
