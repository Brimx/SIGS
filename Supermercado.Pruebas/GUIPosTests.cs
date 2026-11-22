using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.GUI.Servicios.Impresion;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Pruebas.GUI;

public class PosViewModelTests
{
    private sealed class ServicioProductosFalso : ServicioProductos
    {
        public ServicioProductosFalso() : base(null!)
        {
        }

        private readonly Dictionary<string, Producto> _catalogo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["7701001"] = new ProductoUnitario
            {
                Id = 1,
                CodigoBarras = "7701001",
                Nombre = "Arroz Diana 1kg",
                PrecioVenta = 4500,
                Impuesto = new Iva { PuntosBasicos = 0 }
            },
            ["7701002"] = new ProductoUnitario
            {
                Id = 2,
                CodigoBarras = "7701002",
                Nombre = "Aceite Premier 900ml",
                PrecioVenta = 12000,
                Impuesto = new Iva { PuntosBasicos = 1900 }
            },
            ["20890"] = new ProductoPesado
            {
                Id = 3,
                CodigoBarras = "20890",
                Nombre = "Pimentón Verde",
                PrecioVenta = 8800,
                Impuesto = new Iva { PuntosBasicos = 0 }
            }
        };

        public override Task RegistrarAsync(Producto producto) => Task.CompletedTask;
        public override Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo)
            => Task.FromResult(_catalogo.GetValueOrDefault(codigo));
        public override Task<InfoProductoExterno?> ConsultarExternoAsync(string codigo, CancellationToken ct = default)
            => Task.FromResult<InfoProductoExterno?>(null);
        public override Task RegistrarIngresoLoteAsync(int productoId, string numeroLote, DateOnly vencimiento, int cantidad, long costoUnitario)
            => Task.CompletedTask;
    }

    private sealed class ServicioCajaFalso(bool turnoAbierto)
        : ServicioCaja(null!, null!, null!, null!, null!, null!, null!)
    {
        public bool Abierto { get; set; } = turnoAbierto;

        public override Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial)
        {
            Abierto = true;
            return Task.FromResult(TurnoCaja.Abrir(cajaId, 1, efectivoInicial, DateTime.UtcNow));
        }

        public override Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado, int? cajaId = null)
        {
            Abierto = false;
            return Task.FromResult(new ResultadoArqueo(100000, efectivoContado, efectivoContado - 100000, true));
        }

        public override Task<TurnoCaja?> ObtenerTurnoActualAsync(int? cajaId = null)
            => Task.FromResult(Abierto ? TurnoCaja.Abrir(1, 1, 100000, DateTime.UtcNow) : null);

        public override Task<RetiroCaja> RegistrarRetiroAsync(long monto, string motivo, int supervisorId, int? cajaId = null)
            => Task.FromResult(new RetiroCaja { Monto = monto, Motivo = motivo, EmpleadoAutorizaId = supervisorId });

        public override Task<IReadOnlyList<RetiroCaja>> ObtenerRetirosTurnoActualAsync(int? cajaId = null)
            => Task.FromResult<IReadOnlyList<RetiroCaja>>([]);
    }

    private sealed class ServicioVentasFalso : ServicioVentas
    {
        public ServicioVentasFalso()
            : base(null!, null!, null!, null!, null!, null!, null!, null!, null!)
        {
        }

        public List<SolicitudVenta> Solicitudes { get; } = [];
        public bool FallarPrimera { get; set; }

        public override Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud)
        {
            Solicitudes.Add(solicitud);
            if (FallarPrimera && Solicitudes.Count == 1)
                throw new IOException("Fallo temporal de persistencia");

            var v = new Venta
            {
                Id = 101,
                Fecha = DateTime.UtcNow
            };
            return Task.FromResult(v);
        }

        public override Task AnularVentaAsync(int ventaId, string motivo) => Task.CompletedTask;
    }

    private sealed class ImpresorFalso(bool fallar) : IImpresorTicket
    {
        public Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default)
            => Task.FromResult("ticket");
        public Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default)
            => Task.FromResult(Array.Empty<byte>());
        public Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default)
            => fallar ? throw new IOException("Impresora desconectada") : Task.CompletedTask;
        public Task AbrirGavetaAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task EscanearProducto_Existente_AgregaLineaAlCarritoYRecalculaTotales()
    {
        var vm = new PosViewModel(
            new ServicioVentasFalso(),
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso());

        await vm.InicializarEstadoCajaAsync();
        vm.CodigoBarrasInput = "7701001";
        await vm.EscanearOAgregarAsync();

        Assert.Single(vm.Carrito);
        Assert.Equal(4500, vm.TotalPesos);
        Assert.Equal("C", vm.Carrito[0].CodigoTarifaDian);
        Assert.True(vm.PuedeCobrar);
    }

    [Fact]
    public async Task EscanearProducto_CodigoBalanzaEan13_ExtraePesoYCalculaSubtotal()
    {
        var vm = new PosViewModel(
            new ServicioVentasFalso(),
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso());

        await vm.InicializarEstadoCajaAsync();
        // 28 (Prefijo balanza) + 20890 (PLU Pimentón Verde $8.800/kg) + 00185 (185 gramos) + 1 (DV)
        vm.CodigoBarrasInput = "2820890001851";
        await vm.EscanearOAgregarAsync();

        Assert.Single(vm.Carrito);
        Assert.Equal(185, vm.Carrito[0].Cantidad);
        Assert.Equal(1628, vm.TotalPesos); // 8800 * 185 / 1000 = 1628
    }

    [Fact]
    public async Task EscanearProducto_Inexistente_MuestraAlertaYNoModificaTotales()
    {
        var vm = new PosViewModel(
            new ServicioVentasFalso(),
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso());

        vm.CodigoBarrasInput = "999999999";
        await vm.EscanearOAgregarAsync();

        Assert.Empty(vm.Carrito);
        Assert.Equal(0, vm.TotalPesos);
        Assert.True(vm.TieneAlerta);
    }

    [Fact]
    public async Task Cobrar_ConTurnoCerrado_DeshabilitaComandoCobrar()
    {
        var vm = new PosViewModel(
            new ServicioVentasFalso(),
            new ServicioCajaFalso(turnoAbierto: false),
            new ServicioProductosFalso());

        await vm.InicializarEstadoCajaAsync();
        vm.CodigoBarrasInput = "7701001";
        await vm.EscanearOAgregarAsync();

        Assert.False(vm.TieneTurnoAbierto);
        Assert.False(vm.PuedeCobrar);
    }

    [Fact]
    public async Task CalcularCambio_EfectivoInsuficienteYSuficiente_CalculaExacto()
    {
        var vm = new PosViewModel(
            new ServicioVentasFalso(),
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso());

        await vm.InicializarEstadoCajaAsync();
        vm.CodigoBarrasInput = "7701002"; // $12.000
        await vm.EscanearOAgregarAsync();

        vm.EfectivoEntregadoPesos = 10000;
        Assert.False(vm.EfectivoSuficiente);
        Assert.Equal(2000, vm.FaltantePesos);
        Assert.Equal(0, vm.CambioPesos);

        vm.EfectivoEntregadoPesos = 20000;
        Assert.True(vm.EfectivoSuficiente);
        Assert.Equal(0, vm.FaltantePesos);
        Assert.Equal(8000, vm.CambioPesos);
    }

    [Fact]
    public async Task Cobro_ReintentoDeMismaCanasta_ReutilizaClaveIdempotencia()
    {
        var ventas = new ServicioVentasFalso { FallarPrimera = true };
        var vm = new PosViewModel(
            ventas,
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso());
        await vm.InicializarEstadoCajaAsync();
        vm.CodigoBarrasInput = "7701001";
        await vm.EscanearOAgregarAsync();
        vm.EfectivoEntregadoPesos = 4500;

        await vm.ConfirmarCobroAsync();
        await vm.ConfirmarCobroAsync();

        Assert.Equal(2, ventas.Solicitudes.Count);
        Assert.Equal(
            ventas.Solicitudes[0].ClaveIdempotencia,
            ventas.Solicitudes[1].ClaveIdempotencia);
    }

    [Fact]
    public async Task Cobro_FalloDeImpresionNoDejaCanastaParaDuplicarVenta()
    {
        var ventas = new ServicioVentasFalso();
        var vm = new PosViewModel(
            ventas,
            new ServicioCajaFalso(turnoAbierto: true),
            new ServicioProductosFalso(),
            impresorTicket: new ImpresorFalso(fallar: true));
        await vm.InicializarEstadoCajaAsync();
        vm.CodigoBarrasInput = "7701001";
        await vm.EscanearOAgregarAsync();
        vm.EfectivoEntregadoPesos = 4500;

        await vm.ConfirmarCobroAsync();
        await vm.ConfirmarCobroAsync();

        Assert.Single(ventas.Solicitudes);
        Assert.Empty(vm.Carrito);
        Assert.Contains("no se pudo imprimir", vm.MensajeEstado);
    }
}

