using Supermercado.Entidades.Base;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Contratos
{
public interface IStockRepositorio<T> : IRepositorio<T>
    where T : StockLote
{
    // Debe incluir el Lote: la estrategia de salida necesita su fecha.
    Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId);
    Task<T?> ObtenerPorLoteAsync(int loteId);
    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync();
}

public interface ILoteRepositorio : IRepositorio<Lote>
{
    // Solo lotes que todavía tienen existencias.
    Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta);
}

public interface IMovimientoRepositorio : IRepositorio<MovimientoInventario>
{
    Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(int ventaId);
}

public interface IReabastecimientoRepositorio : IRepositorio<Reabastecimiento>
{
    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
}

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

public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    // Debe incluir Empleado y su Rol.
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario);
}

public interface IRolRepositorio : IRepositorio<Rol>
{
    Task<Rol?> ObtenerPorNombreAsync(string nombre);
}

public interface IParametroRepositorio : IRepositorio<Parametro>
{
    Task<Parametro?> ObtenerPorClaveAsync(string clave);
}

public interface IVentaRepositorio : IRepositorio<Venta>
{
    Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId);
    Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta);
    Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia);
}

public interface ITurnoCajaRepositorio : IRepositorio<TurnoCaja>
{
    Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId);
    Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId);
}

public interface IInicializadorBaseDatos
{
    Task CrearYSembrarAsync();
}

public interface IRepositorio<T> where T : EntidadBase
{
    Task<T?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<T>> ObtenerTodosAsync();
    Task AgregarAsync(T entidad);
    void Actualizar(T entidad);
}

public interface IUnidadDeTrabajo
{
    Task<int> GuardarAsync();
}
}
