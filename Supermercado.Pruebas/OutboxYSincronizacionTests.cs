using System.Net;
using System.Text.Json;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Sincronizacion;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class OutboxYSincronizacionTests
{
    private class Escenario
    {
        public EscenarioInventario Inventario { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public ClientesEnMemoria Clientes { get; } = new();
        public EventosOutboxEnMemoria Outbox { get; } = new();
        public ParametrosFalsos Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public ServicioVentas Servicio { get; }
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public Escenario()
        {
            Productos.Datos.Add(new ProductoUnitario
            {
                Id = 1,
                CodigoBarras = "123",
                PrecioVenta = 10000,
                Impuesto = new Iva { PuntosBasicos = 0 }
            });
            Inventario.AgregarEstante(1, 20, 20, 10);
            Turnos.Datos.Add(TurnoCaja.Abrir(
                1, 1, 0, EscenarioInventario.Ahora.UtcDateTime));
            Parametros.Valores[ClavesParametro.PesosPorPunto] = "1000";
            Parametros.Valores[ClavesParametro.ValorPuntoPesos] = "10";
            Parametros.Valores[ClavesParametro.SupabaseHabilitado] = "1";

            var sesion = Sesiones.Con(
                Permiso.Vender | Permiso.AnularVenta | Permiso.GestionarClientes);

            Servicio = new ServicioVentas(
                Productos,
                Ventas,
                Turnos,
                Inventario.Servicio,
                Clientes,
                Parametros,
                Auditoria,
                sesion,
                Trabajo,
                Reloj,
                Outbox);
        }
    }

    [Fact]
    public async Task RegistrarVenta_InsertaEventoOutboxVentaCompletada()
    {
        var e = new Escenario();
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 2)],
            [new PagoEfectivo { Monto = 20000 }],
            ClaveIdempotencia: "CLAVE-123");

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        var evento = Assert.Single(e.Outbox.Datos);
        Assert.Equal("VentaCompletada", evento.TipoEvento);
        Assert.Equal(venta.Id.ToString(), evento.EntidadId);
        Assert.False(evento.Procesado);

        using var doc = JsonDocument.Parse(evento.PayloadJson);
        Assert.Equal(20000, doc.RootElement.GetProperty("Total").GetInt64());
        Assert.Equal("CLAVE-123", doc.RootElement.GetProperty("ClaveIdempotencia").GetString());
    }

    [Fact]
    public async Task VentaConMismaClaveIdempotencia_RetornaExistenteSinDuplicar()
    {
        var e = new Escenario();
        var solicitud1 = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoEfectivo { Monto = 10000 }],
            ClaveIdempotencia: "PAGO-UNICO-999");

        var venta1 = await e.Servicio.RegistrarVentaAsync(solicitud1);

        // Segundo intento con la misma clave (ej: doble clic o reintento de red)
        var solicitud2 = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoEfectivo { Monto = 10000 }],
            ClaveIdempotencia: "PAGO-UNICO-999");

        var venta2 = await e.Servicio.RegistrarVentaAsync(solicitud2);

        Assert.Equal(venta1.Id, venta2.Id);
        Assert.Single(e.Ventas.Datos);
        Assert.Single(e.Outbox.Datos); // No generó segundo evento outbox
        Assert.Equal(9, e.Inventario.Estante.Datos.First().Cantidad); // Solo descontó 1 vez
    }

    [Fact]
    public async Task AnularVenta_InsertaEventoOutboxVentaAnulada()
    {
        var e = new Escenario();
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoEfectivo { Monto = 10000 }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);
        await e.Servicio.AnularVentaAsync(venta.Id, "Devolución por garantía");

        Assert.Equal(2, e.Outbox.Datos.Count);
        var eventoAnulacion = e.Outbox.Datos.Last();
        Assert.Equal("VentaAnulada", eventoAnulacion.TipoEvento);
        Assert.Equal(venta.Id.ToString(), eventoAnulacion.EntidadId);

        using var doc = JsonDocument.Parse(eventoAnulacion.PayloadJson);
        Assert.Equal("Devolución por garantía", doc.RootElement.GetProperty("Motivo").GetString());
    }

    [Fact]
    public async Task SincronizadorSupabase_EnviaPendientesYMarcaProcesado()
    {
        var e = new Escenario();
        var evento = EventoOutbox.Crear(
            "VentaCompletada", "10", "{\"Total\": 50000}");
        await e.Outbox.AgregarAsync(evento);

        var peticiones = 0;
        var handler = new HandlerHttpFalso(req =>
        {
            peticiones++;
            Assert.Contains("apikey", req.Headers.Select(h => h.Key));
            Assert.Contains("Authorization", req.Headers.Select(h => h.Key));
            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        var opciones = new OpcionesApisExternas
        {
            SupabaseUrl = "https://proyecto.supabase.co",
            SupabaseKey = "token-secreto"
        };

        var sincronizador = new ClienteSincronizacionSupabase(
            new HttpClient(handler),
            e.Outbox,
            e.Parametros,
            opciones,
            e.Trabajo,
            e.Reloj);

        var count = await sincronizador.SincronizarPendientesAsync();

        Assert.Equal(1, count);
        Assert.Equal(1, peticiones);
        Assert.True(evento.Procesado);
        Assert.NotNull(evento.FechaProcesado);
        Assert.Null(evento.UltimoError);
    }

    [Fact]
    public async Task SincronizadorSupabase_SinConfiguracion_NoHaceNada()
    {
        var e = new Escenario();
        var evento = EventoOutbox.Crear("Test", "1", "{}");
        await e.Outbox.AgregarAsync(evento);

        var handler = new HandlerHttpFalso(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sincronizador = new ClienteSincronizacionSupabase(
            new HttpClient(handler),
            e.Outbox,
            e.Parametros,
            new OpcionesApisExternas(), // sin URL ni Key
            e.Trabajo,
            e.Reloj);

        var count = await sincronizador.SincronizarPendientesAsync();

        Assert.Equal(0, count);
        Assert.False(evento.Procesado);
    }

    private class HandlerHttpFalso(Func<HttpRequestMessage, HttpResponseMessage> respuesta)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(respuesta(request));
    }
}