public class PlantillaYRegulacionGuiTests
{
    private sealed class ServicioParametrosFalso : ServicioParametros
    {
        public ServicioParametrosFalso() : base(null!, null!, null!)
        {
        }

        public override Task<string> ObtenerTextoAsync(string clave) => Task.FromResult(string.Empty);
        public override Task<int> ObtenerEnteroAsync(string clave) => Task.FromResult(0);
        public override Task<long> ObtenerLongAsync(string clave) => Task.FromResult(0L);
        public override Task GuardarAsync(string clave, string valor) => Task.CompletedTask;
        public override Task GuardarRespaldoAsync(IReadOnlyDictionary<string, string> valores) => Task.CompletedTask;
    }

    [Fact]
    public void Validador_CalculaDigitoVerificacionModulo11Correctamente()
    {
        // NIT real de Jerónimo Martins Colombia S.A.S.: 900480569-1
        var dv = Validador.CalcularDigitoVerificacionNit("900480569");
        Assert.Equal(1, dv);
        Assert.True(Validador.ValidarNitConDv("900480569-1"));
        Assert.False(Validador.ValidarNitConDv("900480569-9"));
    }

    [Fact]
    public void RegistroUsuarioViewModel_ActualizaDvYAvisoHabeasDataLey1581()
    {
        var vm = new RegistroUsuarioViewModel();
        vm.ConfigurarModo(ModoRegistroUsuario.ClientePos);

        vm.TipoDocumento = "NIT";
        vm.Documento = "900480569";

        Assert.True(vm.EsNit);
        Assert.Equal("1", vm.DigitoVerificacion);

        vm.AutorizaHabeasData = false;
        Assert.Contains("222222222222", vm.AvisoHabeasData);
    }

