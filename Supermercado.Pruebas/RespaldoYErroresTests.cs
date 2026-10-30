using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Supermercado.Datos;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class RespaldoYErroresTests
{
    [Fact]
    public void FabricaSigsDbContext_IncluyeMigracionInicial()
    {
        var tempDb = Path.Combine(
            Path.GetTempPath(), $"sigs-test-{Guid.NewGuid():N}.db");
        try
        {
            var fabrica = new FabricaSigsDbContext();
            using var db = fabrica.CreateDbContext([tempDb]);
            var migraciones = db.Database.GetMigrations().ToList();

            Assert.Contains("20260420000000_Inicial", migraciones);
        }
        finally
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    [Fact]
    public async Task ServicioRespaldo_CopiaArchivoYRegistraAuditoria()
    {
        var dirTemp = Path.Combine(
            Path.GetTempPath(), $"sigs-bk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirTemp);

        var origen = Path.Combine(dirTemp, "sigs.db");
        var carpetaDestino = Path.Combine(dirTemp, "backups");
        await File.WriteAllBytesAsync(origen, [1, 2, 3, 4, 5]);

        try
        {
            var parametros = new ParametrosFalsos();
            parametros.Valores[ClavesParametro.CarpetaRespaldos] =
                carpetaDestino;
            var sesion = Sesiones.Con(Permiso.GestionarRoles);
            var auditoria = new AuditoriaFalsa();
            var reloj = new RelojFijo(
                new DateTime(2026, 4, 20, 14, 30, 0, DateTimeKind.Utc));
            var uow = new UnidadDeTrabajoFalsa();

            var servicio = new ServicioRespaldo(
                parametros, sesion, auditoria, uow, reloj);

            var rutaCopia = await servicio.CrearRespaldoAsync(origen);

            Assert.True(File.Exists(rutaCopia));
            Assert.Equal(
                [1, 2, 3, 4, 5], await File.ReadAllBytesAsync(rutaCopia));
            Assert.Contains(
                AccionAuditoria.RespaldoBaseDatos,
                auditoria.Acciones);
            Assert.Equal(1, uow.Guardados);
        }
        finally
        {
            if (Directory.Exists(dirTemp))
                Directory.Delete(dirTemp, recursive: true);
        }
    }

    [Fact]
    public async Task ServicioRespaldo_SinPermisoOSinArchivo_Falla()
    {
        var parametros = new ParametrosFalsos();
        var sesionSinPermiso = Sesiones.Con(Permiso.Vender);
        var auditoria = new AuditoriaFalsa();
        var reloj = new RelojFijo(DateTime.UtcNow);
        var uow = new UnidadDeTrabajoFalsa();

        var servicioSinPermiso = new ServicioRespaldo(
            parametros, sesionSinPermiso, auditoria, uow, reloj);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicioSinPermiso.CrearRespaldoAsync("/no/existe.db"));

        var sesionAdmin = Sesiones.Con(Permiso.GestionarRoles);
        var servicioAdmin = new ServicioRespaldo(
            parametros, sesionAdmin, auditoria, uow, reloj);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicioAdmin.CrearRespaldoAsync("/no/existe/sigs.db"));
    }

    [Fact]
    public void TraductorExcepciones_DistingueDominioDeInesperadasYLlevaLog()
    {
        var archivoLog = Path.Combine(
            Path.GetTempPath(), $"sigs-err-{Guid.NewGuid():N}.log");
        try
        {
            var reloj = new RelojFijo(
                new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc));
            using var proveedor = new ProveedorLogArchivo(archivoLog, reloj);
            using var factory = LoggerFactory.Create(
                b => b.AddProvider(proveedor));
            var logger = factory.CreateLogger<TraductorExcepciones>();
            var traductor = new TraductorExcepciones(logger);

            var msgDominio = traductor.Traducir(
                new ValidacionException("El precio debe ser positivo."));
            Assert.Equal("El precio debe ser positivo.", msgDominio);
            Assert.False(File.Exists(archivoLog));

            var msgInesperado = traductor.Traducir(
                new InvalidOperationException("Fallo interno SQL."));
            Assert.Contains("error inesperado", msgInesperado);
            Assert.DoesNotContain("SQL", msgInesperado);

            Assert.True(File.Exists(archivoLog));
            var contenido = File.ReadAllText(archivoLog);
            Assert.Contains("Fallo interno SQL.", contenido);
            Assert.Contains("[Error]", contenido);
        }
        finally
        {
            if (File.Exists(archivoLog))
                File.Delete(archivoLog);
        }
    }
}
