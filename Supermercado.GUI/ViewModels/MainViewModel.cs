using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Supermercado.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public LoginViewModel LoginVm { get; }
    public ShellViewModel ShellVm { get; }
    public ConfiguracionViewModel ConfiguracionVm { get; }

    [ObservableProperty]
    public partial ViewModelBase VistaActual { get; set; }

    [ObservableProperty]
    public partial bool MostrarConfiguracion { get; set; }

    public MainViewModel()
    {
        LoginVm = new LoginViewModel();
        ShellVm = new ShellViewModel();
        ConfiguracionVm = new ConfiguracionViewModel();
        VistaActual = LoginVm;
        Conectar();
    }

    public MainViewModel(LoginViewModel loginVm, ShellViewModel shellVm, ConfiguracionViewModel? configuracionVm = null)
    {
        LoginVm = loginVm;
        ShellVm = shellVm;
        ConfiguracionVm = configuracionVm ?? new ConfiguracionViewModel();
        VistaActual = LoginVm;
        Conectar();
    }

    private void Conectar()
    {
        LoginVm.AutenticacionExitosa += async () =>
        {
            await ShellVm.InicializarSesionAsync();
            VistaActual = ShellVm;
        };

        ShellVm.SesionCerrada += () => VistaActual = LoginVm;

        LoginVm.ConfiguracionSolicitada += AbrirConfiguracion;
        ShellVm.ConfiguracionSolicitada += AbrirConfiguracion;
        ConfiguracionVm.CerrarSolicitado += CerrarConfiguracion;
    }

    [RelayCommand]
    private void AbrirConfiguracion() => MostrarConfiguracion = true;

    [RelayCommand]
    private void CerrarConfiguracion() => MostrarConfiguracion = false;
}
