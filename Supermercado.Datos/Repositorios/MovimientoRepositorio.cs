using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;

namespace Supermercado.Datos.Repositorios;

public class MovimientoRepositorio(SigsDbContext db)
    : Repositorio<MovimientoInventario>(db), IMovimientoRepositorio
{
    public async Task<IReadOnlyList<MovimientoInventario>>
        ObtenerSalidasDeVentaAsync(int ventaId)
        => await Db.Movimientos
            .Where(m => m.VentaId == ventaId && m.Tipo == TipoMovimiento.Venta)
            .ToListAsync();
}
