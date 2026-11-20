using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;
using Xunit;
using System.Globalization;
using System.Net;
using System.Text;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Clientes;
using Supermercado.Logica.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Pruebas;

public class ApisPorDefectoTests
{
    private static readonly string[] Variables =
    [
        "SIGS_APIS_HABILITADAS", "SIGS_TRM_HABILITADA", "SIGS_OFF_HABILITADA",
        "SIGS_TRM_URL", "SIGS_OFF_URL", "SIGS_EMAIL_VERIFIER_URL"
    ];

    private static OpcionesApisExternas ConEntorno(
        params (string Variable, string Valor)[] valores)
    {
        var anteriores = Variables.ToDictionary(
            v => v, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var v in Variables)
                Environment.SetEnvironmentVariable(v, null);
            foreach (var (variable, valor) in valores)
                Environment.SetEnvironmentVariable(variable, valor);

            return OpcionesApisExternas.DesdeEntorno();
        }
        finally
        {
            foreach (var (v, valor) in anteriores)
                Environment.SetEnvironmentVariable(v, valor);
        }
    }

    [Fact]
    public void SinVariables_TrmYCodigoBarrasVienenActivas()
    {
        var o = ConEntorno();

        Assert.Equal(OpcionesApisExternas.TrmUrlPorDefecto, o.TrmUrl);
        Assert.Equal(
            OpcionesApisExternas.CodigoBarrasUrlPorDefecto,
            o.CodigoBarrasBaseUrl);
        Assert.Null(o.VerificadorCorreoUrl);
    }

    [Fact]
    public void InterruptorGeneral_ApagaTodas()
    {
        var o = ConEntorno(("SIGS_APIS_HABILITADAS", "0"));

        Assert.Null(o.TrmUrl);
        Assert.Null(o.CodigoBarrasBaseUrl);
    }

    [Fact]
    public void InterruptorIndividual_ApagaSoloUna()
    {
        var o = ConEntorno(("SIGS_OFF_HABILITADA", "false"));

        Assert.NotNull(o.TrmUrl);
        Assert.Null(o.CodigoBarrasBaseUrl);
    }

    [Fact]
    public void LaUrlSePuedeReemplazar()
    {
        var o = ConEntorno(("SIGS_TRM_URL", "https://trm.local/x"));

        Assert.Equal("https://trm.local/x", o.TrmUrl);
    }

    [Fact]
    public async Task ParametroApagado_ElClienteDeCodigosNoSale()
    {
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.CodigoBarrasHabilitado] = "0";
        parametros.Valores[ClavesParametro.TimeoutSegundos] = "5";
        var llamadas = 0;
        var http = new HttpClient(new ManejadorContador(() => llamadas++));
        var cliente = new ClienteCodigoBarrasOff(
            http,
            new OpcionesApisExternas { CodigoBarrasBaseUrl = "https://off.test" },
            parametros);

        var info = await cliente.ConsultarAsync("123", CancellationToken.None);

        Assert.Null(info);
        Assert.Equal(0, llamadas);
    }

    [Fact]
    public async Task ObtenerBool_SinParametro_UsaElValorPorDefecto()
    {
        var parametros = new ParametrosFalsos();

        Assert.True(await parametros.ObtenerBoolAsync("x.y"));
        Assert.False(await parametros.ObtenerBoolAsync("x.y", false));
    }

    private class ManejadorContador(Action alLlamar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            alLlamar();
            return Task.FromResult(new HttpResponseMessage());
        }
    }
}

public class ClientesExternosTests
{
    private static ServicioParametros CrearParametros()
    {
        var p = new ParametrosFalsos();
        p.Valores[ClavesParametro.TimeoutSegundos] = "2";
        p.Valores[ClavesParametro.Reintentos] = "1";
        return p;
    }

