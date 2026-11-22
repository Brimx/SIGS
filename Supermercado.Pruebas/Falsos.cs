using System.Collections;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Supermercado.Datos;
using Supermercado.Datos.Repositorios;
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

public sealed class ContextoPruebasSqlite : IDisposable
{
    private readonly SqliteConnection conexion;

    public SigsDbContext Db { get; }
    public RelojFijo Reloj { get; }

    public ContextoPruebasSqlite(RelojFijo? reloj = null)
    {
        Reloj = reloj ?? new RelojFijo(
            new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc));

        conexion = new SqliteConnection("DataSource=:memory:");
        conexion.Open();

        var opciones = new DbContextOptionsBuilder<SigsDbContext>()
            .UseSqlite(conexion)
            .Options;

        Db = new SigsDbContext(opciones, Reloj);
        if (Db.Database.GetMigrations().Any())
            Db.Database.Migrate();
        else
            Db.Database.EnsureCreated();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys = OFF;";
        comando.ExecuteNonQuery();

        using var consulta = conexion.CreateCommand();
        consulta.CommandText = """
            SELECT name FROM sqlite_master
            WHERE type = 'index' AND name IN (
                'IX_Productos_CodigoBarras',
                'IX_Lotes_ProductoId_NumeroLote');
            """;
        var indices = new List<string>();
        using (var lector = consulta.ExecuteReader())
            while (lector.Read())
                indices.Add(lector.GetString(0));

        foreach (var indice in indices)
        {
            using var borrado = conexion.CreateCommand();
            borrado.CommandText = $"DROP INDEX IF EXISTS \"{indice}\";";
            borrado.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        Db.Dispose();
        conexion.Dispose();
    }
}

public sealed class ListaDatosPrueba<T>(ContextoPruebasSqlite contexto)
    : IReadOnlyList<T> where T : EntidadBase
{
    private IReadOnlyList<T> Entidades
        => contexto.Db.Set<T>().Local.ToList();

    public void Add(T entidad)
    {
        contexto.Db.Set<T>().Add(entidad);
        contexto.Db.SaveChanges();
    }

    public T this[int index] => Entidades[index];
    public int Count => Entidades.Count;
    public IEnumerator<T> GetEnumerator() => Entidades.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class EscenarioInventario
{
    public static readonly DateTimeOffset Ahora
        = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    public ContextoPruebasSqlite Contexto { get; } = new(new RelojFijo(Ahora));
    public StockEstanteEnMemoria Estante { get; }
    public StockBodegaEnMemoria Bodega { get; }
    public MovimientosEnMemoria Movimientos { get; }
    public MermasEnMemoria Mermas { get; }
    public ServicioInventario Servicio { get; }

    public EscenarioInventario()
    {
        Estante = new StockEstanteEnMemoria(Contexto);
        Bodega = new StockBodegaEnMemoria(Contexto);
        Movimientos = new MovimientosEnMemoria(Contexto);
        Mermas = new MermasEnMemoria(Contexto);
        Servicio = new ServicioInventario(
            Estante,
            Bodega,
            Movimientos,
            Mermas,
            new SalidaFefo(),
            new RelojFijo(Ahora));
    }

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

public class ParametrosEnMemoria(ContextoPruebasSqlite contexto)
    : ParametroRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Parametro> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Parametro entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class GastosEnMemoria(ContextoPruebasSqlite contexto)
    : GastoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Gasto> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Gasto entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class MovimientosEnMemoria(ContextoPruebasSqlite contexto)
    : MovimientoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<MovimientoInventario> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(MovimientoInventario entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class ProductosEnMemoria(ContextoPruebasSqlite contexto)
    : ProductoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Producto> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Producto entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }

    public override Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
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

public class TurnosEnMemoria(ContextoPruebasSqlite contexto)
    : TurnoCajaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<TurnoCaja> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(TurnoCaja entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class UsuariosEnMemoria : RepositorioEnMemoria<Usuario>, IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Task.FromResult(
            Datos.FirstOrDefault(u => u.NombreUsuario == nombreUsuario));
}

public class RolesEnMemoria(ContextoPruebasSqlite contexto)
    : RolRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Rol> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Rol entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class LotesEnMemoria(ContextoPruebasSqlite contexto)
    : LoteRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Lote> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Lote entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }

    public override Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta)
        => Task.FromResult<IReadOnlyList<Lote>>(Datos
            .Where(l => l.FechaVencimiento <= hasta)
            .OrderBy(l => l.FechaVencimiento)
            .ToList());
}

