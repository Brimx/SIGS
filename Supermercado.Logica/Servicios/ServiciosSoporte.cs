using Microsoft.Extensions.Logging;
using Supermercado.Datos;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

// ==========================================
// Respaldo de Base de Datos
// ==========================================

public interface IServicioRespaldo
{
    Task<string> CrearRespaldoAsync(string? rutaOrigen = null);
}

public class ServicioRespaldo(
    IServicioParametros parametros,
    ISesionActual sesion,
    IServicioAuditoria auditoria,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioRespaldo
{
    public async Task<string> CrearRespaldoAsync(string? rutaOrigen = null)
    {
        sesion.Exigir(Permiso.GestionarRoles);

        var origen = string.IsNullOrWhiteSpace(rutaOrigen)
            ? RutasDatos.BaseDatosPorDefecto()
            : rutaOrigen;

        if (!File.Exists(origen))
            throw new ValidacionException(
                "No se encontró el archivo de base de datos a respaldar.");

        var carpetaDestino = await parametros.ObtenerTextoAsync(
            ClavesParametro.CarpetaRespaldos);

        if (string.IsNullOrWhiteSpace(carpetaDestino))
            throw new ParametroFaltanteException(
                ClavesParametro.CarpetaRespaldos);

        Directory.CreateDirectory(carpetaDestino);

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var nombreArchivo = $"sigs-respaldo-{ahora:yyyyMMdd-HHmmss}.db";
        var rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);

        File.Copy(origen, rutaDestino, overwrite: true);

        await auditoria.RegistrarAsync(
            AccionAuditoria.RespaldoBaseDatos,
            "BaseDatos",
            null,
            $"Respaldo creado en {rutaDestino}");
        await trabajo.GuardarAsync();

        return rutaDestino;
    }
}

// ==========================================
// Logging a Archivo
// ==========================================

public sealed class ProveedorLogArchivo(
    string? rutaArchivo = null,
    TimeProvider? reloj = null) : ILoggerProvider
{
    private readonly string ruta = string.IsNullOrWhiteSpace(rutaArchivo)
        ? RutasDatos.ArchivoLogPorDefecto()
        : rutaArchivo;
    private readonly TimeProvider proveedorReloj = reloj ?? TimeProvider.System;
    private readonly object candado = new();

    public ILogger CreateLogger(string categoryName)
        => new LoggerArchivo(categoryName, ruta, proveedorReloj, candado);

    public void Dispose()
    {
    }
}

public sealed class LoggerArchivo(
    string categoria,
    string rutaArchivo,
    TimeProvider reloj,
    object candado) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Warning;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var mensaje = formatter(state, exception);
        var linea = $"[{ahora:yyyy-MM-dd HH:mm:ss} UTC] [{logLevel}] "
            + $"[{categoria}] {mensaje}";

        if (exception is not null)
            linea += $"{Environment.NewLine}{exception}";

        linea += Environment.NewLine;

        lock (candado)
        {
            var carpeta = Path.GetDirectoryName(rutaArchivo);
            if (!string.IsNullOrEmpty(carpeta))
                Directory.CreateDirectory(carpeta);

            File.AppendAllText(rutaArchivo, linea);
        }
    }
}
