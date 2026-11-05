using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Supermercado.GUI.ViewModels;

namespace Supermercado.GUI.Views;

public partial class DashboardView : UserControl
{
    private static readonly FilePickerFileType TipoPanel = new("Panel de SIGS (JSON)")
    {
        Patterns = ["*.json"]
    };

    public DashboardView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is DashboardViewModel vm)
            vm.IniciarRefrescoAutomatico();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.DetenerRefrescoAutomatico();

        base.OnDetachedFromVisualTree(e);
    }

    // Reordenar arrastrando: solo en modo personalización y nunca desde un
    // control hijo interactivo (título o botones de la tarjeta).
    private async void OnTarjetaPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not DashboardViewModel { EnEdicion: true })
            return;

        if (sender is not Control { DataContext: WidgetCardViewModel tarjeta })
            return;

        if (e.Source is Control origen &&
            (origen.FindAncestorOfType<TextBox>() is not null ||
             origen.FindAncestorOfType<Button>() is not null))
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var item = DataTransferItem.CreateText(tarjeta.Id);
        var datos = new DataTransfer();
        datos.Add(item);

        await DragDrop.DoDragDropAsync(e, datos, DragDropEffects.Move);
    }

    private void OnTarjetaOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.Text)
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnTarjetaDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Control { DataContext: WidgetCardViewModel destino })
            return;

        if (e.DataTransfer.TryGetText() is not { } origenId)
            return;

        if (DataContext is not DashboardViewModel vm)
            return;

        await vm.MoverAntesAsync(origenId, destino.Id);
        e.Handled = true;
    }

    private async void OnExportar(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
            return;

        var archivo = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar disposición del panel",
                SuggestedFileName = $"panel-sigs-{DateTime.Now:yyyyMMdd-HHmm}.json",
                DefaultExtension = "json",
                FileTypeChoices = [TipoPanel]
            });

        if (archivo?.TryGetLocalPath() is { } ruta)
            await vm.ExportarAsync(ruta);
    }

    private async void OnImportar(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DashboardViewModel vm)
            return;

        var archivos = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importar disposición del panel",
                AllowMultiple = false,
                FileTypeFilter = [TipoPanel]
            });

        if (archivos.Count > 0 && archivos[0].TryGetLocalPath() is { } ruta)
            await vm.ImportarAsync(ruta);
    }

    private async void OnRenombrar(object? sender, RoutedEventArgs e)
        => await GuardarTituloAsync(sender);

    private async void OnTituloPerdido(object? sender, RoutedEventArgs e)
        => await GuardarTituloAsync(sender);

    private async void OnTituloTecla(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await GuardarTituloAsync(sender);
    }

    private async Task GuardarTituloAsync(object? origen)
    {
        if (origen is not Control { DataContext: WidgetCardViewModel tarjeta })
            return;

        if (DataContext is not DashboardViewModel vm)
            return;

        await vm.RenombrarCommand.ExecuteAsync(tarjeta);
    }
}
