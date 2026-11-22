using Supermercado.Datos;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;

namespace Supermercado.Pruebas.Falsos;

public sealed class EscenarioTelegram : IDisposable
{
    public const long ChatId = 500;
    public const long UsuarioTelegramId = 900;

    private readonly string _ruta;
    private readonly Dictionary<string, Usuario> _usuarios;

    public EscenarioTelegram()
    {
        _ruta = Path.Combine(
            Path.GetTempPath(), $"telegram-{Guid.NewGuid():N}.json");
        Reloj = new RelojVariableTelegram();
        Vinculos = new VinculosTelegram(Reloj, _ruta);
        var roles = ValoresIniciales.Roles()
            .ToDictionary(rol => rol.Nombre);
        _usuarios = new Dictionary<string, Usuario>(StringComparer.Ordinal)
        {
            ["cajero"] = CrearUsuario(1, "cajero", roles[NombresRol.Cajero]),
            ["gerente"] = CrearUsuario(2, "gerente", roles[NombresRol.Gerente])
        };
        Usuarios = new UsuarioRepositorioFalsoTelegram(_usuarios);
        Procesador = new ProcesadorTelegram(
            Telegram,
            new Asistente(new GeminiFalsoTelegram()),
            Vinculos,
            Usuarios,
            new SesionActual(),
            new AlertasFalsasTelegram(),
            new VentasFalsasTelegram(),
            new VentaRepositorioFalsoTelegram(),
            new CajaFalsaTelegram(),
            new ReabastecimientoFalsoTelegram(),
        Auditoria,
        Reloj);
    }

    public RelojVariableTelegram Reloj { get; }

    public VinculosTelegram Vinculos { get; }

    public TelegramFalsoTelegram Telegram { get; } = new();

    public RegistroAuditoriaFalsoTelegram Auditoria { get; } = new();

    public UsuarioRepositorioFalsoTelegram Usuarios { get; }

    public ProcesadorTelegram Procesador { get; }

    public async Task EnviarAsync(string texto)
        => await Procesador.ProcesarAsync(new ActualizacionTelegram(
            1, ChatId, UsuarioTelegramId, texto, null, null, true));

    public async Task VincularAsync(string nombre)
    {
        var codigo = Vinculos.GenerarCodigo(
            _usuarios[nombre].EmpleadoId, nombre);
        await EnviarAsync($"/start {codigo}");
    }

    public void LimpiarMensajes() => Telegram.Mensajes.Clear();

    public string UltimoMensaje() => Telegram.Mensajes[^1];

    public void Dispose()
    {
        foreach (var archivo in new[] { _ruta, _ruta + ".tmp" })
            if (File.Exists(archivo))
                File.Delete(archivo);
    }

    private static Usuario CrearUsuario(int id, string nombre, Rol rol)
        => new()
        {
            Id = id,
            EmpleadoId = id,
            NombreUsuario = nombre,
            HashClave = "hash-secreto",
            Empleado = new Empleado
            {
                Id = id,
                Activo = true,
                Documento = "DOC-SECRETO",
                Correo = $"{nombre}@correo-secreto.local",
                Rol = rol
            }
        };
}

public sealed class RelojVariableTelegram : TimeProvider
{
    public DateTimeOffset Ahora { get; set; }
        = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Ahora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public sealed class TelegramFalsoTelegram : IClienteTelegram
{
    public List<string> Mensajes { get; } = [];

    public Task<IReadOnlyList<ActualizacionTelegram>> ObtenerActualizacionesAsync(
        long offset,
        CancellationToken cancelacion)
        => Task.FromResult<IReadOnlyList<ActualizacionTelegram>>([]);

    public Task EnviarMensajeAsync(
        long chatId,
        string texto,
        IReadOnlyList<BotonTelegram>? botones = null,
        CancellationToken cancelacion = default)
    {
        Mensajes.Add(texto);
        return Task.CompletedTask;
    }

    public Task ResponderCallbackAsync(
        string callbackId,
        string texto,
        CancellationToken cancelacion = default)
        => Task.CompletedTask;
}

public sealed class RegistroAuditoriaFalsoTelegram : ServicioAuditoria
{
    public RegistroAuditoriaFalsoTelegram() : base(null!, null!, null!)
    {
    }

    public List<(
        AccionAuditoria Accion,
        string Entidad,
        int? EntidadId,
        string Detalle)> Registros { get; } = [];