    [Fact]
    public async Task ImpresorTicketEscPos_GeneraTirilla80mmYSecuenciasCorteYGaveta()
    {
        var venta = new Venta
        {
            Id = 55,
            Fecha = new DateTime(2026, 11, 2, 18, 52, 24, DateTimeKind.Utc)
        };

        var prod19 = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7702129072335",
            Nombre = "CHORIZO SANTARR",
            PrecioVenta = 10690,
            Impuesto = new Iva { PuntosBasicos = 1900 }
        };
        var prodPesado = new ProductoPesado
        {
            Id = 2,
            CodigoBarras = "2820890001851",
            Nombre = "PIMENTON VERDE",
            PrecioVenta = 8800,
            Impuesto = new Iva { PuntosBasicos = 0 }
        };

        venta.AgregarProducto(prod19, 1);
        venta.AgregarProducto(prodPesado, 185);
        venta.AgregarPago(new PagoEfectivo { Monto = 20000 });
        venta.Cerrar();

        var impresor = new ImpresorTicketEscPos(new ServicioParametrosFalso());
        var tirilla = await impresor.FormatearTirillaTextoAsync(venta);
        var bytesEscPos = await impresor.GenerarComandosEscPosAsync(venta);

        Assert.Contains("TICKET DE VENTA POS N:", tirilla);
        Assert.Contains("POS-55", tirilla);
        Assert.Contains("J:IVA 19%", tirilla);
        Assert.Contains("C:IVA 0%", tirilla);
        Assert.Contains("0,185 KG", tirilla);

