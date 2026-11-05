using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Dashboards;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Dashboards;

namespace Supermercado.GUI.ViewModels;

/// <summary>Un punto de dato con su barra proporcional.</summary>
public sealed class FilaDatoViewModel
{
    public string Etiqueta { get; init; } = string.Empty;
    public string Valor { get; init; } = string.Empty;

    /// <summary>Ancho relativo de la barra: 0 a 1 frente al punto mayor.</summary>
    public double Fraccion { get; init; }
}

public sealed partial class WidgetCardViewModel : ObservableObject
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public string Id { get; init; } = string.Empty;
    public string TituloOriginal { get; init; } = string.Empty;
    public string Subtitulo { get; init; } = string.Empty;
    public string ValorPrincipal { get; init; } = string.Empty;
    public IReadOnlyList<FilaDatoViewModel> Filas { get; init; } = [];

    /// <summary>Cadencia declarada por el widget; el panel refresca al mínimo.</summary>
    public TimeSpan Frecuencia { get; init; } = TimeSpan.FromMinutes(5);

    [ObservableProperty]
    public partial string Titulo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TituloEditable { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Ancho))]
    [NotifyPropertyChangedFor(nameof(Alto))]
    [NotifyPropertyChangedFor(nameof(EsGrande))]
    [NotifyPropertyChangedFor(nameof(EtiquetaTamano))]
    public partial TamanoWidget Tamano { get; set; } = TamanoWidget.Mediano;

    public bool TieneVariasFilas => Filas.Count > 0;

    public bool EsGrande => Tamano == TamanoWidget.Grande;

    public string EtiquetaTamano => EtiquetaDe(Tamano);

    public double Ancho => Tamano switch
    {
        TamanoWidget.Pequeno => 240,
        TamanoWidget.Grande => 660,
        _ => 320
    };

    public double Alto => Tamano switch
    {
        TamanoWidget.Pequeno => 160,
        TamanoWidget.Grande => 340,
        _ => 220
    };

    /// <summary>Aplica el título ya guardado: el personalizado o el del widget.</summary>
    public void AplicarTitulo(string titulo)
    {
        Titulo = titulo;
        TituloEditable = titulo == TituloOriginal ? string.Empty : titulo;
    }

    public static string EtiquetaDe(TamanoWidget tamano) => tamano switch
    {
        TamanoWidget.Pequeno => "Pequeño",
        TamanoWidget.Grande => "Grande",
        _ => "Mediano"
    };

    public static WidgetCardViewModel Desde(WidgetEnPanel item, DatoWidget dato)
    {
        var puntos = dato.Puntos;
        var primero = puntos.FirstOrDefault();
        var mayor = puntos.Count == 0
            ? 0
            : puntos.Max(p => Math.Abs(p.Valor));

        var tarjeta = new WidgetCardViewModel
        {
            Id = item.Id,
            TituloOriginal = item.Widget.Titulo,
            Frecuencia = item.Widget.FrecuenciaActualizacion,
            Subtitulo = puntos.Count > 1
                ? $"{puntos.Count} series"
                : primero?.Etiqueta ?? "Sin datos disponibles",
            ValorPrincipal = puntos.Count > 1 ? string.Empty : Formatear(primero),
            Filas = puntos.Count > 1
                ? puntos.Select(p => new FilaDatoViewModel
                {
                    Etiqueta = p.Etiqueta,
                    Valor = Formatear(p),
                    Fraccion = mayor == 0 ? 0 : (double)Math.Abs(p.Valor) / mayor
                }).ToList()
                : [],
            Tamano = item.Tamano
        };

        tarjeta.AplicarTitulo(item.Titulo);
        return tarjeta;
    }

    private static string Formatear(PuntoDato? punto) => punto is null
        ? "Sin datos"
        : punto.Formato switch
        {
            FormatoDato.Moneda => $"$ {punto.Valor.ToString("N0", CulturaCo)}",
            FormatoDato.MonedaEnCentavos =>
                $"$ {(punto.Valor / 100m).ToString("N2", CulturaCo)}",
            _ => punto.Valor.ToString("N0", CulturaCo)
        };
}

