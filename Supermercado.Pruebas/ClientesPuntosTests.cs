using Supermercado.Entidades.Clientes;
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

            Cliente = new Cliente
            {
                Id = 7,
                Documento = "1020304050",
                Nombres = "Lucía"
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
            int cantidad, long efectivo, int? clienteId = 7)
            => new(
                [new LineaVenta("123", cantidad)],
                [new PagoEfectivo { Monto = efectivo }],
                clienteId);
    }

    [Fact]
    public async Task Venta_ConCliente_VinculaClienteId()
    {
        var e = new Escenario();

        var venta = await e.Servicio.RegistrarVentaAsync(e.Solicitud(3, 30000));

        Assert.Equal(7, venta.ClienteId);
    }

    [Fact]
    public async Task Venta_SinCliente_DejaClienteIdEnNulo()
    {
        var e = new Escenario();

        var venta = await e.Servicio.RegistrarVentaAsync(
            e.Solicitud(3, 30000, clienteId: null));

        Assert.Null(venta.ClienteId);
    }

    [Fact]
    public async Task ClienteInactivo_NoPuedeComprar()
    {
        var e = new Escenario();
        e.Cliente.Activo = false;

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(e.Solicitud(1, 10000)));
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
