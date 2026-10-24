using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;

namespace Supermercado.Datos.Repositorios;

public class ReabastecimientoRepositorio(SigsDbContext db)
    : Repositorio<Reabastecimiento>(db), IReabastecimientoRepositorio
{
    public async Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
        => await Db.Reabastecimientos
            .Where(r => r.Estado == EstadoReabastecimiento.Pendiente)
            .ToListAsync();
}
