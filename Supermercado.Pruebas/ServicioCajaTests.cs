using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioCajaTests
{
    private class Escenario
    {
        public TurnosEnMemoria Turnos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public RepositorioEnMemoria<Caja> Cajas { get; } = new();
        public ParametrosFalsos Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public ServicioCaja Servicio { get; }

        public Escenario(bool activa = true, long tolerancia = 1000)
        {
            Cajas.Datos.Add(new Caja { Id = 1, Nombre = "Caja 1", Activa = activa });
            Parametros.Valores[ClavesParametro.ToleranciaArqueo]
                = tolerancia.ToString();

            Servicio = new ServicioCaja(
                Turnos,
                Ventas,
                Cajas,
                Parametros,
                Auditoria,
                Sesiones.Con(Permiso.AbrirCerrarTurno),
                Trabajo,
                new RelojFijo(EscenarioInventario.Ahora));
        }
    }

    [Fact]
    public async Task AbrirTurno_ExigeUnaCajaActiva()
    {
        var inactiva = new Escenario(activa: false);

        await Assert.ThrowsAsync<ValidacionException>(
            () => inactiva.Servicio.AbrirTurnoAsync(1, 50000));
    }

    [Fact]
    public async Task AbrirTurno_ExigeQueLaCajaExista()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.Servicio.AbrirTurnoAsync(99, 50000));
    }

    [Fact]
    public async Task Cierre_ConDiferenciaDentroDeTolerancia()
    {
        var e = new Escenario(tolerancia: 1000);
        await e.Servicio.AbrirTurnoAsync(1, 50000);

        var resultado = await e.Servicio.CerrarTurnoAsync(50500);

        Assert.Equal(50000L, resultado.Esperado);
        Assert.Equal(500L, resultado.Diferencia);
        Assert.True(resultado.DentroDeTolerancia);
    }

    [Fact]
    public async Task Cierre_FueraDeTolerancia_SeMarca()
    {
        var e = new Escenario(tolerancia: 1000);
        await e.Servicio.AbrirTurnoAsync(1, 50000);

        var resultado = await e.Servicio.CerrarTurnoAsync(48000);

        Assert.False(resultado.DentroDeTolerancia);
    }
}
