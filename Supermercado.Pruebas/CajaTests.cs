using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Xunit;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class CajaTests
{
    private static readonly DateTime Ahora
        = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    private static Venta VentaEnEfectivo(long recibido)
    {
        var arroz = new ProductoUnitario
        {
            Id = 1,
            PrecioVenta = 3000,
            Impuesto = new Iva()
        };
        var venta = new Venta();
        venta.AgregarProducto(arroz, 10);
        venta.AgregarPago(new PagoEfectivo { Monto = recibido });
        venta.Cerrar();
        return venta;
    }

    [Fact]
    public void Abrir_RechazaUnEfectivoInicialNegativo()
    {
        Assert.Throws<TurnoInvalidoException>(
            () => TurnoCaja.Abrir(1, 1, -1, Ahora));
    }

    [Fact]
    public void Esperado_SumaElEfectivoNetoYDescuentaLasAnuladas()
    {
        var turno = TurnoCaja.Abrir(1, 1, 50000, Ahora);
        var buena = VentaEnEfectivo(35000);
        var anulada = VentaEnEfectivo(30000);
        anulada.Anular("error del cajero");

        var esperado = turno.CalcularEfectivoEsperado([buena, anulada]);

        Assert.Equal(50000L, esperado);
    }

    [Fact]
    public void Cerrar_GuardaLaDiferenciaDelArqueo()
    {
        var turno = TurnoCaja.Abrir(1, 1, 50000, Ahora);

        turno.Cerrar(Ahora, 80000, 79000);

        Assert.Equal(-1000L, turno.Diferencia);
        Assert.Equal(EstadoTurno.Cerrado, turno.Estado);
    }

    [Fact]
    public void Cerrar_NoSePuedeDosVeces()
    {
        var turno = TurnoCaja.Abrir(1, 1, 0, Ahora);
        turno.Cerrar(Ahora, 0, 0);

        Assert.Throws<TurnoInvalidoException>(
            () => turno.Cerrar(Ahora, 0, 0));
    }

    [Fact]
    public void Esperado_SinMovimientosNiListas_UsaValoresNeutros()
    {
        var turno = TurnoCaja.Abrir(1, 1, 50000, Ahora);
        turno.Retiros = null!;

        Assert.Equal(50000L, turno.CalcularEfectivoEsperado(null));
    }
}

public class ServicioCajaTests
{
    private class Escenario
    {
        public TurnosEnMemoria Turnos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public CajasEnMemoria Cajas { get; } = new();
        public ParametrosFalsos Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public ServicioCaja Servicio { get; }

        public Escenario(bool activa = true, long tolerancia = 1000)
        {
            Cajas.Datos.Add(
                new Caja { Id = 1, Nombre = "Caja 1", Activa = activa });
            Parametros.Valores[ClavesParametro.ToleranciaArqueo]
                = tolerancia.ToString();

            Servicio = new ServicioCaja(
                Turnos,
                Ventas,
                Cajas,
                Parametros,
                Auditoria,
                Sesiones.Con(Permiso.AbrirCerrarTurno),
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

    [Fact]
    public async Task AbrirTurno_SinPermiso_LanzaAccesoDenegado()
    {
        var e = new Escenario();
        var sinPermiso = new ServicioCaja(
            e.Turnos,
            e.Ventas,
            e.Cajas,
            e.Parametros,
            e.Auditoria,
            Sesiones.Con(Permiso.Vender),
            new RelojFijo(EscenarioInventario.Ahora));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => sinPermiso.AbrirTurnoAsync(1, 50000));
    }

    [Fact]
    public async Task AbrirTurno_YaAbiertoPorMismoEmpleado_LanzaError()
    {
        var e = new Escenario();
        await e.Servicio.AbrirTurnoAsync(1, 50000);

        await Assert.ThrowsAsync<TurnoInvalidoException>(
            () => e.Servicio.AbrirTurnoAsync(1, 50000));
    }

    [Fact]
    public async Task CerrarTurno_SinTurnoAbierto_LanzaTurnoInvalidoException()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<TurnoInvalidoException>(
            () => e.Servicio.CerrarTurnoAsync(50000));
    }

    [Fact]
    public async Task TurnoActual_PorCaja_DetectaTurnoDeOtroEmpleado()
    {
        var e = new Escenario();
        e.Turnos.Datos.Add(TurnoCaja.Abrir(1, 2, 50000, EscenarioInventario.Ahora.UtcDateTime));

        var turno = await e.Servicio.ObtenerTurnoActualAsync(1);

        Assert.NotNull(turno);
        Assert.Equal(2, turno.EmpleadoId);

        var ex = await Assert.ThrowsAsync<TurnoInvalidoException>(
            () => e.Servicio.AbrirTurnoAsync(1, 50000));
        Assert.Equal("La caja ya tiene un turno abierto.", ex.Message);
    }

    [Fact]
    public async Task CerrarTurnoSinMovimientos_PorCaja_CalculaLaBase()
    {
        var e = new Escenario();
        e.Turnos.Datos.Add(TurnoCaja.Abrir(1, 2, 50000, EscenarioInventario.Ahora.UtcDateTime));

        var resultado = await e.Servicio.CerrarTurnoAsync(50000, 1);

        Assert.Equal(50000L, resultado.Esperado);
        Assert.Equal(0L, resultado.Diferencia);
        Assert.True(resultado.DentroDeTolerancia);
    }
}
