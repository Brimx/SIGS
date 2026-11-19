using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Supermercado.GUI.ViewModels;
using System.Text;

namespace Supermercado.GUI.Views;

public partial class InventarioView : UserControl
{
    public InventarioView()
    {
        InitializeComponent();
    }

    private async void ExportarCsv_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not InventarioViewModel vm
            || TopLevel.GetTopLevel(this)?.StorageProvider is not { } proveedor)
            return;

        var archivo = await proveedor.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar inventario filtrado",
            SuggestedFileName = "inventario.csv",
            FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }]
        });
        if (archivo is null)
            return;

        try
        {
            await using var flujo = await archivo.OpenWriteAsync();
            await using var escritor = new StreamWriter(flujo, new UTF8Encoding(true));
            await escritor.WriteAsync(vm.CrearCsv());
            vm.MensajeEstado = $"CSV exportado: {vm.ProductosFiltrados.Count} filas.";
        }
        catch (IOException)
        {
            vm.MensajeEstado = "No se pudo escribir el archivo CSV.";
        }
        catch (UnauthorizedAccessException)
        {
            vm.MensajeEstado = "No hay permiso para escribir el archivo CSV.";
        }
    }
}
