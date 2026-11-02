using Avalonia.Controls;
using Avalonia.Threading;
using Supermercado.GUI.ViewModels;

namespace Supermercado.GUI.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += (_, _) => EnfocarLector();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PosViewModel vm)
        {
            vm.SolicitarFocoEscaneo += EnfocarLector;
        }
    }

    private void EnfocarLector()
    {
        Dispatcher.UIThread.Post(() =>
        {
            InputCodigoBarras?.Focus();
        }, DispatcherPriority.Input);
    }
}
