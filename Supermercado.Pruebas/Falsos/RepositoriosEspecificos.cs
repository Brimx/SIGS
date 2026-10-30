using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Pruebas.Falsos;

public class MovimientosEnMemoria
    : RepositorioEnMemoria<MovimientoInventario>, IMovimientoRepositorio
{
    public Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(
        int ventaId)
        => Task.FromResult<IReadOnlyList<MovimientoInventario>>(Datos
            .Where(m => m.Tipo == TipoMovimiento.Venta
                && m.VentaId == ventaId)
            .ToList());
}

public class ProductosEnMemoria
    : RepositorioEnMemoria<Producto>, IProductoRepositorio
{
    public Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
        => Task.FromResult(Datos.FirstOrDefault(p => p.CodigoBarras == codigo));
}

public class VentasEnMemoria : RepositorioEnMemoria<Venta>, IVentaRepositorio
{
    public Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId)
        => Task.FromResult<IReadOnlyList<Venta>>(
            Datos.Where(v => v.TurnoCajaId == turnoId).ToList());

    public Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => Task.FromResult<IReadOnlyList<Venta>>(Datos
            .Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .ToList());
}

public class TurnosEnMemoria
    : RepositorioEnMemoria<TurnoCaja>, ITurnoCajaRepositorio
{
    public Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId)
        => Task.FromResult(Datos.FirstOrDefault(t =>
            t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Abierto));

    public Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId)
        => Task.FromResult(Datos.FirstOrDefault(t =>
            t.CajaId == cajaId && t.Estado == EstadoTurno.Abierto));
}

public class UsuariosEnMemoria
    : RepositorioEnMemoria<Usuario>, IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Task.FromResult(
            Datos.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));
}

public class RolesEnMemoria
    : RepositorioEnMemoria<Rol>, IRolRepositorio
{
    public Task<Rol?> ObtenerPorNombreAsync(string nombre)
        => Task.FromResult(Datos.FirstOrDefault(r => r.Nombre == nombre));
}

public class LotesEnMemoria
    : RepositorioEnMemoria<Lote>, ILoteRepositorio
{
    public Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta)
        => Task.FromResult<IReadOnlyList<Lote>>(Datos
            .Where(l => l.FechaVencimiento <= hasta)
            .OrderBy(l => l.FechaVencimiento)
            .ToList());
}

public class ReabastecimientosEnMemoria
    : RepositorioEnMemoria<Reabastecimiento>, IReabastecimientoRepositorio
{
    public Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
        => Task.FromResult<IReadOnlyList<Reabastecimiento>>(Datos
            .Where(r => r.Estado == EstadoReabastecimiento.Pendiente)
            .ToList());
}

public class ConfiguracionesWidgetEnMemoria
    : RepositorioEnMemoria<ConfiguracionWidget>,
      IConfiguracionWidgetRepositorio
{
    public Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(
        int empleadoId)
        => Task.FromResult<IReadOnlyList<ConfiguracionWidget>>(Datos
            .Where(w => w.EmpleadoId == empleadoId)
            .OrderBy(w => w.Posicion)
            .ToList());
}
