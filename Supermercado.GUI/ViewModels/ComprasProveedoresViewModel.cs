using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;

namespace Supermercado.GUI.ViewModels;

public sealed class SolicitudReabastecimientoItemViewModel
{
    public Reabastecimiento Solicitud { get; init; } = null!;
    public string NombreProducto { get; init; } = string.Empty;
    public string CodigoBarras { get; init; } = string.Empty;

    public int Id => Solicitud.Id;
    public int ProductoId => Solicitud.ProductoId;
    public int CantidadSolicitada => Solicitud.CantidadSolicitada;
    public string FechaTexto => Solicitud.FechaSolicitud.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string EstadoTexto => Solicitud.Estado.ToString().ToUpperInvariant();
}

public partial class ComprasProveedoresViewModel : ViewModelBase
{
    private readonly IServicioCatalogos? _servicioCatalogos;
    private readonly IServicioReabastecimiento? _servicioReabastecimiento;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ObservableCollection<Proveedor> Proveedores { get; } = [];
    public ObservableCollection<Categoria> Categorias { get; } = [];
    public ObservableCollection<SolicitudReabastecimientoItemViewModel> SolicitudesPendientes { get; } = [];

    [ObservableProperty]
    public partial Proveedor? ProveedorSeleccionado { get; set; }

    [ObservableProperty]
    public partial string NombreProveedorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NitProveedorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TelefonoProveedorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CorreoProveedorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NuevaCategoriaInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Gestión de proveedores, categorías y sugeridos de compra/reabastecimiento FEFO.";

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    public ComprasProveedoresViewModel()
    {
    }

    public ComprasProveedoresViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public ComprasProveedoresViewModel(
        IServicioCatalogos servicioCatalogos,
        IServicioReabastecimiento servicioReabastecimiento,
        ITraductorExcepciones? traductor = null)
    {
        _servicioCatalogos = servicioCatalogos;
        _servicioReabastecimiento = servicioReabastecimiento;
        _traductor = traductor;
    }

    partial void OnProveedorSeleccionadoChanged(Proveedor? value)
    {
        if (value is null)
            return;

        NombreProveedorInput = value.Nombre;
        NitProveedorInput = value.Nit;
        TelefonoProveedorInput = value.Telefono;
        CorreoProveedorInput = value.Correo;
    }

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        if (Cargando)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioCatalogos ?? servicios?.GetService<IServicioCatalogos>();
        var reabastecimiento = _servicioReabastecimiento ?? servicios?.GetService<IServicioReabastecimiento>();
        var repoProductos = servicios?.GetService<IProductoRepositorio>();
        if (catalogos is null)
            return;

        Cargando = true;
        TieneError = false;
        try
        {
            var provs = await catalogos.ObtenerProveedoresAsync();
            var cats = await catalogos.ObtenerCategoriasAsync();

            Proveedores.Clear();
            foreach (var p in provs)
                Proveedores.Add(p);

            Categorias.Clear();
            foreach (var c in cats)
                Categorias.Add(c);

            if (reabastecimiento is not null)
            {
                try
                {
                    var pendientes = await reabastecimiento.ObtenerPendientesAsync();
                    var dictProds = repoProductos is not null
                        ? (await repoProductos.ObtenerTodosAsync()).ToDictionary(x => x.Id)
                        : new Dictionary<int, Producto>();

                    SolicitudesPendientes.Clear();
                    foreach (var s in pendientes)
                    {
                        var prod = dictProds.GetValueOrDefault(s.ProductoId);
                        SolicitudesPendientes.Add(new SolicitudReabastecimientoItemViewModel
                        {
                            Solicitud = s,
                            NombreProducto = prod?.Nombre ?? $"Producto #{s.ProductoId}",
                            CodigoBarras = prod?.CodigoBarras ?? "-"
                        });
                    }
                }
                catch
                {
                    // Si el usuario tiene acceso a catálogos pero no a reabastecer, continúa
                }
            }

            MensajeEstado = $"Catálogo actualizado: {Proveedores.Count} proveedores, {Categorias.Count} categorías y {SolicitudesPendientes.Count} sugeridos.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    public async Task GuardarProveedorAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioCatalogos ?? servicios?.GetService<IServicioCatalogos>();
        if (catalogos is null)
            return;

        TieneError = false;
        try
        {
            if (ProveedorSeleccionado is not null)
            {
                await catalogos.EditarProveedorAsync(
                    ProveedorSeleccionado.Id,
                    NombreProveedorInput.Trim(),
                    NitProveedorInput.Trim(),
                    TelefonoProveedorInput.Trim(),
                    CorreoProveedorInput.Trim());
                MensajeEstado = $"Proveedor '{NombreProveedorInput}' actualizado.";
            }
            else
            {
                var creado = await catalogos.CrearProveedorAsync(
                    NombreProveedorInput.Trim(),
                    NitProveedorInput.Trim(),
                    TelefonoProveedorInput.Trim(),
                    CorreoProveedorInput.Trim());
                MensajeEstado = $"Proveedor '{creado.Nombre}' registrado.";
            }

            LimpiarFormularioProveedor();
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public void LimpiarFormularioProveedor()
    {
        ProveedorSeleccionado = null;
        NombreProveedorInput = string.Empty;
        NitProveedorInput = string.Empty;
        TelefonoProveedorInput = string.Empty;
        CorreoProveedorInput = string.Empty;
    }

    [RelayCommand]
    public async Task CrearCategoriaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevaCategoriaInput))
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var catalogos = _servicioCatalogos ?? servicios?.GetService<IServicioCatalogos>();
        if (catalogos is null)
            return;

        TieneError = false;
        try
        {
            var cat = await catalogos.CrearCategoriaAsync(NuevaCategoriaInput.Trim());
            NuevaCategoriaInput = string.Empty;
            await CargarDatosAsync();
            MensajeEstado = $"Categoría '{cat.Nombre}' agregada.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task GenerarSugeridosAutomaticosAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var reabastecimiento = _servicioReabastecimiento ?? servicios?.GetService<IServicioReabastecimiento>();
        if (reabastecimiento is null)
            return;

        TieneError = false;
        try
        {
            var nuevas = await reabastecimiento.GenerarSolicitudesAsync();
            await CargarDatosAsync();
            MensajeEstado = $"Se generaron {nuevas.Count} nuevas solicitudes de reabastecimiento automático por stock bajo mínimo.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task AtenderSolicitudReabastecimientoAsync(SolicitudReabastecimientoItemViewModel? item)
    {
        if (item is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var reabastecimiento = _servicioReabastecimiento ?? servicios?.GetService<IServicioReabastecimiento>();
        if (reabastecimiento is null)
            return;

        TieneError = false;
        try
        {
            await reabastecimiento.AtenderAsync(item.Id);
            await CargarDatosAsync();
            MensajeEstado = $"Solicitud #{item.Id} ({item.NombreProducto}) atendida: stock trasladado de bodega a estante.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private string Traducir(IServiceProvider? servicios, Exception ex)
        => _traductor?.Traducir(ex)
           ?? servicios?.GetService<ITraductorExcepciones>()?.Traducir(ex)
           ?? ex.Message;
}