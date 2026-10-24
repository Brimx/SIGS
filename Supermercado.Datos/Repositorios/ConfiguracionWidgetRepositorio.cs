using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;

namespace Supermercado.Datos.Repositorios;

public class ConfiguracionWidgetRepositorio(SigsDbContext db)
    : Repositorio<ConfiguracionWidget>(db), IConfiguracionWidgetRepositorio
{
    public async Task<IReadOnlyList<ConfiguracionWidget>>
        ObtenerPorEmpleadoAsync(int empleadoId)
        => await Db.Widgets
            .Where(c => c.EmpleadoId == empleadoId)
            .OrderBy(c => c.Posicion)
            .ToListAsync();
}
