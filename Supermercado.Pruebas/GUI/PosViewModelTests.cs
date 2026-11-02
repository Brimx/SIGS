using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;

namespace Supermercado.Pruebas.GUI;

public class PosViewModelTests
{
    private sealed class ServicioProductosFalso : IServicioProductos
    {
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

        public Task RegistrarAsync(Producto producto) => Task.CompletedTask;
        public Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo)
            => Task.FromResult(_catalogo.GetValueOrDefault(codigo));
        public Task<InfoProductoExterno?> ConsultarExternoAsync(string codigo, CancellationToken ct = default)
            => Task.FromResult<InfoProductoExterno?>(null);
        public Task RegistrarIngresoLoteAsync(int productoId, string numeroLote, DateOnly vencimiento, int cantidad, long costoUnitario)
            => Task.CompletedTask;
    }

    private sealed class ServicioCajaFalso(bool turnoAbierto) : IServicioCaja
    {
        public bool Abierto { get; set; } = turnoAbierto;

        public Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial)
        {
            Abierto = true;
            return Task.FromResult(TurnoCaja.Abrir(cajaId, 1, efectivoInicial, DateTime.UtcNow));
        }

        public Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado)
        {
            Abierto = false;
            return Task.FromResult(new ResultadoArqueo(100000, efectivoContado, efectivoContado - 100000, true));
        }

        public Task<TurnoCaja?> ObtenerTurnoActualAsync()
            => Task.FromResult(Abierto ? TurnoCaja.Abrir(1, 1, 100000, DateTime.UtcNow) : null);
    }

    private sealed class ServicioVentasFalso : IServicioVentas
    {
        public Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud)
        {
            var v = new Venta
            {
                Id = 101,
                Fecha = DateTime.UtcNow,
                PrefijoDian = "709B",
                ConsecutivoDian = 169478,
                Cude = new string('a', 96)
            };
            return Task.FromResult(v);
        }

        public Task AnularVentaAsync(int ventaId, string motivo) => Task.CompletedTask;
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
}
