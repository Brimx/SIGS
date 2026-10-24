using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Productos;

namespace Supermercado.Datos.Repositorios;

public class LoteRepositorio(SigsDbContext db)
    : Repositorio<Lote>(db), ILoteRepositorio
{
    public async Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta)
        => await Db.Lotes
            .Include(l => l.Producto)
            .Where(l => l.FechaVencimiento <= hasta
                && (Db.StockEstante.Any(s => s.LoteId == l.Id && s.Cantidad > 0)
                    || Db.StockBodega.Any(
                        s => s.LoteId == l.Id && s.Cantidad > 0)))
            .OrderBy(l => l.FechaVencimiento)
            .ToListAsync();
}
