using Supermercado.Datos;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

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
