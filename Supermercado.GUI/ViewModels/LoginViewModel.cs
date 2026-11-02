using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.DatosDemo;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IServicioAutenticacion? _autenticacion;
    private readonly ISesionActual? _sesion;
    private readonly IServicioInicializacion? _inicializacion;
    private readonly IGeneradorDatosDemo? _datosDemo;
    private readonly ITraductorExcepciones? _traductor;

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

    public LoginViewModel(
        IServicioAutenticacion autenticacion,
        ISesionActual sesion,
        IServicioInicializacion inicializacion,
        IGeneradorDatosDemo datosDemo,
        ITraductorExcepciones traductor)
    {
        _autenticacion = autenticacion;
        _sesion = sesion;
        _inicializacion = inicializacion;
        _datosDemo = datosDemo;
        _traductor = traductor;

        RegistroOnboarding = new RegistroUsuarioViewModel(
            inicializacion,
            clientes: null,
            datosDemo: datosDemo,
            traductor: traductor);
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

        if (_autenticacion is null)
            return;

        Cargando = true;
        try
        {
            if (ModoPin)
            {
                await _autenticacion.IniciarSesionConPinAsync(NombreUsuario.Trim(), PinAcceso.Trim());
            }
            else
            {
                await _autenticacion.IniciarSesionAsync(NombreUsuario.Trim(), Clave);
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
            MensajeError = _traductor?.Traducir(ex) ?? ex.Message;
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task IngresarDemoRapidoAsync(string rolDemo)
    {
        if (Cargando || _inicializacion is null || _datosDemo is null || _autenticacion is null)
            return;

        Cargando = true;
        TieneError = false;
        MensajeError = string.Empty;

        try
        {
            await _inicializacion.InicializarAsync("Admin123*");
            await _datosDemo.GenerarAsync();

            if (string.Equals(rolDemo, "admin", StringComparison.OrdinalIgnoreCase))
            {
                NombreUsuario = "admin_demo";
                await _autenticacion.IniciarSesionAsync("admin_demo", "Admin123*");
            }
            else
            {
                NombreUsuario = "cajero_demo";
                await _autenticacion.IniciarSesionAsync("cajero_demo", "Cajero123*");
            }

            AutenticacionExitosa?.Invoke();
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeError = _traductor?.Traducir(ex) ?? ex.Message;
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
