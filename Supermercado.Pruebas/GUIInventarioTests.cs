using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;
using Xunit;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas.GUI;

public class NuevosModulosGuiTests
{
    [Fact]
    public void ShellViewModel_CajeroPuro_ActivaModoPantallaCompletaSinSidebar()
    {
        var sesionCajero = new SesionActual();
        var cajero = new Usuario
        {
            Id = 1,
            NombreUsuario = "cajero1",
            Empleado = new Empleado
            {
                Id = 10,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Cajero",
                    Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno | Permiso.GestionarClientes
                }
            }
        };
        sesionCajero.Iniciar(cajero);

        var shellVm = new ShellViewModel(sesionCajero, new PosViewModel(), new InventarioViewModel(), new CajaTurnoViewModel(), new DashboardViewModel(), new RegistroUsuarioViewModel());
        shellVm.AplicarPermisosDeNavegacion(sesionCajero);

        Assert.True(shellVm.PuedeVerPos);
        Assert.True(shellVm.PuedeVerTurno);
        Assert.False(shellVm.PuedeVerCompras);
        Assert.False(shellVm.PuedeVerPersonal);
        Assert.False(shellVm.PuedeVerContabilidad);
        Assert.False(shellVm.PuedeVerDashboard);

        // Al ser cajero puro sin módulos de back-office, la barra lateral se oculta
        Assert.False(shellVm.MostrarBarraLateral);
        Assert.True(shellVm.EsModoCajeroPantallaCompleta);
        Assert.Equal(0, shellVm.AnchoColumnaSidebar.Value);
    }

    [Fact]
    public void ShellViewModel_SupervisorYAdmin_MantieneSidebarVisible()
    {
        var sesionAdmin = new SesionActual();
        var admin = new Usuario
        {
            Id = 2,
            NombreUsuario = "admin",
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Admin",
                    Permisos = Enum.GetValues<Permiso>().Aggregate(Permiso.Ninguno, (acc, p) => acc | p)
                }
            }
        };
        sesionAdmin.Iniciar(admin);

        var shellVm = new ShellViewModel(sesionAdmin, new PosViewModel(), new InventarioViewModel(), new CajaTurnoViewModel(), new DashboardViewModel(), new RegistroUsuarioViewModel());
        shellVm.AplicarPermisosDeNavegacion(sesionAdmin);

        Assert.True(shellVm.PuedeVerPos);
        Assert.True(shellVm.PuedeVerCompras);
        Assert.True(shellVm.PuedeVerPersonal);
        Assert.True(shellVm.PuedeVerContabilidad);
        Assert.True(shellVm.PuedeVerDashboard);

        Assert.True(shellVm.MostrarBarraLateral);
        Assert.False(shellVm.EsModoCajeroPantallaCompleta);
        Assert.Equal(240, shellVm.AnchoColumnaSidebar.Value);
    }

    [Fact]
    public void PosViewModel_PagosMixtos_AgregaYRemueveRenglonesCorrectamente()
    {
        var posVm = new PosViewModel
        {
            MetodoPagoSeleccionado = "Efectivo",
            EfectivoEntregadoPesos = 50000
        };

        posVm.AgregarRenglonPago();
        Assert.Single(posVm.PagosAgregados);
        Assert.Equal("Efectivo", posVm.PagosAgregados[0].Metodo);
        Assert.Equal(50000, posVm.PagosAgregados[0].Monto);

        posVm.MetodoPagoSeleccionado = "Tarjeta";
        posVm.EfectivoEntregadoPesos = 30000;
        posVm.UltimosDigitosTarjeta = "4321";
        posVm.AgregarRenglonPago();

        Assert.Equal(2, posVm.PagosAgregados.Count);
        Assert.Equal(80000, posVm.TotalAbonadoPesos);

        posVm.QuitarRenglonPago(posVm.PagosAgregados[0]);
        Assert.Single(posVm.PagosAgregados);
        Assert.Equal(30000, posVm.TotalAbonadoPesos);
    }

    [Fact]
    public void LoginViewModel_ComandosPin_ModificanPinAcceso()
    {
        var loginVm = new LoginViewModel();
        loginVm.AgregarDigitoPinCommand.Execute("1");
        loginVm.AgregarDigitoPinCommand.Execute("2");
        loginVm.AgregarDigitoPinCommand.Execute("3");
        loginVm.AgregarDigitoPinCommand.Execute("4");

        Assert.Equal("1234", loginVm.PinAcceso);

        loginVm.BorrarDigitoPinCommand.Execute(null);
        Assert.Equal("123", loginVm.PinAcceso);

        loginVm.LimpiarPinCommand.Execute(null);
        Assert.Equal(string.Empty, loginVm.PinAcceso);
    }

    [Theory]
    [InlineData("admin", "admin", "Admin123*", "1111")]
    [InlineData("cajero", "cajero", "Cajero123*", "1234")]
    [InlineData("gerente", "gerente", "Gerente123*", "3344")]
    [InlineData("bodega", "bodega", "Bodega123*", "2233")]
    public void LoginViewModel_SeleccionarCredencialDemo_Carga4RolesCorrectamente(
        string rolKey, string usuarioEsperado, string claveEsperada, string pinEsperado)
    {
        var loginVm = new LoginViewModel();
        loginVm.SeleccionarCredencialDemoCommand.Execute(rolKey);

        Assert.Equal(usuarioEsperado, loginVm.NombreUsuario);
        Assert.Equal(claveEsperada, loginVm.Clave);
        Assert.Equal(pinEsperado, loginVm.PinAcceso);
        Assert.True(loginVm.TieneExito);
        Assert.False(loginVm.TieneError);
    }
}

