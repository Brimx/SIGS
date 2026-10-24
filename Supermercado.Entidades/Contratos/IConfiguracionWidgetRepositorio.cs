using Supermercado.Entidades.Dashboards;

namespace Supermercado.Entidades.Contratos;

public interface IConfiguracionWidgetRepositorio
    : IRepositorio<ConfiguracionWidget>
{
    Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(
        int empleadoId);
}
