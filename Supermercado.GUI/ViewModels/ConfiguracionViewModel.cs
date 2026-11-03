using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.GUI.Servicios.Legal;
using Supermercado.GUI.Servicios.Preferencias;
using Supermercado.GUI.Servicios.Tema;

namespace Supermercado.GUI.ViewModels;

/// <summary>
/// Apariencia, legal y acerca de. Visible para todos los roles; no expone configuración de negocio.
/// </summary>
public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly IServicioPreferencias? _preferencias;

    public event Action? CerrarSolicitado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsClaro))]
    [NotifyPropertyChangedFor(nameof(EsOscuro))]
    [NotifyPropertyChangedFor(nameof(EsAltoContraste))]
    public partial string TemaActual { get; set; } = TemaSigs.ClaveClaro;

    public bool EsClaro => TemaActual == TemaSigs.ClaveClaro;
    public bool EsOscuro => TemaActual == TemaSigs.ClaveOscuro;
    public bool EsAltoContraste => TemaActual == TemaSigs.ClaveAltoContraste;

    public string TextoPrivacidad { get; }
    public string TextoTerminos { get; }

    public string Version { get; } =
        typeof(ConfiguracionViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public ConfiguracionViewModel()
        : this(null, null)
    {
    }

    public ConfiguracionViewModel(IServicioPreferencias? preferencias, IServicioTextosLegales? legales)
    {
        _preferencias = preferencias;
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
    private void Cerrar() => CerrarSolicitado?.Invoke();
}
