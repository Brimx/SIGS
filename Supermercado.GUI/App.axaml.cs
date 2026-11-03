using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Contratos;
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

        // Asegura esquema y semillas básicas en SQLite o PostgreSQL antes de renderizar vistas
        try
        {
            var inicializador = _appScope.ServiceProvider.GetRequiredService<IInicializadorBaseDatos>();
            inicializador.CrearYSembrarAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIGS DB] Advertencia al inicializar BD: {ex.Message}");
        }

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

        // ViewModels (Operación POS, Shell y Back-Office P2)
        servicios.AddTransient(sp => new RegistroUsuarioViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new LoginViewModel(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<Supermercado.Logica.Seguridad.ISesionActual>()));
        servicios.AddTransient(sp => new PosViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new InventarioViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new CajaTurnoViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new DashboardViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new PersonalRolesViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new ComprasProveedoresViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient(sp => new ContabilidadDianViewModel(
            sp.GetRequiredService<IServiceScopeFactory>()));
        servicios.AddTransient<ShellViewModel>();
        servicios.AddTransient<MainViewModel>();
    }
}