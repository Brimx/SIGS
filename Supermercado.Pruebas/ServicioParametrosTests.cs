using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioParametrosTests
{
    private class Escenario
    {
        public ParametrosEnMemoria Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public ServicioParametros Servicio { get; }

        public Escenario(Permiso permisos = Permiso.GestionarPersonal)
            => Servicio = new ServicioParametros(
                Parametros,
                Auditoria,
                Sesiones.Con(permisos),
                Trabajo);
    }

    [Fact]
    public async Task Guardar_ConPermiso_AuditaYGuarda()
    {
        var e = new Escenario();

        await e.Servicio.GuardarAsync("clave.prueba", "42");

        Assert.Equal("42", await e.Servicio.ObtenerTextoAsync("clave.prueba"));
        Assert.Equal(1, e.Trabajo.Guardados);
        Assert.Equal(
            AccionAuditoria.ParametroCambiado,
            Assert.Single(e.Auditoria.Acciones));
    }

    [Fact]
    public async Task Guardar_SinPermiso_SeNiega()
    {
        var e = new Escenario(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.GuardarAsync("clave.prueba", "42"));

        Assert.Empty(e.Parametros.Datos);
        Assert.Equal(0, e.Trabajo.Guardados);
    }

    [Fact]
    public async Task GuardarRespaldo_NoExigePermisoNiAudita()
    {
        var servicio = new ServicioParametros(
            new ParametrosEnMemoria(),
            new AuditoriaFalsa(),
            new SesionActual(),
            new UnidadDeTrabajoFalsa());

        await servicio.GuardarRespaldoAsync(new Dictionary<string, string>
        {
            [ClavesParametro.TrmUltimaCentavos] = "401234",
            [ClavesParametro.TrmUltimaFecha] = "2026-10-17"
        });

        Assert.Equal(
            "401234",
            await servicio.ObtenerTextoAsync(ClavesParametro.TrmUltimaCentavos));
        Assert.Equal(
            "2026-10-17",
            await servicio.ObtenerTextoAsync(ClavesParametro.TrmUltimaFecha));
    }

    [Fact]
    public async Task ParametroFaltante_LanzaLaExcepcionDelCatalogo()
    {
        var parametros = new ParametrosFalsos();

        await Assert.ThrowsAsync<ParametroFaltanteException>(
            () => parametros.ObtenerTextoAsync("no.existe"));
    }
}
