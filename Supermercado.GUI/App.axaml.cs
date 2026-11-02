using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.GUI.Servicios.Impresion;
using Supermercado.GUI.ViewModels;
using Supermercado.GUI.Views;
using Supermercado.Logica;

namespace Supermercado.GUI;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    private IServiceScope? _appScope;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var coleccion = new ServiceCollection();
        ConfigurarServicios(coleccion);

        Services = coleccion.BuildServiceProvider();
        _appScope = Services.CreateScope();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = _appScope.ServiceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
            desktop.Exit += (_, _) => _appScope?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void ConfigurarServicios(IServiceCollection servicios)
    {
        servicios.AgregarLogica();

        // Adaptador de impresión térmica ESC/POS 80mm por defecto
        servicios.AddScoped<IImpresorTicket, ImpresorTicketEscPos>();
        servicios.AddScoped<ImpresorTicketPdf>();

        // ViewModels
        servicios.AddTransient<RegistroUsuarioViewModel>();
        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<PosViewModel>();
        servicios.AddTransient<InventarioViewModel>();
        servicios.AddTransient<CajaTurnoViewModel>();
        servicios.AddTransient<DashboardViewModel>();
        servicios.AddTransient<ShellViewModel>();
        servicios.AddTransient<MainViewModel>();
    }
}
