using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.GUI.Servicios.Legal;
using Supermercado.GUI.Servicios.Preferencias;
using Supermercado.GUI.Servicios.Tema;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.GUI.ViewModels;

/// <summary>
/// Apariencia, legal, acerca de y vinculación Telegram del usuario autenticado.
/// </summary>
public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IServicioPreferencias? _preferencias;
    private readonly ISesionActual? _sesion;
    private readonly IVinculosTelegram? _vinculos;
    private readonly TimeProvider? _reloj;

    public event Action? CerrarSolicitado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsClaro))]
    [NotifyPropertyChangedFor(nameof(EsOscuro))]
    [NotifyPropertyChangedFor(nameof(EsAltoContraste))]
    public partial string TemaActual { get; set; } = TemaSigs.ClaveClaro;

    [ObservableProperty]
    public partial string InstruccionVinculacion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MensajeVinculacion { get; set; } =
        "Vincula solo tu chat privado de Telegram. El código dura 10 minutos y se usa una vez.";

    public bool EsClaro => TemaActual == TemaSigs.ClaveClaro;
    public bool EsOscuro => TemaActual == TemaSigs.ClaveOscuro;
    public bool EsAltoContraste => TemaActual == TemaSigs.ClaveAltoContraste;
    public string TextoPrivacidad { get; }
    public string TextoTerminos { get; }
    public string Version { get; } =
        typeof(ConfiguracionViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public ConfiguracionViewModel()
        : this(null, null, null, null, null)
    {
    }

    public ConfiguracionViewModel(
        IServicioPreferencias? preferencias,
        IServicioTextosLegales? legales)
        : this(preferencias, legales, null, null, null)
    {
    }

    public ConfiguracionViewModel(
        IServicioPreferencias? preferencias,
        IServicioTextosLegales? legales,
        ISesionActual? sesion,
        IVinculosTelegram? vinculos,
        TimeProvider? reloj)
    {
        _preferencias = preferencias;
        _sesion = sesion;
        _vinculos = vinculos;
        _reloj = reloj;
        TemaActual = preferencias?.LeerTema()
            ?? TemaSigs.ClaveDe(Avalonia.Application.Current?.RequestedThemeVariant ?? Avalonia.Styling.ThemeVariant.Light);
        TextoPrivacidad = legales?.PoliticaPrivacidad() ?? "Texto no disponible.";
        TextoTerminos = legales?.TerminosServicio() ?? "Texto no disponible.";
    }

    [RelayCommand]
    private void SeleccionarTema(string? clave)
    {
        if (clave is null || !TemaSigs.Claves.Contains(clave))
            return;
        TemaActual = clave;
        TemaSigs.Aplicar(clave);
        _preferencias?.GuardarTema(clave);
    }

    [RelayCommand]
    private void GenerarCodigoVinculacion()
    {
        var usuario = _sesion?.Usuario;
        if (usuario?.Empleado?.Activo != true || _vinculos is null)
        {
            MensajeVinculacion = "Inicia sesión con un usuario activo para generar el código.";
            return;
        }

        InstruccionVinculacion = $"/start {_vinculos.GenerarCodigo(usuario.EmpleadoId, usuario.NombreUsuario)}";
        var vence = (_reloj ?? TimeProvider.System).GetLocalNow().AddMinutes(10);
        MensajeVinculacion = $"Dura 10 minutos. Envíalo al bot antes de las {vence:HH:mm}; se consume una sola vez.";
    }

    [RelayCommand]
    private void Cerrar() => CerrarSolicitado?.Invoke();
}