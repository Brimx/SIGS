using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Inventario;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;

namespace Supermercado.GUI.ViewModels;

public sealed class ProductoInventarioItemViewModel
{
    public int ProductoId { get; init; }
    public string CodigoBarras { get; init; } = string.Empty;
    public string NombreProducto { get; init; } = string.Empty;
    public int LoteId { get; init; }
    public string NumeroLote { get; init; } = string.Empty;
    public DateOnly FechaVencimiento { get; init; }
    public int CantidadEstante { get; init; }
    public int CantidadBodega { get; init; }
    public int TotalStock { get; init; }
    public bool EnAlertaStockBajo { get; init; }
    public bool ProximoAVencer { get; init; }

    public string EstadoBadge => ProximoAVencer
        ? "PRÓXIMO A VENCER"
        : EnAlertaStockBajo
            ? "STOCK BAJO"
            : "ÓPTIMO";
}

public partial class InventarioViewModel : ViewModelBase
{
    private readonly IServicioConsultaInventario? _consultaInventario;
    private readonly IServicioAlertasInventario? _alertasInventario;
    private readonly IServicioMermas? _servicioMermas;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    private readonly List<ProductoInventarioItemViewModel> _todosLosItems = [];

    public ObservableCollection<ProductoInventarioItemViewModel> ProductosFiltrados { get; } = [];

    [ObservableProperty]
    public partial string FiltroTexto { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool SoloStockBajo { get; set; }

    [ObservableProperty]
    public partial bool SoloProximosAVencer { get; set; }

    [ObservableProperty]
    public partial int TotalRegistros { get; set; }

    [ObservableProperty]
    public partial int AlertasStockBajoCount { get; set; }

    [ObservableProperty]
    public partial int LotesPorVencerCount { get; set; }

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Inventario consolidado FEFO listo.";

    [ObservableProperty]
    public partial ProductoInventarioItemViewModel? ItemSeleccionado { get; set; }

    // Modal de Baja por Merma / Desperdicio
    [ObservableProperty]
    public partial bool ModalMermaAbierto { get; set; }

    [ObservableProperty]
    public partial int CantidadMermaInput { get; set; } = 1;

    [ObservableProperty]
    public partial string MotivoMermaInput { get; set; } = "Deterioro perecedero / Fruver";

    public InventarioViewModel()
    {
    }

    public InventarioViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public InventarioViewModel(
        IServicioConsultaInventario consultaInventario,
        IServicioAlertasInventario? alertasInventario = null,
        IServicioMermas? servicioMermas = null,
        ITraductorExcepciones? traductor = null)
    {
        _consultaInventario = consultaInventario;
        _alertasInventario = alertasInventario;
        _servicioMermas = servicioMermas;
        _traductor = traductor;
    }

    partial void OnFiltroTextoChanged(string value) => AplicarFiltros();
    partial void OnSoloStockBajoChanged(bool value) => AplicarFiltros();
    partial void OnSoloProximosAVencerChanged(bool value) => AplicarFiltros();

    public void CargarDesdeLista(IEnumerable<ProductoInventarioItemViewModel> items)
    {
        _todosLosItems.Clear();
        _todosLosItems.AddRange(items);
        TotalRegistros = _todosLosItems.Count;
        AlertasStockBajoCount = _todosLosItems.Count(x => x.EnAlertaStockBajo);
        LotesPorVencerCount = _todosLosItems.Count(x => x.ProximoAVencer);
        AplicarFiltros();
    }

    [RelayCommand]
    public async Task ActualizarListadoAsync()
    {
        if (Cargando)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var consulta = _consultaInventario
            ?? servicios?.GetService<IServicioConsultaInventario>();
        var alertas = _alertasInventario
            ?? servicios?.GetService<IServicioAlertasInventario>();
        if (consulta is null)
            return;

        Cargando = true;
        try
        {
            var consolidado = await consulta.ObtenerStockConsolidadoAsync();
            var idsBajoMinimo = new HashSet<int>();
            var lotesPorVencerIds = new HashSet<int>();

            if (alertas is not null)
            {
                var bajos = await alertas.ObtenerBajoMinimoAsync();
                foreach (var b in bajos)
                    idsBajoMinimo.Add(b.Producto.Id);

                var porVencer = await alertas.ObtenerLotesPorVencerAsync();
                foreach (var l in porVencer)
                    lotesPorVencerIds.Add(l.Id);
            }

            var mapeados = consolidado.Select(c => new ProductoInventarioItemViewModel
            {
                ProductoId = c.ProductoId,
                CodigoBarras = c.CodigoBarras,
                NombreProducto = c.NombreProducto,
                LoteId = c.LoteId,
                NumeroLote = c.NumeroLote,
                FechaVencimiento = c.FechaVencimiento,
                CantidadEstante = c.CantidadEstante,
                CantidadBodega = c.CantidadBodega,
                TotalStock = c.TotalStock,
                EnAlertaStockBajo = idsBajoMinimo.Contains(c.ProductoId) || c.CantidadEstante <= 10,
                ProximoAVencer = lotesPorVencerIds.Contains(c.LoteId)
            });

            CargarDesdeLista(mapeados);
            MensajeEstado = $"Catálogo actualizado: {_todosLosItems.Count} lotes activos.";
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

    public void AplicarFiltros()
    {
        var query = _todosLosItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var termino = FiltroTexto.Trim();
            query = query.Where(p =>
                p.NombreProducto.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                p.CodigoBarras.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                p.NumeroLote.Contains(termino, StringComparison.OrdinalIgnoreCase));
        }

        if (SoloStockBajo)
        {
            query = query.Where(p => p.EnAlertaStockBajo);
        }

        if (SoloProximosAVencer)
        {
            query = query.Where(p => p.ProximoAVencer);
        }

        ProductosFiltrados.Clear();
        foreach (var item in query)
        {
            ProductosFiltrados.Add(item);
        }
    }

    [RelayCommand]
    private void AbrirModalMerma()
    {
        if (ItemSeleccionado is null)
            return;

        CantidadMermaInput = 1;
        ModalMermaAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalMerma()
    {
        ModalMermaAbierto = false;
    }

    [RelayCommand]
    public async Task ConfirmarMermaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioMermas = _servicioMermas
            ?? servicios?.GetService<IServicioMermas>();
        if (servicioMermas is null || ItemSeleccionado is null || CantidadMermaInput <= 0)
            return;

        try
        {
            await servicioMermas.RegistrarAsync(
                ItemSeleccionado.ProductoId,
                ItemSeleccionado.LoteId,
                UbicacionStock.Estante,
                CantidadMermaInput,
                MotivoMermaInput);

            ModalMermaAbierto = false;
            MensajeEstado = $"Merma registrada y auditada para {ItemSeleccionado.NombreProducto} (-{CantidadMermaInput}).";
            await ActualizarListadoAsync();
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
}
