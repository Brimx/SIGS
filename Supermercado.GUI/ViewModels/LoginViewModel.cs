using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.DatosDemo;
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
    public partial bool MostrarPanelDemo { get; set; }

    [ObservableProperty]
    public partial string MensajeError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial string MensajeExito { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneExito { get; set; }

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
    private void AgregarDigitoPin(string? digito)
    {
        if (string.IsNullOrEmpty(digito) || PinAcceso.Length >= 8)
            return;

        PinAcceso += digito;
        TieneError = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private void BorrarDigitoPin()
    {
        if (PinAcceso.Length > 0)
        {
            PinAcceso = PinAcceso[..^1];
        }
    }

    [RelayCommand]
    private void LimpiarPin()
    {
        PinAcceso = string.Empty;
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
            MensajeError = "Ingresa tu nombre de usuario o código de operador.";
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
    private void AlternarPanelDemo()
    {
        MostrarPanelDemo = !MostrarPanelDemo;
    }

    [RelayCommand]
    private void SeleccionarCredencialDemo(string? rol)
    {
        TieneError = false;
        MensajeError = string.Empty;

        switch (rol?.ToLowerInvariant())
        {
            case "admin":
                NombreUsuario = "admin";
                Clave = "Admin123*";
                PinAcceso = "1111";
                break;
            case "cajero":
                NombreUsuario = "cajero";
                Clave = "Cajero123*";
                PinAcceso = "1234";
                break;
            case "supervisor":
                NombreUsuario = "supervisor";
                Clave = "Super123*";
                PinAcceso = "9988";
                break;
            case "gerente":
                NombreUsuario = "gerente";
                Clave = "Gerente123*";
                PinAcceso = "3344";
                break;
            case "bodega":
                NombreUsuario = "bodega";
                Clave = "Bodega123*";
                PinAcceso = "2233";
                break;
            case "compras":
                NombreUsuario = "compras";
                Clave = "Compras123*";
                PinAcceso = "4455";
                break;
            case "contador":
                NombreUsuario = "contador";
                Clave = "Conta123*";
                PinAcceso = "5566";
                break;
            case "soporte":
                NombreUsuario = "soporte";
                Clave = "Soporte123*";
                PinAcceso = "6677";
                break;
            case "atencion":
                NombreUsuario = "atencion";
                Clave = "Atencion123*";
                PinAcceso = "7788";
                break;
            case "analista":
                NombreUsuario = "analista";
                Clave = "Analista123*";
                PinAcceso = "8899";
                break;
            default:
                NombreUsuario = "cajero";
                Clave = "Cajero123*";
                PinAcceso = "1234";
                break;
        }

        TieneExito = true;
        MensajeExito = $"Credencial cargada para '{NombreUsuario}' (Clave: {Clave} · PIN: {PinAcceso}).";
    }

    [RelayCommand]
    private async Task SembrarDatosDemoAsync()
    {
        if (Cargando || _scopeFactory is null)
            return;

        Cargando = true;
        TieneError = false;
        TieneExito = false;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var generador = scope.ServiceProvider.GetRequiredService<IGeneradorDatosDemo>();
            await generador.GenerarAsync();

            TieneExito = true;
            MensajeExito = "Base de datos demo sembrada con 10 roles, catálogo, productos, clientes y turno POS.";
            SeleccionarCredencialDemo("cajero");
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeError = $"No se pudo sembrar datos demo: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void AbrirOnboarding()
    {
        MostrarOnboarding = true;
    }
}