public sealed class ConfiguracionTelegramTests
{
    [Fact]
    public void GenerarCodigo_UsaAlUsuarioDeLaSesionYPermiteVincularloUnaVez()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        try
        {
            var usuario = new Usuario
            {
                Id = 4,
                EmpleadoId = 12,
                NombreUsuario = "gerencia",
                Empleado = new Empleado
                {
                    Id = 12,
                    Activo = true,
                    Rol = new Rol { Nombre = NombresRol.Gerente }
                }
            };
            var sesion = new SesionActual();
            sesion.Iniciar(usuario);
            var reloj = new TimeProviderFalso();
            var vinculos = new VinculosTelegram(
                reloj,
                Path.Combine(carpeta, "telegram.json"));
            var vm = new ConfiguracionViewModel(
                null, null, sesion, vinculos, reloj);

            vm.GenerarCodigoVinculacionCommand.Execute(null);

            Assert.StartsWith("/start ", vm.InstruccionVinculacion);
            Assert.Contains("10 minutos", vm.MensajeVinculacion);
            var codigo = vm.InstruccionVinculacion[7..];
            Assert.Equal(ResultadoVinculacion.Vinculado, vinculos.Vincular(codigo, 50, 60));
            Assert.Equal(ResultadoVinculacion.CodigoInvalido, vinculos.Vincular(codigo, 51, 60));
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void GenerarCodigo_SinSesionActivaNoExponeCodigo()
    {
        var vm = new ConfiguracionViewModel(
            null,
            null,
            new SesionActual(),
            null,
            TimeProvider.System);

        vm.GenerarCodigoVinculacionCommand.Execute(null);

        Assert.Empty(vm.InstruccionVinculacion);
        Assert.Contains("Inicia sesión", vm.MensajeVinculacion);
    }

    private sealed class TimeProviderFalso : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

public sealed class InventarioUiTests
{
    [Fact]
    public void GrillaSoportaMuchasFilasYTextoLargo()
    {
        var vm = new InventarioViewModel();
        var nombreLargo = new string('x', 800);
        vm.CargarDesdeLista(Enumerable.Range(0, 500).Select(i => new ProductoInventarioItemViewModel
        {
            ProductoId = i + 1,
            NombreProducto = i == 0 ? nombreLargo : $"Producto {i}",
            NumeroLote = $"Lote {i}",
            EnAlertaStockBajo = i % 10 == 0
        }));

        Assert.Equal(500, vm.ProductosFiltrados.Count);
        Assert.Equal(50, vm.AlertasStockBajoCount);
        Assert.Contains(nombreLargo, vm.CrearCsv());
    }

    [Fact]
    public void InventarioUsaControlesAdaptablesYElipsisParaVentanasPequenas()
    {
        var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var ventana = File.ReadAllText(Path.Combine(raiz, "Supermercado.GUI/Views/MainWindow.axaml"));
        var inventario = File.ReadAllText(Path.Combine(raiz, "Supermercado.GUI/Views/InventarioView.axaml"));

        Assert.Contains("MinWidth=\"900\" MinHeight=\"600\"", ventana);
        Assert.Contains("<WrapPanel", inventario);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Auto\"", inventario);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", inventario);
        Assert.Contains("IsVisible=\"{Binding EnAlertaStockBajo}\"", inventario);
    }

    [Fact]
    public void ExportarCsv_EscapaComasComillasYRespetaFiltroVisible()
    {
        var vm = new InventarioViewModel();
        vm.CargarDesdeLista(
        [
            new ProductoInventarioItemViewModel
            {
                CodigoBarras = "100",
                NombreProducto = "Arroz, \"Premium\"",
                NumeroLote = "L-1",
                TotalStock = 9
            },
            new ProductoInventarioItemViewModel
            {
                CodigoBarras = "200",
                NombreProducto = "Azúcar",
                NumeroLote = "L-2",
                TotalStock = 4
            }
        ]);
        vm.FiltroTexto = "arroz";

        var csv = vm.CrearCsv();

        Assert.Contains("\"Arroz, \"\"Premium\"\"\"", csv);
        Assert.Contains("100", csv);
        Assert.DoesNotContain("200", csv);
    }
}

public class InventarioViewModelTests
{
    private static List<ProductoInventarioItemViewModel> CrearMuestra() =>
    [
        new ProductoInventarioItemViewModel
        {
            ProductoId = 1,
            CodigoBarras = "7701001",
            NombreProducto = "Arroz Diana 1kg",
            LoteId = 10,
            NumeroLote = "ARR-001",
            FechaVencimiento = new DateOnly(2027, 5, 1),
            CantidadEstante = 30,
            CantidadBodega = 50,
            TotalStock = 80,
            EnAlertaStockBajo = false,
            ProximoAVencer = false
        },
        new ProductoInventarioItemViewModel
        {
            ProductoId = 2,
            CodigoBarras = "7701002",
            NombreProducto = "Aceite Premier 900ml",
            LoteId = 11,
            NumeroLote = "ACE-002",
            FechaVencimiento = new DateOnly(2026, 11, 10),
            CantidadEstante = 4,
            CantidadBodega = 0,
            TotalStock = 4,
            EnAlertaStockBajo = true,
            ProximoAVencer = true
        }
    ];

    [Fact]
    public void FiltroTexto_ActualizaColeccionDeProductosFiltrados()
    {
        var vm = new InventarioViewModel();
        vm.CargarDesdeLista(CrearMuestra());

        Assert.Equal(2, vm.ProductosFiltrados.Count);

        vm.FiltroTexto = "Aceite";
        Assert.Single(vm.ProductosFiltrados);
        Assert.Equal("7701002", vm.ProductosFiltrados[0].CodigoBarras);
    }

    [Fact]
    public void FiltroStockBajo_SoloMuestraProductosEnAlerta()
    {
        var vm = new InventarioViewModel();
        vm.CargarDesdeLista(CrearMuestra());

        vm.SoloStockBajo = true;
        Assert.Single(vm.ProductosFiltrados);
        Assert.True(vm.ProductosFiltrados[0].EnAlertaStockBajo);
        Assert.Equal("PRÓXIMO A VENCER", vm.ProductosFiltrados[0].EstadoBadge);
    }
}