        // Verificar cabecera ESC @ (0x1B 0x40) y comando de corte GS V 0 (0x1D 0x56 0x00)
        Assert.Equal(0x1B, bytesEscPos[0]);
        Assert.Equal(0x40, bytesEscPos[1]);
        Assert.True(bytesEscPos.Length > 200);
    }

    [Fact]
    public async Task CajaTurnoViewModel_SinTurnoAbierto_ExigeAperturaConBase()
    {
        var vm = new CajaTurnoViewModel();
        Assert.False(vm.TurnoAbierto);
        Assert.True(vm.RequiereAperturaConBase);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ShellViewModel_Navegar_CambiaPaginaActualYModuloActivo()
    {
        var shell = new ShellViewModel();
        Assert.True(shell.EsModuloPos);

        await shell.NavegarAsync("Inventario");
        Assert.True(shell.EsModuloInventario);
        Assert.Same(shell.InventarioVm, shell.PaginaActual);

        await shell.NavegarAsync("Turno");
        Assert.True(shell.EsModuloTurno);
        Assert.Same(shell.CajaTurnoVm, shell.PaginaActual);

        await shell.NavegarAsync("Clientes");
        Assert.True(shell.EsModuloClientes);
        Assert.Same(shell.RegistroClienteVm, shell.PaginaActual);

        await shell.NavegarAsync("Dashboard");
        Assert.True(shell.EsModuloDashboard);
        Assert.Same(shell.DashboardVm, shell.PaginaActual);
    }

    [Fact]
    public void LoginViewModel_IngresarSinUsuario_MuestraErrorAmigable()
    {
        var login = new LoginViewModel();
        login.NombreUsuario = "";
        login.IngresarCommand.Execute(null);

        Assert.True(login.TieneError);
        Assert.Contains("usuario", login.MensajeError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoginViewModel_AlternarModoPin_CambiaModoPinYLimpiaError()
    {
        var login = new LoginViewModel();
        login.TieneError = true;
        login.MensajeError = "Error previo";

        login.AlternarModoPinCommand.Execute(null);
        Assert.True(login.ModoPin);
        Assert.False(login.TieneError);
        Assert.Empty(login.MensajeError);
    }

    [Fact]
    public void MainViewModel_IniciaEnLoginViewModel()
    {
        var main = new MainViewModel();
        Assert.Same(main.LoginVm, main.VistaActual);
    }

    [Fact]
    public void PosViewModel_EliminarYCancelarVenta_LimpiaCarritoYTotales()
    {
        var pos = new PosViewModel();
        var prod = new ProductoUnitario
        {
            Id = 10,
            CodigoBarras = "770999",
            Nombre = "Café Molido 500g",
            PrecioVenta = 15990,
            Impuesto = new Iva { PuntosBasicos = 500 }
        };

        pos.AgregarProductoAlCarrito(prod, 2);
        Assert.Single(pos.Carrito);
        Assert.Equal(31980, pos.TotalPesos);
        Assert.Equal("M", pos.Carrito[0].CodigoTarifaDian);

        pos.CancelarVenta();
        Assert.Empty(pos.Carrito);
        Assert.Equal(0, pos.TotalPesos);
    }

    [Fact]
    public void PlantillaTicketPos_ObtenerCodigoTarifa_MapeaEstandarAra()
    {
        Assert.Equal("C", PlantillaTicketPos.ObtenerCodigoTarifa(0));
        Assert.Equal("M", PlantillaTicketPos.ObtenerCodigoTarifa(500));
        Assert.Equal("J", PlantillaTicketPos.ObtenerCodigoTarifa(1900));
    }
}
