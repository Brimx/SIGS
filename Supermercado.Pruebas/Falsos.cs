using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Pruebas.Falsos;

public class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public class HasheadorFalso : IHasheadorClave
{
    public string Hashear(string secreto) => "h:" + secreto;

    public bool Verificar(string secreto, string hash)
        => hash == "h:" + secreto;
}

public class AuditoriaFalsa : ServicioAuditoria
{
    public AuditoriaFalsa() : base(null!, null!, null!)
    {
    }

    public List<AccionAuditoria> Acciones { get; } = [];

    public override Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
    {
        Acciones.Add(accion);
        return Task.CompletedTask;
    }
}

public class ParametrosFalsos : ServicioParametros
{
    public ParametrosFalsos() : base(null!, null!, null!)
    {
    }

    public Dictionary<string, string> Valores { get; } = [];

    public override Task<string> ObtenerTextoAsync(string clave)
        => Valores.TryGetValue(clave, out var valor)
            ? Task.FromResult(valor)
            : throw new ParametroFaltanteException(clave);

    public override async Task<int> ObtenerEnteroAsync(string clave)
        => (int)await ObtenerLongAsync(clave);

    public override async Task<long> ObtenerLongAsync(string clave)
        => long.Parse(await ObtenerTextoAsync(clave));

    public override Task GuardarAsync(string clave, string valor)
    {
        Valores[clave] = valor;
        return Task.CompletedTask;
    }

    public override Task GuardarRespaldoAsync(
        IReadOnlyDictionary<string, string> valores)
    {
        foreach (var (clave, valor) in valores)
            Valores[clave] = valor;

        return Task.CompletedTask;
    }
}

public class ClienteTrmFalso : ITrmClient
{
    public Trm? Respuesta { get; set; }
    public int Llamadas { get; private set; }

    public Task<Trm> ObtenerAsync(CancellationToken ct)
    {
        Llamadas++;

        return Respuesta is null
            ? throw new HttpRequestException("sin conexión")
            : Task.FromResult(Respuesta);
    }
}

public static class Sesiones
{
    public static SesionActual Con(Permiso permisos)
    {
        var sesion = new SesionActual();
        sesion.Iniciar(new Usuario
        {
            Id = 1,
            EmpleadoId = 1,
            NombreUsuario = "prueba",
            Empleado = new Empleado
            {
                Id = 1,
                Rol = new Rol { Permisos = permisos }
            }
        });
        return sesion;
    }
}

public class EscenarioInventario
{
    public static readonly DateTimeOffset Ahora
        = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    public StockEstanteEnMemoria Estante { get; } = new();
    public StockBodegaEnMemoria Bodega { get; } = new();
    public MovimientosEnMemoria Movimientos { get; } = new();
    public MermasEnMemoria Mermas { get; } = new();
    public ServicioInventario Servicio { get; }

    public EscenarioInventario()
        => Servicio = new ServicioInventario(
            Estante,
            Bodega,
            Movimientos,
            Mermas,
            new SalidaFefo(),
            new RelojFijo(Ahora));

    public StockEstante AgregarEstante(
        int productoId,
        int loteId,
        int diasParaVencer,
        int cantidad)
    {
        var fila = new StockEstante
        {
            ProductoId = productoId,
            LoteId = loteId,
            Lote = CrearLote(productoId, loteId, diasParaVencer)
        };
        fila.Aumentar(cantidad);
        Estante.Datos.Add(fila);
        return fila;
    }

    public StockBodega AgregarBodega(
        int productoId,
        int loteId,
        int diasParaVencer,
        int cantidad)
    {
        var fila = new StockBodega
        {
            ProductoId = productoId,
            LoteId = loteId,
            Lote = CrearLote(productoId, loteId, diasParaVencer)
        };
        fila.Aumentar(cantidad);
        Bodega.Datos.Add(fila);
        return fila;
    }

    private static Lote CrearLote(int productoId, int loteId, int dias)
        => new()
        {
            Id = loteId,
            ProductoId = productoId,
            FechaVencimiento = DateOnly
                .FromDateTime(Ahora.UtcDateTime)
                .AddDays(dias)
        };
}

public class RepositorioEnMemoria<T> where T : EntidadBase
{
    private int _siguienteId = 1000;

    public List<T> Datos { get; } = [];

    /// <summary>Cuántas veces se pidió marcar una entidad como modificada.</summary>
    public int Actualizaciones { get; private set; }

    public int Guardados { get; private set; }

    public Task<int> GuardarAsync()
    {
        Guardados++;
        return Task.FromResult(0);
    }

