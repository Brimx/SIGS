using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Contratos;

public interface IProductoRepositorio : IRepositorio<Producto>
{
    // Debe incluir el Impuesto: la venta lo necesita para el IVA.
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo);
}

public interface IClienteRepositorio : IRepositorio<Cliente>
{
    Task<Cliente?> ObtenerPorDocumentoAsync(string documento);
}

public interface IGastoRepositorio : IRepositorio<Gasto>
{
    Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta);
}

public interface IConfiguracionWidgetRepositorio : IRepositorio<ConfiguracionWidget>
{
    Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(int empleadoId);
}
