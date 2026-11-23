using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;

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
    private readonly ServicioInventario? _consultaInventario;
    private readonly ServicioInventario? _alertasInventario;
    private readonly ServicioInventario? _servicioMermas;
    private readonly ServicioProductos? _servicioProductos;
    private readonly SesionActual? _sesion;
    private readonly TraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    private readonly List<ProductoInventarioItemViewModel> _todosLosItems = [];

    public ObservableCollection<ProductoInventarioItemViewModel> ProductosFiltrados { get; } = [];
    public ObservableCollection<Categoria> Categorias { get; } = [];
    public ObservableCollection<Impuesto> Impuestos { get; } = [];
    public ObservableCollection<Producto> Productos { get; } = [];

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

    [ObservableProperty]
    public partial bool PuedeGestionarInventario { get; set; }

    [ObservableProperty]
    public partial bool Guardando { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneErrorModal))]
    public partial string ErrorModal { get; set; } = string.Empty;

    public bool TieneErrorModal => !string.IsNullOrWhiteSpace(ErrorModal);

    [ObservableProperty]
    public partial bool ModalNuevoProductoAbierto { get; set; }

    [ObservableProperty]
    public partial string NuevoCodigoBarrasInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NuevoNombreInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Categoria? CategoriaSeleccionada { get; set; }

    [ObservableProperty]
    public partial long NuevoPrecioInput { get; set; }

    [ObservableProperty]
    public partial Impuesto? IvaSeleccionado { get; set; }

    [ObservableProperty]
    public partial int NuevoStockMinimoInput { get; set; }

    [ObservableProperty]
    public partial bool ModalEntradaBodegaAbierto { get; set; }

    [ObservableProperty]
    public partial Producto? ProductoSeleccionado { get; set; }

    [ObservableProperty]
    public partial int CantidadEntradaInput { get; set; } = 1;

    [ObservableProperty]
    public partial string NumeroLoteInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? FechaVencimientoEntradaInput { get; set; }

    public InventarioViewModel()
    {
    }

    public InventarioViewModel(IServiceScopeFactory scopeFactory, SesionActual? sesion = null)
    {
        _scopeFactory = scopeFactory;
        _sesion = sesion;
        ActualizarPermisos();
    }

    public InventarioViewModel(
        ServicioInventario consultaInventario,
        ServicioInventario? alertasInventario = null,
        ServicioInventario? servicioMermas = null,
        TraductorExcepciones? traductor = null,
        ServicioProductos? servicioProductos = null,
        SesionActual? sesion = null)
    {
        _consultaInventario = consultaInventario;
        _alertasInventario = alertasInventario;
        _servicioMermas = servicioMermas;
        _servicioProductos = servicioProductos;
        _sesion = sesion;
        _traductor = traductor;
        ActualizarPermisos();
    }

    public void ActualizarPermisos()
        => PuedeGestionarInventario = _sesion?.Tiene(Permiso.GestionarProductos) ?? false;

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
            ?? servicios?.GetService<ServicioInventario>();
        var alertas = _alertasInventario
            ?? servicios?.GetService<ServicioInventario>();
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
                EnAlertaStockBajo = idsBajoMinimo.Contains(c.ProductoId),
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

    public string CrearCsv()
    {
        static string Escapar(string valor)
            => "\"" + valor.Replace("\"", "\"\"") + "\"";

        var filas = new List<string>
        {
            "Codigo,Producto,Lote,Vencimiento,Estante,Bodega,Total,Estado"
        };
        filas.AddRange(ProductosFiltrados.Select(item => string.Join(",",
            Escapar(item.CodigoBarras),
            Escapar(item.NombreProducto),
            Escapar(item.NumeroLote),
            Escapar(item.FechaVencimiento.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            item.CantidadEstante.ToString(System.Globalization.CultureInfo.InvariantCulture),
            item.CantidadBodega.ToString(System.Globalization.CultureInfo.InvariantCulture),
            item.TotalStock.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Escapar(item.EstadoBadge))));
        return string.Join("\r\n", filas);
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
            ?? servicios?.GetService<ServicioInventario>();
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

    [RelayCommand]
    private async Task AbrirModalNuevoProductoAsync()
    {
        ActualizarPermisos();
        if (!PuedeGestionarInventario)
        {
            MensajeEstado = "No tiene permiso para crear productos.";
            return;
        }

        NuevoCodigoBarrasInput = string.Empty;
        NuevoNombreInput = string.Empty;
        CategoriaSeleccionada = null;
        NuevoPrecioInput = 0;
        IvaSeleccionado = null;
        NuevoStockMinimoInput = 0;
        ErrorModal = string.Empty;
        await CargarDatosFormulariosAsync();
        ModalNuevoProductoAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoProducto()
    {
        ErrorModal = string.Empty;
        ModalNuevoProductoAbierto = false;
    }

    [RelayCommand]
    private async Task ConfirmarNuevoProductoAsync()
    {
        ActualizarPermisos();
        if (!PuedeGestionarInventario)
        {
            ErrorModal = MensajeEstado = "No tiene permiso para crear productos.";
            return;
        }
        if (Guardando)
            return;

        ErrorModal = string.Empty;
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioProductos ?? servicios?.GetService<ServicioProductos>();
        if (catalogos is null)
        {
            ErrorModal = MensajeEstado = "El catálogo de productos no está disponible.";
            return;
        }

        Guardando = true;
        try
        {
            var creado = await catalogos.CrearProductoAsync(
                NuevoCodigoBarrasInput,
                NuevoNombreInput,
                CategoriaSeleccionada?.Id ?? 0,
                NuevoPrecioInput,
                IvaSeleccionado?.Id ?? 0,
                NuevoStockMinimoInput);

            ModalNuevoProductoAbierto = false;
            MensajeEstado = $"Producto {creado.Nombre} creado y auditado.";
            await ActualizarListadoAsync();
        }
        catch (Exception ex)
        {
            ErrorModal = MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            Guardando = false;
        }
    }

    [RelayCommand]
    private async Task AbrirModalEntradaBodegaAsync()
    {
        ActualizarPermisos();
        if (!PuedeGestionarInventario)
        {
            MensajeEstado = "No tiene permiso para registrar entradas a bodega.";
            return;
        }

        ProductoSeleccionado = null;
        CantidadEntradaInput = 1;
        NumeroLoteInput = string.Empty;
        FechaVencimientoEntradaInput = null;
        ErrorModal = string.Empty;
        await CargarDatosFormulariosAsync();
        ModalEntradaBodegaAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalEntradaBodega()
    {
        ErrorModal = string.Empty;
        ModalEntradaBodegaAbierto = false;
    }

    [RelayCommand]
    private async Task ConfirmarEntradaBodegaAsync()
    {
        ActualizarPermisos();
        if (!PuedeGestionarInventario)
        {
            ErrorModal = MensajeEstado = "No tiene permiso para registrar entradas a bodega.";
            return;
        }
        if (Guardando)
            return;
        if (ProductoSeleccionado is null)
        {
            ErrorModal = MensajeEstado = "Seleccione el producto.";
            return;
        }
        if (FechaVencimientoEntradaInput is null)
        {
            ErrorModal = MensajeEstado = "Seleccione la fecha de vencimiento.";
            return;
        }

        ErrorModal = string.Empty;
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioProductos ?? servicios?.GetService<ServicioProductos>();
        if (catalogos is null)
        {
            ErrorModal = MensajeEstado = "El catálogo de productos no está disponible.";
            return;
        }

        Guardando = true;
        try
        {
            var vencimiento = DateOnly.FromDateTime(
                FechaVencimientoEntradaInput.Value.LocalDateTime);

            await catalogos.RegistrarIngresoLoteAsync(
                ProductoSeleccionado.Id,
                NumeroLoteInput,
                vencimiento,
                CantidadEntradaInput,
                costoUnitario: 0);

            ModalEntradaBodegaAbierto = false;
            MensajeEstado = $"Entrada registrada: {CantidadEntradaInput} und del lote {NumeroLoteInput.Trim()} de {ProductoSeleccionado.Nombre}.";
            await ActualizarListadoAsync();
        }
        catch (Exception ex)
        {
            ErrorModal = MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            Guardando = false;
        }
    }

    private async Task CargarDatosFormulariosAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioProductos ?? servicios?.GetService<ServicioProductos>();
        if (catalogos is null)
            return;

        try
        {
            var categorias = await catalogos.ObtenerCategoriasAsync();
            Categorias.Clear();
            foreach (var categoria in categorias)
                Categorias.Add(categoria);

            var impuestos = await catalogos.ObtenerImpuestosAsync();
            Impuestos.Clear();
            foreach (var impuesto in impuestos)
                Impuestos.Add(impuesto);

            var productos = await catalogos.ObtenerProductosActivosAsync();
            Productos.Clear();
            foreach (var producto in productos)
                Productos.Add(producto);
        }
        catch (Exception ex)
        {
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private string Traducir(IServiceProvider? servicios, Exception ex)
        => _traductor?.Traducir(ex)
            ?? servicios?.GetService<TraductorExcepciones>()?.Traducir(ex)
            ?? ex.Message;
}