public partial class WidgetCatalogoViewModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisponibleParaAgregar))]
    public partial bool EnPanel { get; set; }

    public bool DisponibleParaAgregar => !EnPanel;
}

public sealed class GrupoCatalogoViewModel(string nombre)
{
    public string Nombre { get; } = nombre;
    public ObservableCollection<WidgetCatalogoViewModel> Widgets { get; } = [];
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IServicioDashboard? _servicioDashboard;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    private DispatcherTimer? _refresco;

    public ObservableCollection<WidgetCardViewModel> Tarjetas { get; } = [];
    public ObservableCollection<GrupoCatalogoViewModel> Catalogo { get; } = [];

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Indicadores operativos en tiempo real.";

    [ObservableProperty]
    public partial bool EnEdicion { get; set; }

    [ObservableProperty]
    public partial bool SinWidgets { get; set; }

    [ObservableProperty]
    public partial string UltimaRutaExportada { get; set; } = string.Empty;

    public string EtiquetaModoEdicion => EnEdicion ? "Listo" : "Personalizar";

    public DashboardViewModel()
    {
    }

    public DashboardViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

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
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicio = _servicioDashboard ?? servicios?.GetService<IServicioDashboard>();
        if (servicio is null)
            return;

        await CargarPanelAsync(servicio, servicios);
    }

    [RelayCommand]
    public void AlternarEdicion()
    {
        EnEdicion = !EnEdicion;
        MensajeEstado = EnEdicion
            ? "Modo personalización: agrega, quita, reordena, renombra o cambia el tamaño."
            : $"Panel guardado ({Tarjetas.Count} widgets visibles).";
    }

    [RelayCommand]
    public async Task AgregarWidgetAsync(WidgetCatalogoViewModel? item)
    {
        if (item is null)
            return;

        await EjecutarAsync(
            servicio => servicio.AgregarAsync(item.Id),
            $"«{item.Titulo}» se agregó al panel.");
    }

    [RelayCommand]
    public async Task QuitarWidgetAsync(WidgetCardViewModel? tarjeta)
    {
        if (tarjeta is null)
            return;

        await EjecutarAsync(
            servicio => servicio.QuitarAsync(tarjeta.Id),
            $"«{tarjeta.Titulo}» se quitó del panel.");
    }

    [RelayCommand]
    public async Task QuitarPorIdAsync(string? widgetId)
    {
        if (string.IsNullOrWhiteSpace(widgetId))
            return;

        await EjecutarAsync(
            servicio => servicio.QuitarAsync(widgetId),
            "El widget se quitó del panel.");
    }

    [RelayCommand]
    public Task MoverIzquierdaAsync(WidgetCardViewModel? tarjeta)
        => MoverAsync(tarjeta, -1);

    [RelayCommand]
    public Task MoverDerechaAsync(WidgetCardViewModel? tarjeta)
        => MoverAsync(tarjeta, 1);

    [RelayCommand]
    public async Task CambiarTamanoAsync(WidgetCardViewModel? tarjeta)
    {
        if (tarjeta is null)
            return;

        var siguiente = tarjeta.Tamano switch
        {
            TamanoWidget.Pequeno => TamanoWidget.Mediano,
            TamanoWidget.Mediano => TamanoWidget.Grande,
            _ => TamanoWidget.Pequeno
        };

        await EjecutarAsync(
            servicio => servicio.CambiarTamanoAsync(tarjeta.Id, siguiente),
            $"«{tarjeta.Titulo}» ahora es {WidgetCardViewModel.EtiquetaDe(siguiente)}.",
            alExito: () => tarjeta.Tamano = siguiente,
            recargarPanel: false);
    }

    [RelayCommand]
    public async Task RenombrarAsync(WidgetCardViewModel? tarjeta)
    {
        if (tarjeta is null)
            return;

        await EjecutarAsync(
            servicio => servicio.RenombrarAsync(tarjeta.Id, tarjeta.TituloEditable),
            $"Título de «{tarjeta.TituloOriginal}» actualizado.",
            alExito: () => tarjeta.AplicarTitulo(
                string.IsNullOrWhiteSpace(tarjeta.TituloEditable)
                    ? tarjeta.TituloOriginal
                    : tarjeta.TituloEditable.Trim()),
            recargarPanel: false);
    }

    [RelayCommand]
    public async Task RestablecerAsync()
    {
        await EjecutarAsync(
            servicio => servicio.RestablecerAsync(),
            "El panel volvió a la disposición por defecto de tu rol.");
    }

    /// <summary>
    /// Coloca el widget arrastrado justo antes del destino y guarda el orden.
    /// </summary>
    public async Task MoverAntesAsync(string origenId, string destinoId)
    {
        if (string.IsNullOrWhiteSpace(origenId) ||
            string.IsNullOrWhiteSpace(destinoId) ||
            origenId == destinoId)
            return;

        var ids = Tarjetas.Select(t => t.Id).ToList();
        var desde = ids.IndexOf(origenId);
        var hacia = ids.IndexOf(destinoId);

        if (desde < 0 || hacia < 0)
            return;

        ids.RemoveAt(desde);
        ids.Insert(hacia, origenId);

        await EjecutarAsync(
            servicio => servicio.ReordenarAsync(ids),
            "El panel se reordenó.");
    }

    /// <summary>
    /// Refresca el panel con la cadencia más corta que declaren los widgets.
    /// La vista lo arranca al mostrarse y lo detiene al ocultarse.
    /// </summary>
    public void IniciarRefrescoAutomatico()
    {
        DetenerRefrescoAutomatico();

        _refresco = new DispatcherTimer { Interval = FrecuenciaMinima() };
        _refresco.Tick += AlCumplirIntervalo;
        _refresco.Start();
    }

    public void DetenerRefrescoAutomatico()
    {
        if (_refresco is null)
            return;

        _refresco.Stop();
        _refresco.Tick -= AlCumplirIntervalo;
        _refresco = null;
    }

    private async void AlCumplirIntervalo(object? sender, EventArgs e)
    {
        if (_refresco is not null)
            _refresco.Interval = FrecuenciaMinima();

        // Recargar en plena edición reemplazaría las tarjetas y perdería el
        // título que el usuario está escribiendo.
        if (EnEdicion || Cargando)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicio = _servicioDashboard ?? servicios?.GetService<IServicioDashboard>();
        if (servicio is null)
            return;

        await CargarPanelAsync(servicio, servicios, silencioso: true);
    }

    private TimeSpan FrecuenciaMinima()
    {
        var frecuencias = Tarjetas
            .Select(t => t.Frecuencia)
            .Where(f => f > TimeSpan.Zero)
            .ToList();

        if (frecuencias.Count == 0)
            return TimeSpan.FromMinutes(5);

        var minima = frecuencias.Min();
        return minima < TimeSpan.FromSeconds(15)
            ? TimeSpan.FromSeconds(15)
            : minima;
    }

    /// <summary>Exporta la disposición a la ruta que eligió el usuario.</summary>
    public async Task ExportarAsync(string ruta)
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicio = _servicioDashboard ?? servicios?.GetService<IServicioDashboard>();
        if (servicio is null)
            return;

        try
        {
            var json = await servicio.ExportarAsync();
            await File.WriteAllTextAsync(ruta, json, Encoding.UTF8);

            UltimaRutaExportada = ruta;
            MensajeEstado = $"Disposición exportada en {ruta}.";
        }
        catch (Exception ex)
        {
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    /// <summary>Importa una disposición exportada y recarga el panel.</summary>
    public async Task ImportarAsync(string ruta)
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicio = _servicioDashboard ?? servicios?.GetService<IServicioDashboard>();
        if (servicio is null)
            return;

        try
        {
            var json = await File.ReadAllTextAsync(ruta, Encoding.UTF8);
            var resultado = await servicio.ImportarAsync(json);

            await CargarPanelAsync(servicio, servicios);

            var omitidos = resultado.Omitidos.Count == 0
                ? string.Empty
                : $" Se omitieron {resultado.Omitidos.Count} widgets no disponibles: {string.Join(", ", resultado.Omitidos)}.";

            MensajeEstado = $"Disposición importada ({resultado.Aplicados} widgets).{omitidos}";
        }
        catch (Exception ex)
        {
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private async Task CargarPanelAsync(
        IServicioDashboard servicio,
        IServiceProvider? servicios,
        bool silencioso = false)
    {
        if (Cargando)
            return;

        Cargando = true;
        try
        {
            var panel = await servicio.ObtenerPanelAsync();
            var nuevas = new List<WidgetCardViewModel>();

            foreach (var item in panel)
            {
                try
                {
                    nuevas.Add(WidgetCardViewModel.Desde(item, await item.Widget.ObtenerDatosAsync()));
                }
                catch
                {
                    // Si un widget falla, no interrumpe los demás.
                }
            }

            Tarjetas.Clear();
            foreach (var tarjeta in nuevas)
                Tarjetas.Add(tarjeta);

            SinWidgets = Tarjetas.Count == 0;
            CargarCatalogo(servicio);

            if (silencioso)
                return;

            MensajeEstado = Tarjetas.Count == 0
                ? "No hay widgets visibles. Entra en «Personalizar» para agregarlos."
                : $"Panel actualizado ({Tarjetas.Count} widgets activos).";
        }
        catch (Exception ex)
        {
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            Cargando = false;
        }
    }

    private void CargarCatalogo(IServicioDashboard servicio)
    {
        var visibles = Tarjetas.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

        Catalogo.Clear();
        foreach (var grupo in servicio.ObtenerDisponibles()
            .GroupBy(w => w.Grupo)
            .OrderBy(g => OrdenDeGrupo(g.Key)))
        {
            var nodo = new GrupoCatalogoViewModel(grupo.Key);
            foreach (var widget in grupo.OrderBy(w => w.Titulo, StringComparer.CurrentCulture))
                nodo.Widgets.Add(new WidgetCatalogoViewModel
                {
                    Id = widget.Id,
                    Titulo = widget.Titulo,
                    Grupo = widget.Grupo,
                    EnPanel = visibles.Contains(widget.Id)
                });

            Catalogo.Add(nodo);
        }
    }

    private async Task MoverAsync(WidgetCardViewModel? tarjeta, int desplazamiento)
    {
        if (tarjeta is null)
            return;

        var ids = Tarjetas.Select(t => t.Id).ToList();
        var indice = ids.IndexOf(tarjeta.Id);
        var destino = indice + desplazamiento;

        if (indice < 0 || destino < 0 || destino >= ids.Count)
            return;

        (ids[indice], ids[destino]) = (ids[destino], ids[indice]);

        await EjecutarAsync(
            servicio => servicio.ReordenarAsync(ids),
            $"«{tarjeta.Titulo}» se movió a la posición {destino + 1}.");
    }

    private async Task EjecutarAsync(
        Func<IServicioDashboard, Task> accion,
        string mensajeExito,
        Action? alExito = null,
        bool recargarPanel = true)
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicio = _servicioDashboard ?? servicios?.GetService<IServicioDashboard>();
        if (servicio is null)
            return;

        try
        {
            await accion(servicio);
            alExito?.Invoke();

            if (recargarPanel)
                await CargarPanelAsync(servicio, servicios);

            MensajeEstado = mensajeExito;
        }
        catch (Exception ex)
        {
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private string Traducir(IServiceProvider? servicios, Exception ex)
        => _traductor?.Traducir(ex)
           ?? servicios?.GetService<ITraductorExcepciones>()?.Traducir(ex)
           ?? ex.Message;

    private static int OrdenDeGrupo(string grupo)
    {
        var indice = GruposWidget.Orden
            .ToList()
            .FindIndex(g => string.Equals(g, grupo, StringComparison.OrdinalIgnoreCase));
        return indice < 0 ? int.MaxValue : indice;
    }
}
