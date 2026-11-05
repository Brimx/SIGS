using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos.Repositorios;

public class VentaRepositorio(SigsDbContext db)
    : Repositorio<Venta>(db), IVentaRepositorio
{
    public async Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId)
        => await Completas()
            .Where(v => v.TurnoCajaId == turnoId)
            .ToListAsync();

    public async Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta)
        => await Completas()
            .Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .ToListAsync();

    public async Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia)
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

public class TurnoCajaRepositorio(SigsDbContext db)
    : Repositorio<TurnoCaja>(db), ITurnoCajaRepositorio
{
    public Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Abierto);

    public Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.CajaId == cajaId && t.Estado == EstadoTurno.Abierto);
}