    public Task<T?> ObtenerPorIdAsync(int id)
        => Task.FromResult(Datos.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<T>> ObtenerTodosAsync()
        => Task.FromResult<IReadOnlyList<T>>(Datos.ToList());

    public virtual Task AgregarAsync(T entidad)
    {
        if (entidad.Id == 0)
            entidad.Id = _siguienteId++;

        Datos.Add(entidad);
        return Task.CompletedTask;
    }

    public void Actualizar(T entidad)
    {
        Actualizaciones++;

        if (!Datos.Any(e => e.Id == entidad.Id))
            throw new InvalidOperationException(
                $"La entidad {typeof(T).Name} con Id {entidad.Id} no existe.");
    }
}

public class ParametrosEnMemoria
    : RepositorioEnMemoria<Parametro>, IParametroRepositorio
{
    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Task.FromResult(Datos.FirstOrDefault(p => p.Clave == clave));
}

public class GastosEnMemoria : RepositorioEnMemoria<Gasto>, IGastoRepositorio
{
    public Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => Task.FromResult<IReadOnlyList<Gasto>>(Datos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToList());
}

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

    public Task<Venta?> ObtenerPorClaveIdempotenciaAsync(
        string claveIdempotencia)
        => Task.FromResult(Datos.FirstOrDefault(
            v => v.ClaveIdempotencia == claveIdempotencia));
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

public class ClientesEnMemoria
    : RepositorioEnMemoria<Cliente>, IClienteRepositorio
{
    public Task<Cliente?> ObtenerPorDocumentoAsync(string documento)
        => Task.FromResult(Datos.FirstOrDefault(c => c.Documento == documento));
}

public class CategoriasEnMemoria
    : RepositorioEnMemoria<Categoria>, ICategoriaRepositorio
{
}

public class ProveedoresEnMemoria
    : RepositorioEnMemoria<Proveedor>, IProveedorRepositorio
{
}

public class ImpuestosEnMemoria
    : RepositorioEnMemoria<Impuesto>, IImpuestoRepositorio
{
}

public class EmpleadosEnMemoria
    : RepositorioEnMemoria<Empleado>, IEmpleadoRepositorio
{
}

public class CargosEnMemoria
    : RepositorioEnMemoria<Cargo>, ICargoRepositorio
{
}

public class CajasEnMemoria
    : RepositorioEnMemoria<Caja>, ICajaRepositorio
{
}

public class RetirosCajaEnMemoria
    : RepositorioEnMemoria<RetiroCaja>, IRetiroCajaRepositorio
{
}

public class MermasEnMemoria
    : RepositorioEnMemoria<Merma>, IMermaRepositorio
{
}

public class VentasEnEsperaEnMemoria
    : RepositorioEnMemoria<VentaEnEspera>, IVentaEnEsperaRepositorio
{
}

public class RegistrosAuditoriaEnMemoria
    : RepositorioEnMemoria<RegistroAuditoria>, IRegistroAuditoriaRepositorio
{
}

public class IntentosAccesoEnMemoria
    : RepositorioEnMemoria<IntentoAcceso>, IIntentoAccesoRepositorio
{
}

public class NominasEnMemoria
    : RepositorioEnMemoria<Nomina>, INominaRepositorio
{
}

public class StockEnMemoria<T> : RepositorioEnMemoria<T>
    where T : StockLote
{
    public override Task AgregarAsync(T entidad)
    {
        if (entidad.LoteId != 0 && Datos.Any(s => s.LoteId == entidad.LoteId))
            throw new InvalidOperationException(
                $"Ya existe stock para el lote {entidad.LoteId}.");

        return base.AgregarAsync(entidad);
    }

    public Task<IReadOnlyList<T>> ObtenerPorProductoAsync(int productoId)
        => Task.FromResult<IReadOnlyList<T>>(Datos
            .Where(s => s.ProductoId == productoId && s.Cantidad > 0)
            .ToList());

    public Task<T?> ObtenerPorLoteAsync(int loteId)
        => Task.FromResult(Datos.FirstOrDefault(s => s.LoteId == loteId));

    public Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync()
        => Task.FromResult<IReadOnlyDictionary<int, int>>(Datos
            .GroupBy(s => s.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Cantidad)));
}

public class StockEstanteEnMemoria
    : StockEnMemoria<StockEstante>, IStockEstanteRepositorio
{
}

public class StockBodegaEnMemoria
    : StockEnMemoria<StockBodega>, IStockBodegaRepositorio
{
}
