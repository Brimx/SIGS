using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ISesionActual? _sesion;

    [ObservableProperty]
    public partial string NombreUsuario { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Clave { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PinAcceso { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ModoPin { get; set; }

    [ObservableProperty]
    public partial bool MostrarOnboarding { get; set; }

    [ObservableProperty]
    public partial string MensajeError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    public RegistroUsuarioViewModel RegistroOnboarding { get; }

    public event Action? AutenticacionExitosa;

    public LoginViewModel()
    {
        RegistroOnboarding = new RegistroUsuarioViewModel();
        RegistroOnboarding.ConfigurarModo(ModoRegistroUsuario.OnboardingAdmin);
    }

    public LoginViewModel(IServiceScopeFactory scopeFactory, ISesionActual sesion)
    {
        _scopeFactory = scopeFactory;
        _sesion = sesion;

        RegistroOnboarding = new RegistroUsuarioViewModel(scopeFactory);
        RegistroOnboarding.ConfigurarModo(ModoRegistroUsuario.OnboardingAdmin);
        RegistroOnboarding.OnboardingCompletado += () =>
        {
            MostrarOnboarding = false;
            NombreUsuario = ServicioInicializacion.UsuarioAdministrador;
        };
        RegistroOnboarding.Cancelado += () => MostrarOnboarding = false;
    }

    [RelayCommand]
    private async Task IngresarAsync()
    {
        if (Cargando)
            return;

        TieneError = false;
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreUsuario))
        {
            TieneError = true;
            MensajeError = "Ingresa tu nombre de usuario.";
            return;
        }

        if (!ModoPin && string.IsNullOrWhiteSpace(Clave))
        {
            TieneError = true;
            MensajeError = "Ingresa tu contraseña.";
            return;
        }

        if (ModoPin && string.IsNullOrWhiteSpace(PinAcceso))
        {
            TieneError = true;
            MensajeError = "Ingresa tu PIN de cajero.";
            return;
        }

        if (_scopeFactory is null)
            return;

        Cargando = true;
        using var scope = _scopeFactory.CreateScope();
        try
        {
            var autenticacion = scope.ServiceProvider
                .GetRequiredService<IServicioAutenticacion>();
            if (ModoPin)
            {
                await autenticacion.IniciarSesionConPinAsync(NombreUsuario.Trim(), PinAcceso.Trim());
            }
            else
            {
                await autenticacion.IniciarSesionAsync(NombreUsuario.Trim(), Clave);
            }

            Clave = string.Empty;
            PinAcceso = string.Empty;
            AutenticacionExitosa?.Invoke();
        }
        catch (DebeCambiarClaveException)
        {
            // El usuario quedó autenticado pero con marca de cambio de clave; permitimos paso al Shell.
            if (_sesion?.EstaAutenticada == true)
            {
                Clave = string.Empty;
                AutenticacionExitosa?.Invoke();
            }
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeError = scope.ServiceProvider
                .GetRequiredService<ITraductorExcepciones>().Traducir(ex);
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void AlternarModoPin()
    {
        ModoPin = !ModoPin;
        TieneError = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private void AbrirOnboarding()
    {
        MostrarOnboarding = true;
    }
}