    [Fact]
    public async Task TrmDatosGov_ParseaCentavosConCulturaInvariante()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-CO");
            var json = """
                [
                  {
                    "valor": "4150.75",
                    "unidad": "COP",
                    "vigenciadesde": "2026-04-20T00:00:00.000"
                  }
                ]
                """;
            var handler = new HandlerFalso(_ => new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8)
            });
            var opciones = new OpcionesApisExternas
            {
                TrmUrl = "https://trm.test/recurso.json",
                TrmAppToken = "token-123"
            };
            var cliente = new ClienteTrmDatosGov(
                new HttpClient(handler), opciones);

            var trm = await cliente.ObtenerAsync(CancellationToken.None);

            Assert.Equal(415075L, trm.Centavos);
            Assert.Equal(new DateOnly(2026, 4, 20), trm.Fecha);
            Assert.Equal("token-123", handler.UltimoTokenApp);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
        }
    }

    [Fact]
    public async Task TrmDatosGov_SinUrl_LanzaServicioNoConfigurado()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.OK));
        var cliente = new ClienteTrmDatosGov(
            new HttpClient(handler), new OpcionesApisExternas());

        await Assert.ThrowsAsync<ServicioNoConfiguradoException>(
            () => cliente.ObtenerAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CodigoBarrasOff_ProductoEncontrado_MapeaDatos()
    {
        var json = """
            {
              "status": 1,
              "product": {
                "product_name": "Café Molido 500g",
                "brands": "Sello Rojo",
                "categories": "Bebidas, Café"
              }
            }
            """;
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8)
        });
        var opciones = new OpcionesApisExternas
        {
            CodigoBarrasBaseUrl = "https://off.test"
        };
        var cliente = new ClienteCodigoBarrasOff(
            new HttpClient(handler), opciones, CrearParametros());

        var info = await cliente.ConsultarAsync(
            "7701234567890", CancellationToken.None);

        Assert.NotNull(info);
        Assert.Equal("Café Molido 500g", info.Nombre);
        Assert.Equal("Sello Rojo", info.Marca);
        Assert.Equal("Bebidas, Café", info.Categoria);
    }

    [Fact]
    public async Task CodigoBarrasOff_NoEncontradoOFalloHttp_RetornaNull()
    {
        var handler = new HandlerFalso(_ => new HttpResponseMessage(
            HttpStatusCode.InternalServerError));
        var opciones = new OpcionesApisExternas
        {
            CodigoBarrasBaseUrl = "https://off.test"
        };
        var cliente = new ClienteCodigoBarrasOff(
            new HttpClient(handler), opciones, CrearParametros());

        var info = await cliente.ConsultarAsync(
            "000000", CancellationToken.None);

        Assert.Null(info);
    }

    [Fact]
    public async Task VerificadorCorreo_RespuestasYFailOpen_Funciona()
    {
        var respuestaJson = """{"is_reachable": "invalid"}""";
        var codigoHttp = HttpStatusCode.OK;
        var handler = new HandlerFalso(_ => new HttpResponseMessage(codigoHttp)
        {
            Content = new StringContent(respuestaJson, Encoding.UTF8)
        });
        var opciones = new OpcionesApisExternas
        {
            VerificadorCorreoUrl = "https://mail.test/check"
        };
        var cliente = new ClienteVerificadorCorreoHttp(
            new HttpClient(handler), opciones, CrearParametros());

        Assert.False(await cliente.VerificarAsync("sin-arroba"));
        Assert.False(await cliente.VerificarAsync("falso@dominio.com"));

        respuestaJson = """{"is_reachable": "safe"}""";
        Assert.True(await cliente.VerificarAsync("real@dominio.com"));

        codigoHttp = HttpStatusCode.ServiceUnavailable;
        Assert.True(await cliente.VerificarAsync("real@dominio.com"));
    }

    private sealed class HandlerFalso(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public string? UltimoTokenApp { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-App-Token", out var vals))
                UltimoTokenApp = vals.FirstOrDefault();

            return Task.FromResult(responder(request));
        }
    }
}

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
                Parametros, Auditoria, sesion,
                new RelojFijo(EscenarioInventario.Ahora));
            ServicioClientes = new ServicioClientes(
                Clientes, Auditoria, sesion);
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
        Assert.Equal(0, e.Clientes.Guardados);
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

