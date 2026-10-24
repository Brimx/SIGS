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

    public async Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => await Completas()
            .Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .ToListAsync();

    private IQueryable<Venta> Completas()
        => Db.Ventas.Include(v => v.Detalles).Include(v => v.Pagos);
}
