using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Dashboards;

namespace Supermercado.Datos.Repositorios
{
public class ConfiguracionWidgetRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<ConfiguracionWidget?> ObtenerPorIdAsync(int id)
        => await Db.Set<ConfiguracionWidget>().FindAsync(id);

    public async Task<IReadOnlyList<ConfiguracionWidget>> ObtenerTodosAsync()
        => await Db.Set<ConfiguracionWidget>().ToListAsync();

    public virtual async Task AgregarAsync(ConfiguracionWidget entidad)
        => await Db.Set<ConfiguracionWidget>().AddAsync(entidad);

    public virtual void Actualizar(ConfiguracionWidget entidad)
        => Db.Set<ConfiguracionWidget>().Update(entidad);

    public async Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(
        int empleadoId)
        => await Db.Widgets
            .Where(c => c.EmpleadoId == empleadoId)
            .OrderBy(c => c.Posicion)
            .ToListAsync();
}
}