    public override Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
    {
        Registros.Add((accion, entidad, entidadId, detalle));
        return Task.CompletedTask;
    }
}

public sealed class GeminiFalsoTelegram : IClienteGemini
{
    public Task<SolicitudHerramienta?> ElegirHerramientaAsync(
        string mensaje,
        CancellationToken cancelacion = default)
        => throw new InvalidOperationException(
            "Gemini no debe intervenir en estas pruebas.");
}

public sealed class UsuarioRepositorioFalsoTelegram(
    Dictionary<string, Usuario> usuarios) : IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Task.FromResult(usuarios.GetValueOrDefault(nombreUsuario));

    public Task<Usuario?> ObtenerPorIdAsync(int id)
        => Task.FromResult(usuarios.Values
            .FirstOrDefault(usuario => usuario.Id == id));

    public Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
        => Task.FromResult<IReadOnlyList<Usuario>>(usuarios.Values.ToList());

    public Task AgregarAsync(Usuario entidad) => Task.CompletedTask;

    public void Actualizar(Usuario entidad)
    {
    }

    public int Guardados { get; private set; }

    public Task<int> GuardarAsync()
    {
        Guardados++;
        return Task.FromResult(0);
    }
}

public sealed class VentaRepositorioFalsoTelegram : IVentaRepositorio
{
    public int Guardados { get; private set; }

    public Task<int> GuardarAsync()
    {
        Guardados++;
        return Task.FromResult(0);
    }
    public Task<Venta?> ObtenerPorIdAsync(int id)
        => Task.FromResult<Venta?>(null);

    public Task<IReadOnlyList<Venta>> ObtenerTodosAsync()
        => Task.FromResult<IReadOnlyList<Venta>>(Array.Empty<Venta>());

    public Task AgregarAsync(Venta entidad) => Task.CompletedTask;

    public void Actualizar(Venta entidad)
    {
    }

    public Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId)
        => Task.FromResult<IReadOnlyList<Venta>>(Array.Empty<Venta>());

    public Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => Task.FromResult<IReadOnlyList<Venta>>(Array.Empty<Venta>());

    public Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia)
        => Task.FromResult<Venta?>(null);
}

public sealed class AlertasFalsasTelegram : ServicioInventario
{
    public AlertasFalsasTelegram() : base(null!, null!, null!, null!, null!, null!)
    {
    }

    public override Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync()
        => Task.FromResult<IReadOnlyList<AlertaStockMinimo>>(
            Array.Empty<AlertaStockMinimo>());

    public override Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
        => Task.FromResult<IReadOnlyList<Lote>>(Array.Empty<Lote>());
}

public sealed class CajaFalsaTelegram : ServicioCaja
{
    public CajaFalsaTelegram() : base(null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public override Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial)
        => throw new NotImplementedException();

    public override Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado, int? cajaId = null)
        => throw new NotImplementedException();

    public override Task<TurnoCaja?> ObtenerTurnoActualAsync(int? cajaId = null)
        => Task.FromResult<TurnoCaja?>(null);

    public override Task<RetiroCaja> RegistrarRetiroAsync(
        long monto,
        string motivo,
        int supervisorId,
        int? cajaId = null)
        => throw new NotImplementedException();

    public override Task<IReadOnlyList<RetiroCaja>> ObtenerRetirosTurnoActualAsync(int? cajaId = null)
        => Task.FromResult<IReadOnlyList<RetiroCaja>>(
            Array.Empty<RetiroCaja>());
}

public sealed class VentasFalsasTelegram : ServicioVentas
{
    public VentasFalsasTelegram() : base(null!, null!, null!, null!, null!, null!, null!, null!, null!)
    {
    }

    public override Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud)
        => throw new NotImplementedException();

    public override Task AnularVentaAsync(int ventaId, string motivo)
        => Task.CompletedTask;

    public override Task AnularVentaAsync(int ventaId, string motivo, string? origen)
        => Task.CompletedTask;
}

public sealed class ReabastecimientoFalsoTelegram : ServicioInventario
{
    public ReabastecimientoFalsoTelegram()
        : base(null!, null!, null!, null!, null!, null!)
    {
    }

    public override Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync(
        string? origen = null)
        => Task.FromResult<IReadOnlyList<Reabastecimiento>>(
            Array.Empty<Reabastecimiento>());

    public override Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
        => Task.FromResult<IReadOnlyList<Reabastecimiento>>(
            Array.Empty<Reabastecimiento>());

    public override Task AtenderAsync(int reabastecimientoId) => Task.CompletedTask;
}
