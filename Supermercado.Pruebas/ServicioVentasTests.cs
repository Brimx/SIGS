using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioVentasTests
{
    private class Escenario
    {
        public EscenarioInventario Inventario { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public StockEstante Fila { get; }
        public ServicioVentas Servicio { get; }

        public Escenario(Permiso permisos, bool conTurno = true)
        {
            Productos.Datos.Add(new ProductoUnitario
            {
                Id = 1,
                CodigoBarras = "123",
                PrecioVenta = 11900,
                Impuesto = new Iva { PuntosBasicos = 1900 }
            });
            Fila = Inventario.AgregarEstante(1, 10, 20, 10);

            if (conTurno)
                Turnos.Datos.Add(TurnoCaja.Abrir(
                    1, 1, 0, EscenarioInventario.Ahora.UtcDateTime));

            Servicio = new ServicioVentas(
                Productos,
                Ventas,
                Turnos,
                Inventario.Servicio,
                Auditoria,
                Sesiones.Con(permisos),
                Trabajo,
                new RelojFijo(EscenarioInventario.Ahora));
        }
    }

    private static SolicitudVenta Solicitud(int cantidad, long efectivo)
        => new(
            [new LineaVenta("123", cantidad)],
            [new PagoEfectivo { Monto = efectivo }]);

    [Fact]
    public async Task VentaCompleta_DescuentaStockYGuardaUnaSolaVez()
    {
        var e = new Escenario(Permiso.Vender);

        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        Assert.Equal(23800L, venta.Total);
        Assert.Equal(3800L, venta.TotalIva);
        Assert.Equal(6200L, venta.Cambio);
        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(8, e.Fila.Cantidad);
        Assert.Equal(1, e.Trabajo.Guardados);
    }

    [Fact]
    public async Task SinStockSuficiente_NoGuardaNada()
    {
        var e = new Escenario(Permiso.Vender);

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(11, 200000)));

        Assert.Equal(0, e.Trabajo.Guardados);
    }

    [Fact]
    public async Task SinPermisoDeVender_SeNiega()
    {
        var e = new Escenario(Permiso.VerInventario);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000)));
    }

    [Fact]
    public async Task SinTurnoAbierto_NoSeVende()
    {
        var e = new Escenario(Permiso.Vender, conTurno: false);

        await Assert.ThrowsAsync<TurnoInvalidoException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000)));
    }

    [Fact]
    public async Task CodigoDesconocido_SeRechaza()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("999", 1)],
            [new PagoEfectivo { Monto = 20000 }]);

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(solicitud));
    }

    [Fact]
    public async Task Anulacion_DevuelveElStockYQuedaAuditada()
    {
        var e = new Escenario(Permiso.Vender | Permiso.AnularVenta);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        await e.Servicio.AnularVentaAsync(venta.Id, "cliente se arrepintió");

        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
        Assert.Equal(10, e.Fila.Cantidad);
        var accion = e.Auditoria.Acciones.Single();
        Assert.Equal(AccionAuditoria.VentaAnulada, accion);
    }

    [Fact]
    public async Task Anulacion_ExigeElPermiso()
    {
        var e = new Escenario(Permiso.Vender);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.AnularVentaAsync(venta.Id, "motivo"));
    }

    [Fact]
    public async Task PagoConTarjeta_CompletaLaVenta()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoTarjeta { Monto = 11900, UltimosDigitos = "1234" }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(0L, venta.Cambio);
    }

    [Fact]
    public async Task PagoPorTransferencia_CompletaLaVenta()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoTransferencia { Monto = 11900, Referencia = "TRX-1" }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(0L, venta.Cambio);
    }

    [Fact]
    public async Task AnularDosVeces_SoloDevuelveElStockUnaVez()
    {
        var e = new Escenario(Permiso.Vender | Permiso.AnularVenta);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        await e.Servicio.AnularVentaAsync(venta.Id, "motivo");
        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.AnularVentaAsync(venta.Id, "otra vez"));

        Assert.Equal(10, e.Fila.Cantidad);
        Assert.Single(
            e.Inventario.Movimientos.Datos,
            m => m.Tipo == TipoMovimiento.Devolucion);
    }
}
