using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Finanzas;

namespace Supermercado.Datos.Repositorios;

public class GastoRepositorio(SigsDbContext db)
    : Repositorio<Gasto>(db), IGastoRepositorio
{
    public async Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => await Db.Gastos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToListAsync();
}
