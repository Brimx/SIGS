using Microsoft.Extensions.Logging;

namespace Supermercado.Logica.Servicios;

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
