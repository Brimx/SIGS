using System.Security.Cryptography;
using System.Text.Json;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Servicios;

public sealed record VinculoTelegram(
    long ChatId,
    long UsuarioTelegramId,
    int EmpleadoId,
    string NombreUsuario);

public sealed record AprobacionTelegram(
    string Token,
    HerramientaAsistente Herramienta,
    int? Id,
    string? Motivo,
    long ChatDestino,
    long UsuarioDestino,
    DateTimeOffset Vence);

public interface IVinculosTelegram
{
    string GenerarCodigo(int empleadoId, string nombreUsuario);
    bool Vincular(string codigo, long chatId, long usuarioTelegramId);
    void Desvincular(long chatId);
    VinculoTelegram? Buscar(long chatId);
    IReadOnlyList<VinculoTelegram> ObtenerTodos();
    bool PermitirMensaje(long chatId, DateTimeOffset ahora, int maximo = 20);
    AprobacionTelegram CrearAprobacion(
        HerramientaAsistente herramienta,
        int? id,
        string? motivo,
        long chatDestino,
        long usuarioDestino,
        DateTimeOffset vence);
    bool TomarAprobacion(
        string token,
        long chatId,
        long usuarioTelegramId,
        DateTimeOffset ahora,
        out AprobacionTelegram? aprobacion);
}

public sealed class VinculosTelegram : IVinculosTelegram
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly object _candado = new();
    private readonly TimeProvider _reloj;
    private readonly string _ruta;
    private readonly Dictionary<string, CodigoTemporal> _codigos = new(StringComparer.Ordinal);
    private readonly Dictionary<long, VinculoTelegram> _vinculos = [];
    private readonly Dictionary<long, Queue<DateTimeOffset>> _mensajes = [];
    private readonly Dictionary<string, AprobacionTelegram> _aprobaciones = new(StringComparer.Ordinal);

    public VinculosTelegram(TimeProvider reloj, string? ruta = null)
    {
        _reloj = reloj;
        _ruta = ruta ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIGS",
            "telegram-vinculos.json");
        Cargar();
    }

    public string GenerarCodigo(int empleadoId, string nombreUsuario)
    {
        if (empleadoId <= 0 || string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El usuario para vincular no es válido.");

        lock (_candado)
        {
            LimpiarExpirados();
            string codigo;
            do
            {
                codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            }
            while (_codigos.ContainsKey(codigo));

            _codigos[codigo] = new CodigoTemporal(
                empleadoId,
                nombreUsuario,
                _reloj.GetUtcNow().AddMinutes(10));
            return codigo;
        }
    }

    public bool Vincular(string codigo, long chatId, long usuarioTelegramId)
    {
        lock (_candado)
        {
            LimpiarExpirados();
            if (!_codigos.Remove(codigo, out var temporal)
                || chatId == 0
                || usuarioTelegramId == 0
                || _vinculos.ContainsKey(chatId))
                return false;

            _vinculos.Add(chatId, new VinculoTelegram(
                chatId,
                usuarioTelegramId,
                temporal.EmpleadoId,
                temporal.NombreUsuario));
            Guardar();
            return true;
        }
    }

    public VinculoTelegram? Buscar(long chatId)
    {
        lock (_candado)
            return _vinculos.GetValueOrDefault(chatId);
    }

    public void Desvincular(long chatId)
    {
        lock (_candado)
        {
            if (_vinculos.Remove(chatId))
                Guardar();
        }
    }

    public IReadOnlyList<VinculoTelegram> ObtenerTodos()
    {
        lock (_candado)
            return _vinculos.Values.ToArray();
    }

    public bool PermitirMensaje(long chatId, DateTimeOffset ahora, int maximo = 20)
    {
        lock (_candado)
        {
            if (!_mensajes.TryGetValue(chatId, out var cola))
                _mensajes[chatId] = cola = new Queue<DateTimeOffset>();

            while (cola.TryPeek(out var anterior) && ahora - anterior >= TimeSpan.FromMinutes(1))
                cola.Dequeue();

            if (cola.Count >= maximo)
                return false;

            cola.Enqueue(ahora);
            return true;
        }
    }

    public AprobacionTelegram CrearAprobacion(
        HerramientaAsistente herramienta,
        int? id,
        string? motivo,
        long chatDestino,
        long usuarioDestino,
        DateTimeOffset vence)
    {
        lock (_candado)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var aprobacion = new AprobacionTelegram(
                token, herramienta, id, motivo, chatDestino, usuarioDestino, vence);
            _aprobaciones.Add(token, aprobacion);
            return aprobacion;
        }
    }

    public bool TomarAprobacion(
        string token,
        long chatId,
        long usuarioTelegramId,
        DateTimeOffset ahora,
        out AprobacionTelegram? aprobacion)
    {
        lock (_candado)
        {
            aprobacion = null;
            if (!_aprobaciones.TryGetValue(token, out var guardada)
                || guardada.ChatDestino != chatId
                || guardada.UsuarioDestino != usuarioTelegramId)
                return false;

            _aprobaciones.Remove(token);
            if (guardada.Vence <= ahora)
                return false;

            aprobacion = guardada;
            return true;
        }
    }

    private void LimpiarExpirados()
    {
        var ahora = _reloj.GetUtcNow();
        foreach (var codigo in _codigos
            .Where(par => par.Value.Vence <= ahora)
            .Select(par => par.Key)
            .ToArray())
            _codigos.Remove(codigo);

        foreach (var token in _aprobaciones
            .Where(par => par.Value.Vence <= ahora)
            .Select(par => par.Key)
            .ToArray())
            _aprobaciones.Remove(token);
    }

    private void Cargar()
    {
        if (!File.Exists(_ruta))
            return;

        try
        {
            var vinculados = JsonSerializer.Deserialize<VinculoTelegram[]>(
                File.ReadAllText(_ruta), Json) ?? [];
            foreach (var vinculo in vinculados)
                _vinculos[vinculo.ChatId] = vinculo;
        }
        catch (JsonException)
        {
            // El archivo local puede reconstruirse volviendo a vincular cada chat.
        }
    }

    private void Guardar()
    {
        var carpeta = Path.GetDirectoryName(_ruta);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);
        var temporal = _ruta + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(_vinculos.Values, Json));
        File.Move(temporal, _ruta, overwrite: true);
    }

    private sealed record CodigoTemporal(
        int EmpleadoId,
        string NombreUsuario,
        DateTimeOffset Vence);
}
