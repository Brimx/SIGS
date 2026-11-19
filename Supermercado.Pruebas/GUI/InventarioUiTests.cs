using Supermercado.GUI.ViewModels;

namespace Supermercado.Pruebas.GUI;

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