public class ServicioTrmTests
{
    private static readonly RelojFijo Reloj =
        new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    private static ParametrosFalsos Parametros(int reintentos = 1)
    {
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.Reintentos] = reintentos.ToString();
        parametros.Valores[ClavesParametro.TimeoutSegundos] = "5";
        parametros.Valores[ClavesParametro.TrmUltimaCentavos] = "0";
        parametros.Valores[ClavesParametro.TrmUltimaFecha] = "1970-01-01";
        return parametros;
    }

    [Fact]
    public async Task SiLaApiResponde_GuardaElValorComoRespaldo()
    {
        var cliente = new ClienteTrmFalso
        {
            Respuesta = new Trm(401234, new DateOnly(2026, 10, 17))
        };
        var parametros = Parametros();

        var trm = await new ServicioTrm(cliente, parametros, Reloj)
            .ObtenerAsync();

        Assert.Equal(401234L, trm!.Centavos);
        var guardado = parametros.Valores[ClavesParametro.TrmUltimaCentavos];
        Assert.Equal("401234", guardado);
    }

    [Fact]
    public async Task SiLaApiFalla_ReintentaYUsaElRespaldo()
    {
        var cliente = new ClienteTrmFalso();
        var parametros = Parametros(reintentos: 1);
        parametros.Valores[ClavesParametro.TrmUltimaCentavos] = "400000";
        parametros.Valores[ClavesParametro.TrmUltimaFecha] = "2026-10-16";

        var trm = await new ServicioTrm(cliente, parametros, Reloj)
            .ObtenerAsync();

        Assert.Equal(2, cliente.Llamadas);
        Assert.Equal(400000L, trm!.Centavos);
        Assert.Equal(new DateOnly(2026, 10, 16), trm.Fecha);
    }

    [Fact]
    public async Task SinApiNiRespaldo_DevuelveNullEnVezDeFallar()
    {
        var servicio = new ServicioTrm(
            new ClienteTrmFalso(), Parametros(), Reloj);

        Assert.Null(await servicio.ObtenerAsync());
    }
}

public class ValidadorTests
{
    [Theory]
    [InlineData("Ana", true)]
    [InlineData("María José", true)]
    [InlineData("O'Neil", true)]
    [InlineData("Pérez-Ñúñez", true)]
    [InlineData("Ana3", false)]
    [InlineData("123", false)]
    [InlineData("Ana@", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void Nombre_RechazaNumerosYSimbolos(string? valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsNombreValido(valor));

    [Theory]
    [InlineData("12345", true)]
    [InlineData("1020304050", true)]
    [InlineData("1234", false)]
    [InlineData("12A45", false)]
    [InlineData("12.345.678", false)]
    [InlineData("", false)]
    public void Documento_SoloDigitos(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsDocumentoValido(valor));

    [Theory]
    [InlineData("900123456", true)]
    [InlineData("900123456-1", true)]
    [InlineData("900123456-12", false)]
    [InlineData("123", false)]
    [InlineData("ABC123456", false)]
    public void Nit_AdmiteDigitoDeVerificacion(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsNitValido(valor));

    [Theory]
    [InlineData("", true)]
    [InlineData("3001234567", true)]
    [InlineData("300 123 4567", true)]
    [InlineData("+573001234567", true)]
    [InlineData("12345", false)]
    [InlineData("abc1234567", false)]
    public void Telefono_EsOpcionalPeroConFormato(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsTelefonoValido(valor));

    [Theory]
    [InlineData("", true)]
    [InlineData("a@b.com", true)]
    [InlineData("sin-arroba", false)]
    [InlineData("a@b", false)]
    [InlineData("Ana <a@b.com>", false)]
    public void Correo_EsOpcionalPeroConFormato(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsCorreoValido(valor));

    [Fact]
    public void ExigirNombre_ConNumeros_LanzaValidacion()
    {
        var ex = Assert.Throws<ValidacionException>(
            () => Validador.ExigirNombre("Ana3", "el nombre"));

        Assert.Contains("sin números", ex.Message);
    }

    [Fact]
    public void ExigirNombre_RecortaEspacios()
        => Assert.Equal("Ana", Validador.ExigirNombre("  Ana ", "el nombre"));
}
