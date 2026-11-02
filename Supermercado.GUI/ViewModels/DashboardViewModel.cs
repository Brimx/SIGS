using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Dashboards;

namespace Supermercado.GUI.ViewModels;

public sealed class WidgetCardViewModel
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public string Id { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string ValorPrincipal { get; init; } = "$ 0";
    public string Subtitulo { get; init; } = string.Empty;
    public IReadOnlyList<PuntoDato> Puntos { get; init; } = [];

    public static WidgetCardViewModel DesdeWidget(IWidget widget, DatoWidget dato)
    {
        var primero = dato.Puntos.FirstOrDefault();
        var esMoneda = widget.Id.Contains("venta", StringComparison.OrdinalIgnoreCase) ||
                       widget.Id.Contains("iva", StringComparison.OrdinalIgnoreCase) ||
                       widget.Id.Contains("arqueo", StringComparison.OrdinalIgnoreCase) ||
                       widget.Id.Contains("trm", StringComparison.OrdinalIgnoreCase);

        var valorFmt = primero is null
            ? "0"
            : esMoneda
                ? $"$ {primero.Valor.ToString("N0", CulturaCo)}"
                : primero.Valor.ToString("N0", CulturaCo);

        var sub = primero?.Etiqueta ?? "Actualizado en tiempo real";
        return new WidgetCardViewModel
        {
            Id = widget.Id,
            Titulo = widget.Titulo,
            ValorPrincipal = valorFmt,
            Subtitulo = sub,
            Puntos = dato.Puntos
        };
    }
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IServicioDashboard? _servicioDashboard;
    private readonly ITraductorExcepciones? _traductor;

    public ObservableCollection<WidgetCardViewModel> Tarjetas { get; } = [];

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Indicadores operativos en tiempo real.";

    public DashboardViewModel()
    {
    }

    public DashboardViewModel(
        IServicioDashboard servicioDashboard,
        ITraductorExcepciones? traductor = null)
    {
        _servicioDashboard = servicioDashboard;
        _traductor = traductor;
    }

    [RelayCommand]
    public async Task ActualizarDashboardAsync()
    {
        if (_servicioDashboard is null || Cargando)
            return;

        Cargando = true;
        try
        {
            var widgets = await _servicioDashboard.ObtenerDisposicionAsync();
            if (widgets.Count == 0)
            {
                widgets = _servicioDashboard.ObtenerDisponibles();
            }

            var nuevas = new List<WidgetCardViewModel>();
            foreach (var w in widgets)
            {
                try
                {
                    var datos = await w.ObtenerDatosAsync();
                    nuevas.Add(WidgetCardViewModel.DesdeWidget(w, datos));
                }
                catch
                {
                    // Si un widget requiere permiso superior o falla, no interrumpe los demás
                }
            }

            Tarjetas.Clear();
            foreach (var card in nuevas)
            {
                Tarjetas.Add(card);
            }

            MensajeEstado = $"Panel actualizado ({Tarjetas.Count} widgets activos).";
        }
        catch (Exception ex)
        {
            MensajeEstado = _traductor?.Traducir(ex) ?? ex.Message;
        }
        finally
        {
            Cargando = false;
        }
    }
}
