using CommunityToolkit.Mvvm.ComponentModel;

namespace Supermercado.GUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public LoginViewModel LoginVm { get; }
    public ShellViewModel ShellVm { get; }

    [ObservableProperty]
    public partial ViewModelBase VistaActual { get; set; }

    public MainViewModel()
    {
        LoginVm = new LoginViewModel();
        ShellVm = new ShellViewModel();
        VistaActual = LoginVm;
    }

    public MainViewModel(LoginViewModel loginVm, ShellViewModel shellVm)
    {
        LoginVm = loginVm;
        ShellVm = shellVm;
        VistaActual = LoginVm;

        LoginVm.AutenticacionExitosa += async () =>
        {
            await ShellVm.InicializarSesionAsync();
            VistaActual = ShellVm;
        };

        ShellVm.SesionCerrada += () =>
        {
            VistaActual = LoginVm;
        };
    }
}