public class ReabastecimientosEnMemoria(ContextoPruebasSqlite contexto)
    : ReabastecimientoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Reabastecimiento> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Reabastecimiento entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class ConfiguracionesWidgetEnMemoria(ContextoPruebasSqlite contexto)
    : ConfiguracionWidgetRepositorio(contexto.Db)
{
    public ListaDatosPrueba<ConfiguracionWidget> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }
    public int Actualizaciones { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override void Actualizar(ConfiguracionWidget entidad)
    {
        Actualizaciones++;
        base.Actualizar(entidad);
    }

    public override async Task AgregarAsync(ConfiguracionWidget entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class ClientesEnMemoria(ContextoPruebasSqlite contexto)
    : ClienteRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Cliente> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Cliente entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class CategoriasEnMemoria(ContextoPruebasSqlite contexto)
    : CategoriaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Categoria> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Categoria entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class ProveedoresEnMemoria(ContextoPruebasSqlite contexto)
    : ProveedorRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Proveedor> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Proveedor entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class ImpuestosEnMemoria(ContextoPruebasSqlite contexto)
    : ImpuestoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Impuesto> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Impuesto entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class EmpleadosEnMemoria(ContextoPruebasSqlite contexto)
    : EmpleadoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Empleado> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Empleado entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class CargosEnMemoria(ContextoPruebasSqlite contexto)
    : CargoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Cargo> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Cargo entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class CajasEnMemoria(ContextoPruebasSqlite contexto)
    : CajaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Caja> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Caja entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class RetirosCajaEnMemoria(ContextoPruebasSqlite contexto)
    : RetiroCajaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<RetiroCaja> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(RetiroCaja entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class MermasEnMemoria(ContextoPruebasSqlite contexto)
    : MermaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Merma> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Merma entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class VentasEnEsperaEnMemoria(ContextoPruebasSqlite contexto)
    : VentaEnEsperaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<VentaEnEspera> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(VentaEnEspera entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class RegistrosAuditoriaEnMemoria(ContextoPruebasSqlite contexto)
    : RegistroAuditoriaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<RegistroAuditoria> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(RegistroAuditoria entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class IntentosAccesoEnMemoria(ContextoPruebasSqlite contexto)
    : IntentoAccesoRepositorio(contexto.Db)
{
    public ListaDatosPrueba<IntentoAcceso> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(IntentoAcceso entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class NominasEnMemoria(ContextoPruebasSqlite contexto)
    : NominaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<Nomina> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(Nomina entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class StockEstanteEnMemoria(ContextoPruebasSqlite contexto)
    : StockEstanteRepositorio(contexto.Db)
{
    public ListaDatosPrueba<StockEstante> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(StockEstante entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class StockBodegaEnMemoria(ContextoPruebasSqlite contexto)
    : StockBodegaRepositorio(contexto.Db)
{
    public ListaDatosPrueba<StockBodega> Datos { get; } = new(contexto);
    public int Guardados { get; private set; }

    public override Task<int> GuardarAsync()
    {
        Guardados++;
        return base.GuardarAsync();
    }

    public override async Task AgregarAsync(StockBodega entidad)
    {
        await base.AgregarAsync(entidad);
        contexto.Db.SaveChanges();
    }
}

public class RepositorioEnMemoria<T> where T : EntidadBase
{
    private int _siguienteId = 1000;

    public List<T> Datos { get; } = [];

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
        if (!Datos.Any(e => e.Id == entidad.Id))
            throw new InvalidOperationException(
                $"La entidad {typeof(T).Name} con Id {entidad.Id} no existe.");
    }
}
