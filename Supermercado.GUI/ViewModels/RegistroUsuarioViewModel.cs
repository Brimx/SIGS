using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.Entidades.Clientes;
using Supermercado.Logica.Clientes;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.DatosDemo;
using Supermercado.Logica.Seguridad;
using Supermercado.GUI.Servicios.Validacion;

namespace Supermercado.GUI.ViewModels;

public enum ModoRegistroUsuario
{
    OnboardingAdmin,
    ClientePos,
    EmpleadoPersonal
}

public partial class RegistroUsuarioViewModel : ViewModelBase
{
    private readonly IServicioInicializacion? _inicializacion;
    private readonly IServicioClientes? _clientes;
    private readonly IGeneradorDatosDemo? _datosDemo;
    private readonly ITraductorExcepciones? _traductor;

    [ObservableProperty]
    public partial ModoRegistroUsuario Modo { get; set; } = ModoRegistroUsuario.ClientePos;

    [ObservableProperty]
    public partial string TituloFormulario { get; set; } = "Registro de Cliente (POS / Fidelización)";

    [ObservableProperty]
    public partial string SubtituloFormulario { get; set; } = "Datos para facturación electrónica DIAN y acumulación de puntos";

    [ObservableProperty]
    public partial string TipoDocumento { get; set; } = "CC";

    [ObservableProperty]
    public partial string Documento { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DigitoVerificacion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool EsNit { get; set; }

    [ObservableProperty]
    public partial string Nombres { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Apellidos { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Telefono { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Correo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NombreUsuarioAdmin { get; set; } = "admin";

    [ObservableProperty]
    public partial string ClaveInicial { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmarClave { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CargarDatosDemo { get; set; } = true;

    [ObservableProperty]
    public partial bool AutorizaHabeasData { get; set; } = true;

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial bool GuardadoExitoso { get; set; }

    [ObservableProperty]
    public partial bool Ocupado { get; set; }

    public IReadOnlyList<string> TiposDocumentoDisponibles { get; } = ["CC", "NIT", "CE", "PP"];

    public string TextoLegalHabeasData => ValidadorDian.TextoLegalHabeasData;

    public string AvisoHabeasData => AutorizaHabeasData
        ? "Facturación nominativa habilitada y acumulación de puntos activa."
        : "Sin autorización Ley 1581: Se facturará bajo CONSUMIDOR FINAL (222222222222).";

    public event Action<Cliente?>? ClienteRegistrado;
    public event Action? OnboardingCompletado;
    public event Action? Cancelado;

    public RegistroUsuarioViewModel()
    {
    }

    public RegistroUsuarioViewModel(
        IServicioInicializacion? inicializacion,
        IServicioClientes? clientes,
        IGeneradorDatosDemo? datosDemo = null,
        ITraductorExcepciones? traductor = null)
    {
        _inicializacion = inicializacion;
        _clientes = clientes;
        _datosDemo = datosDemo;
        _traductor = traductor;
    }

    public void ConfigurarModo(ModoRegistroUsuario modo)
    {
        Modo = modo;
        TieneError = false;
        GuardadoExitoso = false;
        MensajeEstado = string.Empty;

        switch (modo)
        {
            case ModoRegistroUsuario.OnboardingAdmin:
                TituloFormulario = "Configuración Inicial de SIGS";
                SubtituloFormulario = "Crea la cuenta del Administrador principal o carga la semilla de demostración";
                break;
            case ModoRegistroUsuario.ClientePos:
                TituloFormulario = "Registro de Cliente y Habeas Data";
                SubtituloFormulario = "Facturación Electrónica DIAN y Programa de Puntos (Ley 1581 de 2012)";
                break;
            case ModoRegistroUsuario.EmpleadoPersonal:
                TituloFormulario = "Registro de Personal Operativo";
                SubtituloFormulario = "Alta de cajeros y supervisores con control de acceso basado en roles";
                break;
        }
    }

    partial void OnTipoDocumentoChanged(string value)
    {
        EsNit = string.Equals(value, "NIT", StringComparison.OrdinalIgnoreCase);
        ActualizarDigitoVerificacion();
    }

    partial void OnDocumentoChanged(string value)
    {
        ActualizarDigitoVerificacion();
    }

    partial void OnAutorizaHabeasDataChanged(bool value)
    {
        OnPropertyChanged(nameof(AvisoHabeasData));
    }

    private void ActualizarDigitoVerificacion()
    {
        if (!EsNit || string.IsNullOrWhiteSpace(Documento))
        {
            DigitoVerificacion = string.Empty;
            return;
        }

        var dv = ValidadorDian.CalcularDigitoVerificacion(Documento);
        DigitoVerificacion = dv >= 0 ? dv.ToString() : string.Empty;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Ocupado)
            return;

        TieneError = false;
        GuardadoExitoso = false;
        MensajeEstado = string.Empty;
        Ocupado = true;

        try
        {
            if (Modo == ModoRegistroUsuario.OnboardingAdmin)
            {
                await EjecutarOnboardingAsync();
            }
            else if (Modo == ModoRegistroUsuario.ClientePos)
            {
                await EjecutarRegistroClienteAsync();
            }
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = _traductor?.Traducir(ex) ?? ex.Message;
        }
        finally
        {
            Ocupado = false;
        }
    }

    private async Task EjecutarOnboardingAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaveInicial) || ClaveInicial.Length < 6)
        {
            TieneError = true;
            MensajeEstado = "La clave inicial del administrador debe tener al menos 6 caracteres.";
            return;
        }

        if (!string.Equals(ClaveInicial, ConfirmarClave, StringComparison.Ordinal))
        {
            TieneError = true;
            MensajeEstado = "Las contraseñas no coinciden.";
            return;
        }

        if (_inicializacion is not null)
        {
            await _inicializacion.InicializarAsync(ClaveInicial);
        }

        if (CargarDatosDemo && _datosDemo is not null)
        {
            await _datosDemo.GenerarAsync();
        }

        GuardadoExitoso = true;
        MensajeEstado = "Sistema inicializado correctamente.";
        OnboardingCompletado?.Invoke();
    }

    private async Task EjecutarRegistroClienteAsync()
    {
        if (string.IsNullOrWhiteSpace(Documento))
        {
            TieneError = true;
            MensajeEstado = "El número de documento es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nombres))
        {
            TieneError = true;
            MensajeEstado = "El nombre o razón social es obligatorio.";
            return;
        }

        var docFinal = EsNit && !string.IsNullOrWhiteSpace(DigitoVerificacion) && !Documento.Contains('-')
            ? $"{Documento.Trim()}-{DigitoVerificacion}"
            : Documento.Trim();

        if (_clientes is not null)
        {
            var cliente = await _clientes.CrearAsync(
                docFinal,
                Nombres.Trim(),
                Apellidos.Trim(),
                Telefono.Trim(),
                Correo.Trim(),
                AutorizaHabeasData);

            GuardadoExitoso = true;
            MensajeEstado = AutorizaHabeasData
                ? $"Cliente {cliente.NombreCompleto} registrado con consentimiento Ley 1581."
                : $"Cliente registrado. Por falta de consentimiento Ley 1581 se facturará como Consumidor Final.";
            ClienteRegistrado?.Invoke(cliente);
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        Cancelado?.Invoke();
    }
}
