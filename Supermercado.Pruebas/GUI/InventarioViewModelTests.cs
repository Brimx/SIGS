using Supermercado.GUI.ViewModels;

namespace Supermercado.Pruebas.GUI;

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
