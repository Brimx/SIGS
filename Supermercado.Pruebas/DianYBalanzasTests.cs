using System.Xml.Linq;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Dian;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class DianYBalanzasTests
{
    private static void ConfigurarParametrosDianValidos(ParametrosFalsos parametros)
    {
        parametros.Valores[ClavesParametro.DianHabilitado] = "1";
        parametros.Valores[ClavesParametro.DianResolucionNumero] = "18764000001";
        parametros.Valores[ClavesParametro.DianResolucionFecha] = "2026-01-01";
        parametros.Valores[ClavesParametro.DianResolucionVigenciaHasta] = "2026-12-31";
        parametros.Valores[ClavesParametro.DianPrefijo] = "SETP";
        parametros.Valores[ClavesParametro.DianRangoDesde] = "1";
        parametros.Valores[ClavesParametro.DianRangoHasta] = "10000";
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "100";
        parametros.Valores[ClavesParametro.DianClaveTecnica] = "fc8eac422eba16e122d5aa92a14da838ecb12300";
        parametros.Valores[ClavesParametro.DianTipoAmbiente] = "2";
        parametros.Valores[ClavesParametro.NegocioNit] = "900123456";
        parametros.Valores[ClavesParametro.NegocioNombre] = "Supermercado SIGS";
        parametros.Valores[ClavesParametro.NegocioDireccion] = "Calle 10 # 20-30";
    }

    #region 1. Módulo 11 DIAN (NIT)

    [Theory]
    [InlineData("800197268", 4)] // DIAN
    [InlineData("899999068", 1)] // Ecopetrol
    [InlineData("890903938", 8)] // Bancolombia
    [InlineData("890900608", 9)] // Almacenes Éxito
    [InlineData("900276962", 1)] // Tiendas D1 (Koba)
    [InlineData("900494411", 8)] // Tiendas Ara (Jerónimo Martins)
    [InlineData("41784931", 5)]  // Cédula / Persona natural
    public void Validador_CalculaDigitoVerificacionNit_OficialDian(string nit, int dvEsperado)
    {
        var dv = Validador.CalcularDigitoVerificacionNit(nit);
        Assert.Equal(dvEsperado, dv);
    }

    [Theory]
    [InlineData("800197268-4", true)]
    [InlineData("800197268-5", false)]
    [InlineData("899999068-1", true)]
    [InlineData("899999068-9", false)]
    [InlineData("890903938-8", true)]
    [InlineData("890900608-9", true)]
    [InlineData("900494411-8", true)]
    [InlineData("41784931-5", true)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("800197268", false)] // Falta guion y DV
    public void Validador_ValidaNitConDv_Correctamente(string? nitConDv, bool esValidoEsperado)
    {
        var esValido = Validador.ValidarNitConDv(nitConDv!);
        Assert.Equal(esValidoEsperado, esValido);
    }

    #endregion

    #region 2. Parser Códigos de Balanza GS1

    [Fact]
    public void ParserCodigoBalanza_TiendasAraPapaya_DescomponePluYGramos()
    {
        // 28 20890 00185 1 -> Papaya 185g
        var res = ParserCodigoBalanza.Parsear("2820890001851");

        Assert.True(res.EsValido);
        Assert.Equal("28", res.Prefijo);
        Assert.Equal("20890", res.Plu);
        Assert.Equal(185, res.Gramos);
        Assert.Equal(0.185m, res.PesoKg);
        Assert.Equal(1, res.DigitoControl);
    }

    [Fact]
    public void ParserCodigoBalanza_TiendasAraManzana_DescomponePluYGramos()
    {
        // 28 21140 00320 5 -> Manzana 320g
        var res = ParserCodigoBalanza.Parsear("2821140003205");

        Assert.True(res.EsValido);
        Assert.Equal("28", res.Prefijo);
        Assert.Equal("21140", res.Plu);
        Assert.Equal(320, res.Gramos);
        Assert.Equal(0.320m, res.PesoKg);
        Assert.Equal(5, res.DigitoControl);
    }

    [Theory]
    [InlineData("7702001001234")] // Código EAN-13 estándar (no balanza)
    [InlineData("1234567890123")] // Prefijo 12 (no 20..29)
    [InlineData("28123")]          // Muy corto
    [InlineData("28208900018519")] // Muy largo
    [InlineData(null)]
    [InlineData("")]
    [InlineData("28ABCDE001851")] // Letras
    public void ParserCodigoBalanza_CodigoInvalido_RetornaInvalido(string? codigo)
    {
        var res = ParserCodigoBalanza.Parsear(codigo!);
        Assert.False(res.EsValido);
        Assert.Equal(0, res.Gramos);
    }

    #endregion

    #region 3. Conversor de Unidades

    [Fact]
    public void ConversorUnidades_GramajesYCantidades_SonCorrectos()
    {
        Assert.Equal(50000, ConversorUnidades.Bultos50KgAGramos(1));
        Assert.Equal(125000, ConversorUnidades.Bultos50KgAGramos(2.5m));

        Assert.Equal(12500, ConversorUnidades.ArrobasAGramos(1));
        Assert.Equal(50000, ConversorUnidades.ArrobasAGramos(4));

        Assert.Equal(1000, ConversorUnidades.KilosAGramos(1));
        Assert.Equal(3500, ConversorUnidades.KilosAGramos(3.5m));

        Assert.Equal(500, ConversorUnidades.LibrasAGramos(1));
        Assert.Equal(1500, ConversorUnidades.LibrasAGramos(3));

        Assert.Equal(24, ConversorUnidades.CajasAUnidades(1, 24));
        Assert.Equal(72, ConversorUnidades.CajasAUnidades(3, 24));
    }

    [Fact]
    public void ConversorUnidades_PreciosUnitarios_CalculaCorrectamente()
    {
        // Bulto de 50kg comprado en $100.000 -> Costo por gramo = $2
        var costoGramo = ConversorUnidades.CalcularCostoPorGramo(100000, 1, ConversorUnidades.Bulto50Kg);
        Assert.Equal(2.0m, costoGramo);

        // Caja de 24 cervezas comprada en $48.000 -> Costo unitario = $2.000
        var costoUnitario = ConversorUnidades.CalcularCostoUnitarioDeCaja(48000, 1, 24);
        Assert.Equal(2000L, costoUnitario);
    }

    #endregion

    #region 4. CUDE SHA-384 y QR DIAN

    [Fact]
    public void GeneradorCudeDian_GeneraSha384De96CaracteresHexYQrValido()
    {
        var fecha = new DateTime(2026, 11, 2, 14, 30, 0, DateTimeKind.Utc);
        var cude = GeneradorCudeDian.CalcularCude(
            numDoc: "SETP990000001",
            fechaUtc: fecha,
            subtotal: 40500,
            totalIva: 9500,
            total: 50000,
            nitEmisor: "900123456",
            numAdquirente: "222222222222",
            claveTecnica: "fc8eac422eba16e122d5aa92a14da838ecb12300",
            tipoAmbiente: "2");

        Assert.NotNull(cude);
        Assert.Equal(96, cude.Length); // SHA-384 = 384 bits = 48 bytes = 96 hex chars
        Assert.True(cude.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')));

        var qr = GeneradorCudeDian.GenerarCadenaQr(cude);

        Assert.StartsWith("https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey=", qr);
        Assert.Contains(cude, qr);
    }

    #endregion

    #region 5. Generación XML UBL 2.1

    [Fact]
    public void GeneradorUblDian_GeneraXmlUbl21Valido()
    {
        var venta = new Venta
        {
            Id = 10,
            PrefijoDian = "POS",
            ConsecutivoDian = 101,
            Cude = "abc123cude96caractereshexadecimalesparaefectosdeprueba1234567890abcdef1234567890abcdef12345678",
            CadenaQr = "https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey=abc123cude",
            FechaValidacionDian = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc),
            DocumentoAdquirente = "222222222222",
            NombreAdquirente = "CONSUMIDOR FINAL"
        };

        var arroz = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7701",
            Nombre = "Arroz Diana 1kg",
            PrecioVenta = 5000,
            Impuesto = new Iva { PuntosBasicos = 0 }
        };
        venta.AgregarProducto(arroz, 2);

        var xmlString = GeneradorUblDian.GenerarXml(
            venta,
            resolucionNumero: "18764000001",
            fechaResolucion: "2026-01-01",
            vigenciaHasta: "2026-12-31",
            nitEmisor: "900123456",
            nombreEmisor: "Supermercado El Ahorro S.A.S.",
            direccionEmisor: "Carrera 7 # 12-34");

        Assert.NotNull(xmlString);
        var doc = XDocument.Parse(xmlString);
        Assert.Equal("Invoice", doc.Root?.Name.LocalName);

        // Namespaces UBL 2.1
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

        var idElem = doc.Root?.Element(cbc + "ID");
        Assert.Equal("POS101", idElem?.Value);

        var uuidElem = doc.Root?.Element(cbc + "UUID");
        Assert.Equal(venta.Cude, uuidElem?.Value);

        var lineas = doc.Root?.Elements(cac + "InvoiceLine");
        Assert.Single(lineas!);
    }

    #endregion

    #region 6. Servicio DIAN (Control de Resoluciones y Rangos)

    [Fact]
    public async Task ServicioDian_ConsultarEstadoResolucion_DetectaVigenciaYRangos()
    {
        var parametros = new ParametrosFalsos();
        ConfiguracionDian(parametros);
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "1500";

        var ahora = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(ahora);

        var servicio = new ServicioDian(parametros, reloj);
        var estado = await servicio.ConsultarEstadoResolucionAsync();

        Assert.True(estado.EsValida);
        Assert.False(estado.EstaVencidaPorFecha);
        Assert.False(estado.EstaAgotadaPorFolios);
        Assert.Equal(8501, estado.FoliosRestantes);
        Assert.True(estado.DiasRestantes > 30);
        Assert.Null(estado.Alerta);
    }

    [Fact]
    public async Task ServicioDian_ResolucionVencidaPorFecha_LanzaValidacionException()
    {
        var parametros = new ParametrosFalsos();
        ConfiguracionDian(parametros);
        parametros.Valores[ClavesParametro.DianResolucionVigenciaHasta] = "2026-12-31"; // Vencida
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "50";

        // Simular fecha posterior a vigencia
        var ahora = new DateTimeOffset(2027, 2, 1, 10, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(ahora);

        var servicio = new ServicioDian(parametros, reloj);
        var venta = new Venta();

        var ex = await Assert.ThrowsAsync<ValidacionException>(() => servicio.EmitirDocumentoPosAsync(venta));
        Assert.Contains("venci", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ServicioDian_FoliosAgotados_LanzaValidacionException()
    {
        var parametros = new ParametrosFalsos();
        ConfiguracionDian(parametros);
        parametros.Valores[ClavesParametro.DianRangoDesde] = "1";
        parametros.Valores[ClavesParametro.DianRangoHasta] = "500";
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "501"; // Agotado

        var ahora = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(ahora);

        var servicio = new ServicioDian(parametros, reloj);
        var venta = new Venta();

        var ex = await Assert.ThrowsAsync<ValidacionException>(() => servicio.EmitirDocumentoPosAsync(venta));
        Assert.Contains("agot", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ServicioDian_AlertaPreventiva_CuandoQuedanPocosFolios()
    {
        var parametros = new ParametrosFalsos();
        ConfiguracionDian(parametros);
        parametros.Valores[ClavesParametro.DianRangoDesde] = "1";
        parametros.Valores[ClavesParametro.DianRangoHasta] = "1000";
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "950"; // Quedan 51 folios

        var ahora = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(ahora);

        var servicio = new ServicioDian(parametros, reloj);
        var estado = await servicio.ConsultarEstadoResolucionAsync();

        Assert.True(estado.EsValida);
        Assert.NotNull(estado.Alerta);
        Assert.Contains("51 folios", estado.Alerta);
    }

    private static void ConfiguracionDian(ParametrosFalsos parametros)
    {
        parametros.Valores[ClavesParametro.DianHabilitado] = "1";
        parametros.Valores[ClavesParametro.DianResolucionNumero] = "18764000001";
        parametros.Valores[ClavesParametro.DianResolucionFecha] = "2026-01-01";
        parametros.Valores[ClavesParametro.DianResolucionVigenciaHasta] = "2026-12-31";
        parametros.Valores[ClavesParametro.DianPrefijo] = "SETP";
        parametros.Valores[ClavesParametro.DianRangoDesde] = "1";
        parametros.Valores[ClavesParametro.DianRangoHasta] = "10000";
        parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "100";
        parametros.Valores[ClavesParametro.DianClaveTecnica] = "fc8eac422eba16e122d5aa92a14da838ecb12300";
        parametros.Valores[ClavesParametro.DianTipoAmbiente] = "2";
        parametros.Valores[ClavesParametro.NegocioNit] = "900123456";
        parametros.Valores[ClavesParametro.NegocioNombre] = "Supermercado SIGS";
        parametros.Valores[ClavesParametro.NegocioDireccion] = "Calle 10 # 20-30";
    }

    #endregion

    #region 7. Integración ServicioVentas (Habeas Data, Régimen Tributario, Balanzas)

    private class EscenarioIntegracion
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
        public ServicioDian ServicioDian { get; }
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public EscenarioIntegracion()
        {
            Turnos.Datos.Add(TurnoCaja.Abrir(1, 1, 0, EscenarioInventario.Ahora.UtcDateTime));
            ConfiguracionDian(Parametros);
            Parametros.Valores[ClavesParametro.PesosPorPunto] = "1000";
            Parametros.Valores[ClavesParametro.ValorPuntoPesos] = "10";
            Parametros.Valores[ClavesParametro.ResponsableIva] = "1"; // Por defecto responsable
            Parametros.Valores[ClavesParametro.DianHabilitado] = "0"; // Deshabilitado para pruebas no-DIAN

            var sesion = Sesiones.Con(Permiso.Vender | Permiso.AnularVenta | Permiso.GestionarClientes);

            ServicioDian = new ServicioDian(Parametros, Reloj);

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
                Outbox,
                ServicioDian);
        }
    }

    [Fact]
    public async Task ServicioVentas_HabeasData_ClienteSinAutorizacionForzaConsumidorFinal()
    {
        var e = new EscenarioIntegracion();
        var producto = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7701",
            PrecioVenta = 10000,
            Impuesto = new Iva { PuntosBasicos = 0 }
        };
        e.Productos.Datos.Add(producto);
        e.Inventario.AgregarEstante(1, 10, 10, 5);

        // Cliente que expresamente NO autoriza tratamiento de datos
        var cliente = new Cliente
        {
            Id = 42,
            Documento = "1020304050",
            Nombres = "Juan",
            Apellidos = "Perez",
            Telefono = "3001234567",
            AutorizaTratamientoDatos = false
        };
        e.Clientes.Datos.Add(cliente);

        var solicitud = new SolicitudVenta(
            [new LineaVenta("7701", 1)],
            [new PagoEfectivo { Monto = 10000 }],
            ClienteId: 42);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        // Debe forzar CONSUMIDOR FINAL (222222222222)
        Assert.Equal(Venta.DocumentoConsumidorFinal, venta.DocumentoAdquirente);
        Assert.Equal(Venta.NombreConsumidorFinal, venta.NombreAdquirente);
    }

    [Fact]
    public async Task ServicioVentas_HabeasData_ClienteConAutorizacionRegistraSusDatos()
    {
        var e = new EscenarioIntegracion();
        var producto = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7701",
            PrecioVenta = 10000,
            Impuesto = new Iva { PuntosBasicos = 0 }
        };
        e.Productos.Datos.Add(producto);
        e.Inventario.AgregarEstante(1, 10, 10, 5);

        var cliente = new Cliente
        {
            Id = 42,
            Documento = "1020304050",
            Nombres = "Juan",
            Apellidos = "Perez",
            AutorizaTratamientoDatos = true
        };
        e.Clientes.Datos.Add(cliente);

        var solicitud = new SolicitudVenta(
            [new LineaVenta("7701", 1)],
            [new PagoEfectivo { Monto = 10000 }],
            ClienteId: 42);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Equal("1020304050", venta.DocumentoAdquirente);
        Assert.Equal("Juan Perez", venta.NombreAdquirente);
    }

    [Fact]
    public async Task ServicioVentas_SwitchRegimenTributario_NoResponsablePoneIvaEnCero()
    {
        var e = new EscenarioIntegracion();
        // Régimen no responsable de IVA (persona natural / monotributo)
        e.Parametros.Valores[ClavesParametro.ResponsableIva] = "0";

        var productoConIva = new ProductoUnitario
        {
            Id = 2,
            CodigoBarras = "7702",
            PrecioVenta = 11900,
            Impuesto = new Iva { PuntosBasicos = 1900 } // 19% IVA configurado en catálogo
        };
        e.Productos.Datos.Add(productoConIva);
        e.Inventario.AgregarEstante(2, 10, 10, 5);

        var solicitud = new SolicitudVenta(
            [new LineaVenta("7702", 1)],
            [new PagoEfectivo { Monto = 11900 }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        // Como no es responsable, el IVA liquidado es 0
        Assert.Equal(0L, venta.TotalIva);
        Assert.All(venta.Detalles, d => Assert.Equal(0L, d.Iva));
    }

    [Fact]
    public async Task ServicioVentas_EscaneoCodigoBalanza_ResuelveProductoPesadoYGramos()
    {
        var e = new EscenarioIntegracion();
        var papaya = new ProductoPesado
        {
            Id = 5,
            CodigoBarras = "20890", // PLU registrado en el sistema
            Nombre = "Papaya Maradol",
            PrecioVenta = 4000, // $4.000 por kilo ($4 por gramo)
            Impuesto = new Iva { PuntosBasicos = 0 }
        };
        e.Productos.Datos.Add(papaya);
        e.Inventario.AgregarEstante(5, 50000, 50000, 5000); // 50kg en inventario

        // Se escanea el sticker de balanza: 28 20890 00500 0 (500 gramos de papaya)
        var codigoStickerBalanza = "2820890005000";

        var solicitud = new SolicitudVenta(
            [new LineaVenta(codigoStickerBalanza, 1)],
            [new PagoEfectivo { Monto = 2000 }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Single(venta.Detalles);
        var detalle = venta.Detalles[0];
        Assert.Equal(500, detalle.Cantidad); // Cantidad en gramos
        Assert.Equal(2000L, detalle.Subtotal); // 500g * $4.000/1000 = $2.000
    }

    [Fact]
    public async Task ServicioVentas_ConDianHabilitado_EmiteDocumentoElectronicoPos()
    {
        var e = new EscenarioIntegracion();
        e.Parametros.Valores[ClavesParametro.DianHabilitado] = "1";
        e.Parametros.Valores[ClavesParametro.DianSiguienteConsecutivo] = "100";

        var producto = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7701",
            PrecioVenta = 10000,
            Impuesto = new Iva { PuntosBasicos = 1900 }
        };
        e.Productos.Datos.Add(producto);
        e.Inventario.AgregarEstante(1, 10, 10, 5);

        var solicitud = new SolicitudVenta(
            [new LineaVenta("7701", 1)],
            [new PagoEfectivo { Monto = 10000 }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        // Debe haber emitido campos DIAN
        Assert.NotNull(venta.Cude);
        Assert.Equal(96, venta.Cude.Length);
        Assert.NotNull(venta.CadenaQr);
        Assert.StartsWith("https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey=", venta.CadenaQr);
        Assert.Equal("SETP", venta.PrefijoDian);
        Assert.Equal(100L, venta.ConsecutivoDian);
        Assert.NotNull(venta.FechaValidacionDian);
        Assert.NotNull(venta.XmlUbl);
        Assert.Contains("<Invoice", venta.XmlUbl);

        // Consecutivo debió avanzar a 101 en parámetros
        Assert.Equal("101", e.Parametros.Valores[ClavesParametro.DianSiguienteConsecutivo]);
    }

    #endregion
}
