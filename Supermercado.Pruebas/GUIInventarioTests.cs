using CommunityToolkit.Mvvm.Input;
using Supermercado.Datos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;
using Xunit;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas.GUI;

public class NuevosModulosGuiTests
{
    private static ShellViewModel CrearShell(string nombreRol, Permiso permisos)
    {
        var sesion = new SesionActual();
        sesion.Iniciar(new Usuario
        {
            NombreUsuario = nombreRol.ToLowerInvariant(),
            Empleado = new Empleado
            {
                Activo = true,
                Rol = new Rol { Nombre = nombreRol, Permisos = permisos }
            }
        });

        var shellVm = new ShellViewModel(
            sesion,
            new PosViewModel(),
            new InventarioViewModel(),
            new CajaTurnoViewModel(),
            new DashboardViewModel(),
            new RegistroUsuarioViewModel());
        shellVm.AplicarPermisosDeNavegacion(sesion);
        return shellVm;
    }

    private static void AssertVisibilidad(
        ShellViewModel vm,
        bool pos,
        bool turno,
        bool clientes,
        bool inventario,
        bool compras,
        bool personal,
        bool contabilidad,
        bool dashboard,
        bool barraLateral)
    {
        Assert.Equal(pos, vm.PuedeVerPos);
        Assert.Equal(turno, vm.PuedeVerTurno);
        Assert.Equal(clientes, vm.PuedeVerClientes);
        Assert.Equal(inventario, vm.PuedeVerInventario);
        Assert.Equal(compras, vm.PuedeVerCompras);
        Assert.Equal(personal, vm.PuedeVerPersonal);
        Assert.Equal(contabilidad, vm.PuedeVerContabilidad);
        Assert.Equal(dashboard, vm.PuedeVerDashboard);
        Assert.Equal(barraLateral, vm.MostrarBarraLateral);
    }

    [Fact]
    public void ShellViewModel_Admin_MuestraTodosLosModulos()
    {
        var todos = Enum.GetValues<Permiso>()
            .Aggregate(Permiso.Ninguno, (acc, p) => acc | p);

        AssertVisibilidad(CrearShell(NombresRol.Admin, todos),
            pos: true, turno: true, clientes: true, inventario: true,
            compras: true, personal: true, contabilidad: true, dashboard: true,
            barraLateral: true);
    }

    [Fact]
    public void ShellViewModel_Gerente_MuestraClientesInventarioPersonalFinanzasYDashboard()
    {
        var gerente = Permiso.AnularVenta | Permiso.VerReportes | Permiso.VerFinanzas
            | Permiso.GestionarPersonal | Permiso.VerInventario
            | Permiso.GestionarClientes | Permiso.AutorizarOperacionPos;

        AssertVisibilidad(CrearShell(NombresRol.Gerente, gerente),
            pos: false, turno: false, clientes: true, inventario: true,
            compras: false, personal: true, contabilidad: true, dashboard: true,
            barraLateral: true);
    }

    [Fact]
    public void ShellViewModel_Cajero_MuestraPosTurnoYClientes()
    {
        var cajero = Permiso.Vender | Permiso.AbrirCerrarTurno | Permiso.GestionarClientes;

        AssertVisibilidad(CrearShell(NombresRol.Cajero, cajero),
            pos: true, turno: true, clientes: true, inventario: false,
            compras: false, personal: false, contabilidad: false, dashboard: false,
            barraLateral: true);
    }

