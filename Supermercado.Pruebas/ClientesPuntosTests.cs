using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Clientes;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ClientesPuntosTests
{
    private class Escenario
    {
        public EscenarioInventario Inventario { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public ClientesEnMemoria Clientes { get; } = new();
        public ParametrosFalsos Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public Cliente Cliente { get; }
        public ServicioVentas Servicio { get; }
        public ServicioClientes ServicioClientes { get; }

        public Escenario(Permiso permisos = Permiso.Vender
            | Permiso.AnularVenta | Permiso.GestionarClientes)
        {
            Productos.Datos.Add(new ProductoUnitario
            {
                Id = 1,
                CodigoBarras = "123",
                PrecioVenta = 10000,
                Impuesto = new Iva { PuntosBasicos = 0 }
            });
            Inventario.AgregarEstante(1, 50, 20, 10);
            Turnos.Datos.Add(TurnoCaja.Abrir(
                1, 1, 0, EscenarioInventario.Ahora.UtcDateTime));
            Parametros.Valores[ClavesParametro.PesosPorPunto] = "1000";
            Parametros.Valores[ClavesParametro.ValorPuntoPesos] = "10";

            Cliente = new Cliente
            {
                Id = 7,
                Documento = "1020304050",
                Nombres = "Lucía",
                PuntosAcumulados = 100
            };
            Clientes.Datos.Add(Cliente);

            var sesion = Sesiones.Con(permisos);
            Servicio = new ServicioVentas(
                Productos, Ventas, Turnos, Inventario.Servicio, Clientes,
                Parametros, Auditoria, sesion, Trabajo,
                new RelojFijo(EscenarioInventario.Ahora));
            ServicioClientes = new ServicioClientes(
                Clientes, Auditoria, sesion, Trabajo);
        }

        public SolicitudVenta Solicitud(
            int cantidad, long efectivo, int? clienteId = 7, int puntos = 0)
            => new(
                [new LineaVenta("123", cantidad)],
                [new PagoEfectivo { Monto = efectivo }],
                clienteId,
                puntos);
    }

    [Fact]
    public async Task Venta_ConCliente_AcumulaUnPuntoPorCadaMilPesos()
    {
        var e = new Escenario();

        var venta = await e.Servicio.RegistrarVentaAsync(e.Solicitud(3, 30000));

        Assert.Equal(30, venta.PuntosGanados);
        Assert.Equal(130, e.Cliente.PuntosAcumulados);
        Assert.Equal(7, venta.ClienteId);
    }

    [Fact]
    public async Task Venta_SinCliente_NoTocaPuntos()
    {
        var e = new Escenario();

        var venta = await e.Servicio.RegistrarVentaAsync(
            e.Solicitud(3, 30000, clienteId: null));

        Assert.Equal(0, venta.PuntosGanados);
        Assert.Null(venta.ClienteId);
        Assert.Equal(100, e.Cliente.PuntosAcumulados);
    }

    [Fact]
    public async Task Redencion_PagaParteDeLaVentaYNoGanaSobreEsaParte()
    {
        var e = new Escenario();

        // 50 puntos × $10 = $500 de $30.000; el resto en efectivo.
        var venta = await e.Servicio.RegistrarVentaAsync(
            e.Solicitud(3, 29500, puntos: 50));

        Assert.Contains(venta.Pagos, p => p is PagoPuntos { Puntos: 50, Monto: 500 });
        Assert.Equal(50, venta.PuntosRedimidos);
        Assert.Equal(29, venta.PuntosGanados);
        Assert.Equal(100 - 50 + 29, e.Cliente.PuntosAcumulados);
    }

    [Fact]
    public async Task Redencion_MayorAlSaldo_SeRechazaSinTocarNada()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(
                e.Solicitud(3, 0, puntos: 101)));

        Assert.Equal(100, e.Cliente.PuntosAcumulados);
        Assert.Equal(0, e.Trabajo.Guardados);
    }

    [Fact]
    public async Task Redencion_QueSuperaElTotal_SeRechaza()
    {
        var e = new Escenario();
        e.Cliente.PuntosAcumulados = 5000;

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(
                e.Solicitud(1, 0, puntos: 2000)));
    }

    [Fact]
    public async Task Redencion_SinCliente_SeRechaza()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(
                e.Solicitud(1, 10000, clienteId: null, puntos: 10)));
    }

    [Fact]
    public async Task VentaRechazada_NoDejaPuntosModificadosEnMemoria()
    {
        var e = new Escenario();

        // Efectivo insuficiente: Cerrar() falla después de preparar puntos.
        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(
                e.Solicitud(3, 1000, puntos: 50)));

        Assert.Equal(100, e.Cliente.PuntosAcumulados);
    }

    [Fact]
    public async Task ClienteInactivo_NoPuedeComprarConPuntos()
    {
        var e = new Escenario();
        e.Cliente.Activo = false;

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(e.Solicitud(1, 10000)));
    }

    [Fact]
    public async Task AcumulacionDesactivada_ConPesosPorPuntoEnCero()
    {
        var e = new Escenario();
        e.Parametros.Valores[ClavesParametro.PesosPorPunto] = "0";

        var venta = await e.Servicio.RegistrarVentaAsync(e.Solicitud(3, 30000));

        Assert.Equal(0, venta.PuntosGanados);
        Assert.Equal(100, e.Cliente.PuntosAcumulados);
    }

    [Fact]
    public async Task Anulacion_QuitaLoGanadoYDevuelveLoRedimido()
    {
        var e = new Escenario();
        var venta = await e.Servicio.RegistrarVentaAsync(
            e.Solicitud(3, 29500, puntos: 50));

        await e.Servicio.AnularVentaAsync(venta.Id, "error de caja");

        Assert.Equal(100, e.Cliente.PuntosAcumulados);
    }

    [Fact]
    public void RevertirVenta_NuncaDejaElSaldoNegativo()
    {
        var cliente = new Cliente { PuntosAcumulados = 5 };

        cliente.RevertirVenta(ganados: 30, redimidos: 0);

        Assert.Equal(0, cliente.PuntosAcumulados);
    }

    [Fact]
    public async Task Crear_ValidaYAudita()
    {
        var e = new Escenario();

        var c = await e.ServicioClientes.CrearAsync(
            "5566778", "Pedro", "Gómez", "300 111 2233", "pedro@correo.com");

        Assert.Equal("3001112233", c.Telefono);
        Assert.Contains(AccionAuditoria.ClienteCreado, e.Auditoria.Acciones);
    }

    [Theory]
    [InlineData("5566778", "Pedro3", "")]
    [InlineData("55A6778", "Pedro", "")]
    [InlineData("5566778", "Pedro", "no-es-correo")]
    public async Task Crear_ConDatosInvalidos_LanzaValidacion(
        string documento, string nombres, string correo)
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.ServicioClientes.CrearAsync(
                documento, nombres, "", "", correo));
        Assert.Equal(0, e.Trabajo.Guardados);
    }

    [Fact]
    public async Task Crear_DocumentoDuplicado_LanzaValidacion()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.ServicioClientes.CrearAsync(
                "1020304050", "Otro", "", "", ""));
    }

    [Fact]
    public async Task Crear_SinPermiso_SeNiega()
    {
        var e = new Escenario(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.ServicioClientes.CrearAsync(
                "5566778", "Pedro", "", "", ""));
    }

    [Fact]
    public async Task BuscarPorDocumento_LoPuedeUsarElCajero()
    {
        var e = new Escenario(Permiso.Vender);

        var c = await e.ServicioClientes.BuscarPorDocumentoAsync("1020304050");

        Assert.Equal(7, c?.Id);
    }
}
