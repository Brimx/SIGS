using Microsoft.Extensions.Logging;
using Supermercado.Datos;

namespace Supermercado.Logica.Servicios;

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