    [Fact]
    public void ShellViewModel_Bodeguero_MuestraInventarioYCompras()
    {
        var bodeguero = Permiso.VerInventario | Permiso.AjustarInventario
            | Permiso.Reabastecer | Permiso.GestionarProductos | Permiso.GestionarCompras;

        AssertVisibilidad(CrearShell(NombresRol.Bodeguero, bodeguero),
            pos: false, turno: false, clientes: false, inventario: true,
            compras: true, personal: false, contabilidad: false, dashboard: false,
            barraLateral: true);
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

public sealed class InventarioAltasGuiTests
{
    private readonly ProductosEnMemoria productos = new();
    private readonly LotesEnMemoria lotes = new();
    private readonly StockBodegaEnMemoria bodega = new();
    private readonly MovimientosEnMemoria movimientos = new();
    private readonly CategoriasEnMemoria categorias = new();
    private readonly ProveedoresEnMemoria proveedores = new();
    private readonly ImpuestosEnMemoria impuestos = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioProductos CrearCatalogo(Permiso permisos)
        => new(
            productos,
            lotes,
            new ServicioInventario(null!, bodega, movimientos, null!, null!, reloj,
                productos: productos, lotes: lotes),
            sesion: Sesiones.Con(permisos),
            reloj: reloj,
            categorias: categorias,
            proveedores: proveedores,
            auditoria: auditoria,
            impuestos: impuestos);

    private InventarioViewModel CrearVm(Permiso permisos)
        => new(null!, null!, null!, null!, CrearCatalogo(permisos), Sesiones.Con(permisos));

    [Fact]
    public void PermisosDeRol_GestionarProductos_SoloAdminYBodeguero()
    {
        var roles = ValoresIniciales.Roles();

        Assert.True(roles.Single(r => r.Nombre == NombresRol.Admin).Permisos.HasFlag(Permiso.GestionarProductos));
        Assert.True(roles.Single(r => r.Nombre == NombresRol.Bodeguero).Permisos.HasFlag(Permiso.GestionarProductos));
        Assert.False(roles.Single(r => r.Nombre == NombresRol.Gerente).Permisos.HasFlag(Permiso.GestionarProductos));
        Assert.False(roles.Single(r => r.Nombre == NombresRol.Cajero).Permisos.HasFlag(Permiso.GestionarProductos));
    }

    [Fact]
    public void Admin_MuestraAccionesDeAlta()
    {
        var vm = CrearVm(Permiso.GestionarProductos);

        Assert.True(vm.PuedeGestionarInventario);
    }

    [Fact]
    public void Cajero_OcultaAccionesYRechazaAperturaDeModales()
    {
        var vm = CrearVm(Permiso.Vender);

        Assert.False(vm.PuedeGestionarInventario);

        vm.AbrirModalNuevoProductoCommand.Execute(null);
        vm.AbrirModalEntradaBodegaCommand.Execute(null);

        Assert.False(vm.ModalNuevoProductoAbierto);
        Assert.False(vm.ModalEntradaBodegaAbierto);
        Assert.Contains("No tiene permiso", vm.MensajeEstado);
    }

    [Fact]
    public async Task ConfirmarNuevoProducto_GuardaProductoYCierraModal()
    {
        var vm = CrearVm(Permiso.GestionarProductos);
        await categorias.AgregarAsync(new Categoria { Id = 1, Nombre = "Granos" });
        await proveedores.AgregarAsync(new Proveedor { Id = 1, Nombre = "Distribuidora" });
        await impuestos.AgregarAsync(new Iva { Id = 1, Nombre = "IVA 19 %", PuntosBasicos = 1900 });

        vm.NuevoCodigoBarrasInput = "770123";
        vm.NuevoNombreInput = "Arroz Diana 1 kg";
        vm.CategoriaSeleccionada = categorias.Datos[0];
        vm.NuevoPrecioInput = 4500;
        vm.IvaSeleccionado = impuestos.Datos[0];
        vm.NuevoStockMinimoInput = 10;
        vm.ModalNuevoProductoAbierto = true;

        await vm.ConfirmarNuevoProductoCommand.ExecuteAsync(null);

        Assert.Single(productos.Datos);
        Assert.Equal("Arroz Diana 1 kg", productos.Datos[0].Nombre);
        Assert.False(vm.ModalNuevoProductoAbierto);
        Assert.Contains("creado", vm.MensajeEstado);
        Assert.Contains(AccionAuditoria.ProductoCreado, auditoria.Acciones);
    }

    [Fact]
    public async Task ConfirmarEntradaBodega_CreaLoteConVencimientoYCierraModal()
    {
        var vm = CrearVm(Permiso.GestionarProductos);
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770123",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        });

        vm.ProductoSeleccionado = productos.Datos[0];
        vm.CantidadEntradaInput = 7;
        vm.NumeroLoteInput = "L-2026-07";
        var vencimientoLocal = new DateTime(2027, 5, 1, 12, 0, 0, DateTimeKind.Local);
        vm.FechaVencimientoEntradaInput = new DateTimeOffset(
            vencimientoLocal, TimeZoneInfo.Local.GetUtcOffset(vencimientoLocal));
        vm.ModalEntradaBodegaAbierto = true;

        await vm.ConfirmarEntradaBodegaCommand.ExecuteAsync(null);

        var lote = Assert.Single(lotes.Datos);
        Assert.Equal("L-2026-07", lote.NumeroLote);
        Assert.Equal(new DateOnly(2027, 5, 1), lote.FechaVencimiento);
        Assert.Equal(7, Assert.Single(bodega.Datos).Cantidad);
        Assert.False(vm.ModalEntradaBodegaAbierto);
        Assert.Contains("Entrada registrada", vm.MensajeEstado);
    }

    [Fact]
    public async Task ConfirmarEntradaBodega_SinVencimiento_MuestraMensajeYNoRegistra()
    {
        var vm = CrearVm(Permiso.GestionarProductos);
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770123",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        });

        vm.ProductoSeleccionado = productos.Datos[0];
        vm.NumeroLoteInput = "L-2026-07";
        vm.FechaVencimientoEntradaInput = null;

        await vm.ConfirmarEntradaBodegaCommand.ExecuteAsync(null);

        Assert.Empty(lotes.Datos);
        Assert.Equal("Seleccione la fecha de vencimiento.", vm.MensajeEstado);
    }

    [Fact]
    public async Task CrearProductoAsync_SinPermiso_LanzaAccesoDenegado()
    {
        await categorias.AgregarAsync(new Categoria { Id = 1, Nombre = "Granos" });
        await proveedores.AgregarAsync(new Proveedor { Id = 1, Nombre = "Distribuidora" });
        await impuestos.AgregarAsync(new Iva { Id = 1, Nombre = "IVA 19 %", PuntosBasicos = 1900 });
        var servicio = CrearCatalogo(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearProductoAsync("770999", "Arroz", 1, 4500, 1, 10));

        Assert.Empty(productos.Datos);
    }

    [Fact]
    public async Task EntradaBodega_DatosInvalidos_LanzaValidacionException()
    {
        var servicio = CrearCatalogo(Permiso.GestionarProductos);
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770999",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        });

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

        var sinLote = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarIngresoLoteAsync(5, "  ", hoy.AddDays(10), 10, 0));
        var cantidadCero = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarIngresoLoteAsync(5, "L-1", hoy.AddDays(10), 0, 0));
        var vencida = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarIngresoLoteAsync(5, "L-1", hoy.AddDays(-1), 10, 0));

        Assert.Equal("Falta el número de lote.", sinLote.Message);
        Assert.Equal("La cantidad debe ser positiva.", cantidadCero.Message);
        Assert.Equal("La fecha de vencimiento no puede estar en el pasado.", vencida.Message);
        Assert.Empty(lotes.Datos);
    }
}
