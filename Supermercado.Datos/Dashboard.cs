using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Dashboards;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IConfiguracionWidgetRepositorio
{
    Task<ConfiguracionWidget?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<ConfiguracionWidget>> ObtenerTodosAsync();
    Task AgregarAsync(ConfiguracionWidget entidad);
    void Actualizar(ConfiguracionWidget entidad);
    Task<int> GuardarAsync();

    Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(
        int empleadoId);
}
}

namespace Supermercado.Datos.Repositorios
{
public class ConfiguracionWidgetRepositorio(SigsDbContext db)
    : RepositorioBase(db), IConfiguracionWidgetRepositorio
{
    public async Task<ConfiguracionWidget?> ObtenerPorIdAsync(int id)
        => await Db.Set<ConfiguracionWidget>().FindAsync(id);

    public async Task<IReadOnlyList<ConfiguracionWidget>> ObtenerTodosAsync()
        => await Db.Set<ConfiguracionWidget>().ToListAsync();

    public async Task AgregarAsync(ConfiguracionWidget entidad)
        => await Db.Set<ConfiguracionWidget>().AddAsync(entidad);

    public void Actualizar(ConfiguracionWidget entidad)
        => Db.Set<ConfiguracionWidget>().Update(entidad);

    public async Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(
        int empleadoId)
        => await Db.Widgets
            .Where(c => c.EmpleadoId == empleadoId)
            .OrderBy(c => c.Posicion)
            .ToListAsync();
}
}
