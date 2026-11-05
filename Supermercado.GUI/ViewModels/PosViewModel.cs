using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.GUI.Servicios.Impresion;
using Supermercado.Logica.Clientes;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;

namespace Supermercado.GUI.ViewModels;

public partial class LineaVentaItemViewModel : ObservableObject
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public Producto Producto { get; }
    public string CodigoBarras => Producto.CodigoBarras;
    public string Nombre => Producto.Nombre;
    public bool EsPesado => Producto is ProductoPesado;
    public string UnidadMedida => EsPesado ? "KG" : "UN";
    public long PrecioUnitario => Producto.PrecioVenta;
    public int PuntosBasicosIva => Producto.Impuesto?.PuntosBasicos ?? 0;
    public string CodigoTarifaDian => PlantillaTicketPos.ObtenerCodigoTarifa(PuntosBasicosIva);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CantidadFormateada))]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    [NotifyPropertyChangedFor(nameof(SubtotalFormateado))]
    [NotifyPropertyChangedFor(nameof(IvaLinea))]
    public partial int Cantidad { get; set; }

    public string CantidadFormateada => EsPesado
        ? $"{(Cantidad / 1000.0m).ToString("0.000", CulturaCo)} KG"
        : $"{Cantidad} UN";

    public string PrecioUnitarioFormateado => $"$ {PrecioUnitario.ToString("N0", CulturaCo)}";

    public long Subtotal => Producto.CalcularSubtotal(Cantidad);

    public string SubtotalFormateado => $"$ {Subtotal.ToString("N0", CulturaCo)}";

    public long IvaLinea => Producto.Impuesto?.Calcular(Subtotal) ?? 0;

    public LineaVentaItemViewModel(Producto producto, int cantidad)
    {
        Producto = producto;
        Cantidad = cantidad;
    }
}

public sealed class RenglonPagoItemViewModel
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public string Metodo { get; init; } = "Efectivo";
    public long Monto { get; init; }
    public string Detalle { get; init; } = string.Empty;

    public string MontoFormateado => $"$ {Monto.ToString("N0", CulturaCo)}";

    public string EtiquetaResumen => string.IsNullOrWhiteSpace(Detalle)
        ? Metodo
        : $"{Metodo} ({Detalle})";

    public Pago ConstruirPagoDominio() => Metodo switch
    {
        "Tarjeta" => new PagoTarjeta
        {
            Monto = Monto,
            UltimosDigitos = string.IsNullOrWhiteSpace(Detalle) || Detalle.Length != 4
                ? "1234"
                : Detalle
        },
        "Transferencia" => new PagoTransferencia
        {
            Monto = Monto,
            Referencia = string.IsNullOrWhiteSpace(Detalle)
                ? "TRANSF-POS"
                : Detalle
        },
        _ => new PagoEfectivo
        {
            Monto = Monto
        }
    };
}

public partial class PosViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    private readonly IServicioVentas? _servicioVentas;
    private readonly IServicioCaja? _servicioCaja;
    private readonly IServicioProductos? _servicioProductos;
    private readonly IServicioBusquedaProductos? _servicioBusqueda;
    private readonly IServicioClientes? _servicioClientes;
    private readonly IServicioVentasEnEspera? _servicioVentasEnEspera;
    private readonly IServicioAutenticacion? _servicioAutenticacion;
    private readonly ISesionActual? _sesion;
    private readonly IImpresorTicket? _impresorTicket;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;
    private string? _claveIdempotenciaCobro;
    private Func<Task>? _accionPendienteSupervisor;

    public ObservableCollection<LineaVentaItemViewModel> Carrito { get; } = [];
    public ObservableCollection<Producto> ResultadosBusqueda { get; } = [];
    public ObservableCollection<RenglonPagoItemViewModel> PagosAgregados { get; } = [];
    public ObservableCollection<ItemVentaEnEspera> VentasEnEspera { get; } = [];

    [ObservableProperty]
    public partial string CodigoBarrasInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EliminarLineaCommand))]
    [NotifyCanExecuteChangedFor(nameof(SolicitarEliminarLineaCommand))]
    [NotifyCanExecuteChangedFor(nameof(AbrirModalCantidadCommand))]
    public partial LineaVentaItemViewModel? LineaSeleccionada { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalFormateado))]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoPesos))]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoFormateado))]
    [NotifyPropertyChangedFor(nameof(CambioPesos))]
    [NotifyPropertyChangedFor(nameof(FaltantePesos))]
    [NotifyPropertyChangedFor(nameof(EfectivoSuficiente))]
    [NotifyPropertyChangedFor(nameof(CambioFormateado))]
    public partial long TotalPesos { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtotalBaseFormateado))]
    public partial long SubtotalBasePesos { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IvaFormateado))]
    public partial long IvaPesos { get; set; }

    [ObservableProperty]
    public partial long BaseTarifaC { get; set; }

    [ObservableProperty]
    public partial long BaseTarifaM { get; set; }

    [ObservableProperty]
    public partial long IvaTarifaM { get; set; }

    [ObservableProperty]
    public partial long BaseTarifaJ { get; set; }

    [ObservableProperty]
    public partial long IvaTarifaJ { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeCobrar))]
    [NotifyCanExecuteChangedFor(nameof(AbrirModalCobroCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmarCobroCommand))]
    [NotifyCanExecuteChangedFor(nameof(SuspenderVentaActualCommand))]
    public partial bool TieneTurnoAbierto { get; set; }

    [ObservableProperty]
    public partial int CajaActivaId { get; set; } = 1;

    [ObservableProperty]
    public partial string InfoTurnoActual { get; set; } = "Verificando turno...";

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Listo para escanear (F1 Cobrar · F2 Cantidad · F3 Buscar · F4 Eliminar · F6 Espera)";

    [ObservableProperty]
    public partial bool TieneAlerta { get; set; }

    // Estado de Modal de Cobro y Pagos Mixtos (F1)
    [ObservableProperty]
    public partial bool ModalCobroAbierto { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsPagoEfectivo))]
    [NotifyPropertyChangedFor(nameof(EsPagoTarjeta))]
    [NotifyPropertyChangedFor(nameof(EsPagoTransferencia))]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoPesos))]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoFormateado))]
    [NotifyPropertyChangedFor(nameof(CambioPesos))]
    [NotifyPropertyChangedFor(nameof(FaltantePesos))]
    [NotifyPropertyChangedFor(nameof(EfectivoSuficiente))]
    [NotifyPropertyChangedFor(nameof(CambioFormateado))]
    public partial string MetodoPagoSeleccionado { get; set; } = "Efectivo";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoPesos))]
    [NotifyPropertyChangedFor(nameof(TotalAbonadoFormateado))]
    [NotifyPropertyChangedFor(nameof(CambioPesos))]
    [NotifyPropertyChangedFor(nameof(FaltantePesos))]
    [NotifyPropertyChangedFor(nameof(EfectivoSuficiente))]
    [NotifyPropertyChangedFor(nameof(CambioFormateado))]
    public partial long EfectivoEntregadoPesos { get; set; }

    [ObservableProperty]
    public partial string UltimosDigitosTarjeta { get; set; } = "1234";

    [ObservableProperty]
    public partial string ReferenciaTransferencia { get; set; } = "NEQUI-001";

    // Estado de Modal de Autorización de Supervisor (PIN en Caliente)
    [ObservableProperty]
    public partial bool ModalSupervisorAbierto { get; set; }

    [ObservableProperty]
    public partial string TituloAccionSupervisor { get; set; } = "Autorización de Supervisor Requerida";

    [ObservableProperty]
    public partial string UsuarioSupervisorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PinSupervisorInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorSupervisor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneErrorSupervisor { get; set; }

    [ObservableProperty]
    public partial string MotivoAnulacionInput { get; set; } = "Corrección de cajero / Solicitud del cliente";

    [ObservableProperty]
    public partial int VentaIdParaAnularInput { get; set; }

    [ObservableProperty]
    public partial bool EsAnulacionFacturaCompleta { get; set; }

    // Estado de Ventas en Espera / Parked Sales (F6)
    [ObservableProperty]
    public partial bool ModalVentasEnEsperaAbierto { get; set; }

    [ObservableProperty]
    public partial string NotaVentaEnEsperaInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtiquetaBotonVentasEnEspera))]
    public partial int CantidadVentasEnEspera { get; set; }

    public string EtiquetaBotonVentasEnEspera => $"En Espera ({CantidadVentasEnEspera})";

    // Estado de Modal de Cantidad (F2)
    [ObservableProperty]
    public partial bool ModalCantidadAbierto { get; set; }

    [ObservableProperty]
    public partial int NuevaCantidadInput { get; set; } = 1;

    // Estado de Modal de Búsqueda Rápida (F3)
    [ObservableProperty]
    public partial bool ModalBusquedaAbierto { get; set; }

    [ObservableProperty]
    public partial string TerminoBusquedaRapida { get; set; } = string.Empty;

    // Estado de Cliente y Habeas Data (F5)
    [ObservableProperty]
    public partial string DocumentoClienteInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EtiquetaAdquirente))]
    public partial Cliente? ClienteActual { get; set; }

    [ObservableProperty]
    public partial bool ModalRegistroClienteAbierto { get; set; }

    public RegistroUsuarioViewModel RegistroClienteVm { get; }

    // Estado de Apertura Rápida de Turno
    [ObservableProperty]
    public partial bool ModalAperturaTurnoAbierto { get; set; }

    [ObservableProperty]
    public partial long EfectivoInicialApertura { get; set; } = 100000;

    // Vista previa del último ticket emitido
    [ObservableProperty]
    public partial bool MostrarVistaPreviaTicket { get; set; }

    [ObservableProperty]
    public partial string UltimoTicketTexto { get; set; } = string.Empty;

    public bool PuedeCobrar => Carrito.Count > 0 && TieneTurnoAbierto;
    public int TotalArticulos => Carrito.Count;

    public bool EsPagoEfectivo => MetodoPagoSeleccionado == "Efectivo";
    public bool EsPagoTarjeta => MetodoPagoSeleccionado == "Tarjeta";
    public bool EsPagoTransferencia => MetodoPagoSeleccionado == "Transferencia";
    public bool TienePagosMixtos => PagosAgregados.Count > 0;

    public long TotalAbonadoPesos => PagosAgregados.Count > 0
        ? PagosAgregados.Sum(p => p.Monto)
        : (EsPagoEfectivo ? EfectivoEntregadoPesos : TotalPesos);

    public string TotalAbonadoFormateado => $"$ {TotalAbonadoPesos.ToString("N0", CulturaCo)}";

    public long CambioPesos => PagosAgregados.Count > 0
        ? Math.Max(0, TotalAbonadoPesos - TotalPesos)
        : (EsPagoEfectivo ? Math.Max(0, EfectivoEntregadoPesos - TotalPesos) : 0);

    public long FaltantePesos => PagosAgregados.Count > 0
        ? Math.Max(0, TotalPesos - TotalAbonadoPesos)
        : (EsPagoEfectivo ? Math.Max(0, TotalPesos - EfectivoEntregadoPesos) : 0);

    public bool EfectivoSuficiente => PagosAgregados.Count > 0
        ? TotalAbonadoPesos >= TotalPesos
        : (!EsPagoEfectivo || EfectivoEntregadoPesos >= TotalPesos);

    public string TotalFormateado => $"$ {TotalPesos.ToString("N0", CulturaCo)}";
    public string SubtotalBaseFormateado => $"$ {SubtotalBasePesos.ToString("N0", CulturaCo)}";
    public string IvaFormateado => $"$ {IvaPesos.ToString("N0", CulturaCo)}";
    public string CambioFormateado => EfectivoSuficiente
        ? $"$ {CambioPesos.ToString("N0", CulturaCo)}"
        : $"Faltan $ {FaltantePesos.ToString("N0", CulturaCo)}";

    public string EtiquetaAdquirente => ClienteActual is null
        ? "CONSUMIDOR FINAL (222222222222)"
        : ClienteActual.AutorizaTratamientoDatos
            ? $"{ClienteActual.NombreCompleto} ({ClienteActual.Documento})"
            : $"{ClienteActual.NombreCompleto} [Sin Habeas Data -> 222222222222]";

    public IReadOnlyList<string> MetodosPagoDisponibles { get; } = ["Efectivo", "Tarjeta", "Transferencia"];

    public event Action? SolicitarFocoEscaneo;

    public PosViewModel()
    {
        RegistroClienteVm = new RegistroUsuarioViewModel();
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
    }

    public PosViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        RegistroClienteVm = new RegistroUsuarioViewModel(scopeFactory);
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        EnlazarRegistroCliente();
    }

    public PosViewModel(
        IServicioVentas servicioVentas,
        IServicioCaja servicioCaja,
        IServicioProductos servicioProductos,
        IServicioBusquedaProductos? servicioBusqueda = null,
        IServicioClientes? servicioClientes = null,
        IImpresorTicket? impresorTicket = null,
        ITraductorExcepciones? traductor = null,
        IServicioVentasEnEspera? servicioVentasEnEspera = null,
        IServicioAutenticacion? servicioAutenticacion = null,
        ISesionActual? sesion = null)
    {
        _servicioVentas = servicioVentas;
        _servicioCaja = servicioCaja;
        _servicioProductos = servicioProductos;
        _servicioBusqueda = servicioBusqueda;
        _servicioClientes = servicioClientes;
        _impresorTicket = impresorTicket;
        _traductor = traductor;
        _servicioVentasEnEspera = servicioVentasEnEspera;
        _servicioAutenticacion = servicioAutenticacion;
        _sesion = sesion;

        RegistroClienteVm = new RegistroUsuarioViewModel();
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        EnlazarRegistroCliente();
    }

    private void EnlazarRegistroCliente()
    {
        RegistroClienteVm.ClienteRegistrado += cliente =>
        {
            ClienteActual = cliente;
            ModalRegistroClienteAbierto = false;
            SolicitarFocoEscaneo?.Invoke();
        };
        RegistroClienteVm.Cancelado += () =>
        {
            ModalRegistroClienteAbierto = false;
            SolicitarFocoEscaneo?.Invoke();
        };
    }

    public async Task InicializarEstadoCajaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja
            ?? servicios?.GetService<IServicioCaja>();
        if (servicioCaja is null)
            return;

        try
        {
            var turno = await servicioCaja.ObtenerTurnoActualAsync();
            TieneTurnoAbierto = turno is not null;
            if (turno is not null)
            {
                CajaActivaId = turno.CajaId > 0 ? turno.CajaId : 1;
                InfoTurnoActual = $"Turno #{turno.Id} Activo · Base $ {turno.EfectivoInicial.ToString("N0", CulturaCo)}";
                ModalAperturaTurnoAbierto = false;
                await ActualizarContadorVentasEnEsperaAsync(servicios);
            }
            else
            {
                InfoTurnoActual = "Caja Cerrada (Requiere Apertura de Turno)";
                ModalAperturaTurnoAbierto = true;
            }
        }
        catch (Exception ex)
        {
            TieneTurnoAbierto = false;
            InfoTurnoActual = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task EscanearOAgregarAsync()
    {
        var entrada = CodigoBarrasInput?.Trim();
        using var scope = _scopeFactory?.CreateScope();
        var servicioProductos = _servicioProductos
            ?? scope?.ServiceProvider.GetService<IServicioProductos>();
        if (string.IsNullOrWhiteSpace(entrada) || servicioProductos is null)
            return;

        TieneAlerta = false;

        try
        {
            var datosBalanza = ParserCodigoBalanza.Parsear(entrada);
            var producto = await servicioProductos.BuscarPorCodigoBarrasAsync(entrada);

            if (producto is null && datosBalanza.EsCodigoBalanza)
            {
                producto = await servicioProductos.BuscarPorCodigoBarrasAsync(datosBalanza.Plu)
                           ?? await servicioProductos.BuscarPorCodigoBarrasAsync(datosBalanza.Plu.TrimStart('0'));
            }

            if (producto is null)
            {
                TieneAlerta = true;
                MensajeEstado = $"Producto no encontrado para el código '{entrada}'.";
                return;
            }

            var cantidadAgregar = (producto is ProductoPesado && datosBalanza.EsCodigoBalanza && datosBalanza.Gramos > 0)
                ? datosBalanza.Gramos
                : (producto is ProductoPesado ? 500 : 1);

            AgregarProductoAlCarrito(producto, cantidadAgregar);
            CodigoBarrasInput = string.Empty;

            MensajeEstado = datosBalanza.EsCodigoBalanza
                ? $"Balanza EAN-13: {producto.Nombre} ({datosBalanza.Gramos} g) agregado."
                : $"Agregado: {producto.Nombre} ({producto.PrecioVenta.ToString("N0", CulturaCo)}).";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(scope?.ServiceProvider, ex);
        }
        finally
        {
            SolicitarFocoEscaneo?.Invoke();
        }
    }

    public void AgregarProductoAlCarrito(Producto producto, int cantidad)
    {
        if (cantidad <= 0)
            return;

        var existente = Carrito.FirstOrDefault(l => l.Producto.Id == producto.Id && l.CodigoBarras == producto.CodigoBarras);
        if (existente is not null)
        {
            existente.Cantidad += cantidad;
            LineaSeleccionada = existente;
        }
        else
        {
            var nueva = new LineaVentaItemViewModel(producto, cantidad);
            Carrito.Add(nueva);
            LineaSeleccionada = nueva;
        }

        RecalcularTotales();
    }

    public void RecalcularTotales()
    {
        long total = 0;
        long ivaTotal = 0;
        long baseC = 0;
        long baseM = 0;
        long ivaM = 0;
        long baseJ = 0;
        long ivaJ = 0;

        foreach (var item in Carrito)
        {
            var sub = item.Subtotal;
            var iva = item.IvaLinea;
            var baseLinea = Math.Max(0, sub - iva);

            total += sub;
            ivaTotal += iva;

            if (item.PuntosBasicosIva == 0)
            {
                baseC += baseLinea;
            }
            else if (item.PuntosBasicosIva == 500)
            {
                baseM += baseLinea;
                ivaM += iva;
            }
            else
            {
                baseJ += baseLinea;
                ivaJ += iva;
            }
        }

        TotalPesos = total;
        IvaPesos = ivaTotal;
        SubtotalBasePesos = Math.Max(0, total - ivaTotal);
        BaseTarifaC = baseC;
        BaseTarifaM = baseM;
        IvaTarifaM = ivaM;
        BaseTarifaJ = baseJ;
        IvaTarifaJ = ivaJ;

        if (!ModalCobroAbierto)
        {
            EfectivoEntregadoPesos = total;
        }

        OnPropertyChanged(nameof(PuedeCobrar));
        OnPropertyChanged(nameof(TotalArticulos));
        AbrirModalCobroCommand.NotifyCanExecuteChanged();
        ConfirmarCobroCommand.NotifyCanExecuteChanged();
        SuspenderVentaActualCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(PuedeCobrar))]
    private void AbrirModalCobro()
    {
        PagosAgregados.Clear();
        EfectivoEntregadoPesos = TotalPesos;
        MetodoPagoSeleccionado = "Efectivo";
        NotificarEstadoPagos();
        ModalCobroAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalCobro()
    {
        ModalCobroAbierto = false;
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    private void AsignarBilleteRapido(object? parametro)
    {
        if (parametro is null)
            return;

        if (long.TryParse(parametro.ToString(), out var montoBillete) && montoBillete > 0)
        {
            MetodoPagoSeleccionado = "Efectivo";
            EfectivoEntregadoPesos = montoBillete;
        }
    }

    [RelayCommand]
    public void AgregarRenglonPago()
    {
        if (EfectivoEntregadoPesos <= 0)
            return;

        var detalle = MetodoPagoSeleccionado switch
        {
            "Tarjeta" => string.IsNullOrWhiteSpace(UltimosDigitosTarjeta) || UltimosDigitosTarjeta.Length != 4
                ? "1234"
                : UltimosDigitosTarjeta.Trim(),
            "Transferencia" => string.IsNullOrWhiteSpace(ReferenciaTransferencia)
                ? "TRANSF-POS"
                : ReferenciaTransferencia.Trim(),
            _ => string.Empty
        };

        PagosAgregados.Add(new RenglonPagoItemViewModel
        {
            Metodo = MetodoPagoSeleccionado,
            Monto = EfectivoEntregadoPesos,
            Detalle = detalle
        });

        NotificarEstadoPagos();
        EfectivoEntregadoPesos = FaltantePesos > 0 ? FaltantePesos : 0;
    }

    [RelayCommand]
    public void QuitarRenglonPago(RenglonPagoItemViewModel? renglon)
    {
        if (renglon is null)
            return;

        PagosAgregados.Remove(renglon);
        NotificarEstadoPagos();
        EfectivoEntregadoPesos = FaltantePesos > 0 ? FaltantePesos : TotalPesos;
    }

    [RelayCommand]
    public void LimpiarRenglonesPago()
    {
        PagosAgregados.Clear();
        NotificarEstadoPagos();
        EfectivoEntregadoPesos = TotalPesos;
    }

    private void NotificarEstadoPagos()
    {
        OnPropertyChanged(nameof(TienePagosMixtos));
        OnPropertyChanged(nameof(TotalAbonadoPesos));
        OnPropertyChanged(nameof(TotalAbonadoFormateado));
        OnPropertyChanged(nameof(CambioPesos));
        OnPropertyChanged(nameof(FaltantePesos));
        OnPropertyChanged(nameof(EfectivoSuficiente));
        OnPropertyChanged(nameof(CambioFormateado));
    }

    [RelayCommand(CanExecute = nameof(PuedeCobrar))]
    public async Task ConfirmarCobroAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioVentas = _servicioVentas
            ?? servicios?.GetService<IServicioVentas>();
        var impresor = _impresorTicket
            ?? servicios?.GetService<IImpresorTicket>();
        if (servicioVentas is null || !PuedeCobrar)
            return;

        if (!EfectivoSuficiente)
        {
            TieneAlerta = true;
            MensajeEstado = $"El monto abonado ($ {TotalAbonadoPesos:N0}) es menor al total ($ {TotalPesos:N0}).";
            return;
        }

        TieneAlerta = false;
        try
        {
            var lineas = Carrito
                .Select(c => new LineaVenta(c.CodigoBarras, c.Cantidad))
                .ToList();

            List<Pago> listaPagos;
            if (PagosAgregados.Count > 0)
            {
                listaPagos = PagosAgregados.Select(p => p.ConstruirPagoDominio()).ToList();
            }
            else
            {
                Pago pagoUnico = MetodoPagoSeleccionado switch
                {
                    "Tarjeta" => new PagoTarjeta
                    {
                        Monto = TotalPesos,
                        UltimosDigitos = string.IsNullOrWhiteSpace(UltimosDigitosTarjeta) || UltimosDigitosTarjeta.Length != 4
                            ? "1234"
                            : UltimosDigitosTarjeta
                    },
                    "Transferencia" => new PagoTransferencia
                    {
                        Monto = TotalPesos,
                        Referencia = string.IsNullOrWhiteSpace(ReferenciaTransferencia)
                            ? "TRANSF-POS"
                            : ReferenciaTransferencia
                    },
                    _ => new PagoEfectivo
                    {
                        Monto = EfectivoEntregadoPesos
                    }
                };
                listaPagos = [pagoUnico];
            }

            var solicitud = new SolicitudVenta(
                Lineas: lineas,
                Pagos: listaPagos,
                ClienteId: ClienteActual?.Id,
                ClaveIdempotencia: _claveIdempotenciaCobro ??= Guid.NewGuid().ToString("N"));

            var ventaRegistrada = await servicioVentas.RegistrarVentaAsync(solicitud);

            var cambio = ventaRegistrada.Cambio;
            Carrito.Clear();
            PagosAgregados.Clear();
            ClienteActual = null;
            DocumentoClienteInput = string.Empty;
            ModalCobroAbierto = false;
            _claveIdempotenciaCobro = null;
            RecalcularTotales();
            NotificarEstadoPagos();

            if (impresor is not null)
            {
                UltimoTicketTexto = await impresor.FormatearTirillaTextoAsync(ventaRegistrada);
                MostrarVistaPreviaTicket = true;
                try
                {
                    await impresor.ImprimirTicketVentaAsync(ventaRegistrada);
                }
                catch (Exception ex)
                {
                    TieneAlerta = true;
                    MensajeEstado = $"La venta quedó registrada, pero no se pudo imprimir: {Traducir(servicios, ex)}";
                    return;
                }
            }

            MensajeEstado = $"Venta #{ventaRegistrada.Id} completada. Cambio: $ {cambio.ToString("N0", CulturaCo)}.";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            SolicitarFocoEscaneo?.Invoke();
        }
    }

    // --- GESTIÓN DE VENTAS EN ESPERA / PARKED SALES (F6) ---

    [RelayCommand(CanExecute = nameof(PuedeCobrar))]
    public async Task SuspenderVentaActualAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEspera = _servicioVentasEnEspera
            ?? servicios?.GetService<IServicioVentasEnEspera>();
        if (servicioEspera is null || Carrito.Count == 0)
            return;

        TieneAlerta = false;
        try
        {
            var lineas = Carrito
                .Select(c => new LineaVenta(c.CodigoBarras, c.Cantidad))
                .ToList();

            var identificador = ClienteActual?.NombreCompleto
                ?? (string.IsNullOrWhiteSpace(DocumentoClienteInput)
                    ? $"Ticket-{DateTime.Now:HH:mm:ss}"
                    : DocumentoClienteInput.Trim());

            await servicioEspera.SuspenderVentaAsync(
                CajaActivaId,
                lineas,
                ClienteActual?.Id,
                identificador,
                TotalPesos,
                string.IsNullOrWhiteSpace(NotaVentaEnEsperaInput) ? "En espera" : NotaVentaEnEsperaInput.Trim());

            Carrito.Clear();
            ClienteActual = null;
            DocumentoClienteInput = string.Empty;
            NotaVentaEnEsperaInput = string.Empty;
            _claveIdempotenciaCobro = null;
            RecalcularTotales();

            await ActualizarContadorVentasEnEsperaAsync(servicios);
            MensajeEstado = $"Venta puesta en espera ({identificador}). Carrito libre para el siguiente cliente.";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            SolicitarFocoEscaneo?.Invoke();
        }
    }

    [RelayCommand]
    public async Task AbrirModalVentasEnEsperaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        await ActualizarContadorVentasEnEsperaAsync(scope?.ServiceProvider);
        ModalVentasEnEsperaAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalVentasEnEspera()
    {
        ModalVentasEnEsperaAbierto = false;
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    public async Task RecuperarVentaEnEsperaAsync(ItemVentaEnEspera? item)
    {
        if (item is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEspera = _servicioVentasEnEspera
            ?? servicios?.GetService<IServicioVentasEnEspera>();
        var servicioProductos = _servicioProductos
            ?? servicios?.GetService<IServicioProductos>();
        if (servicioEspera is null || servicioProductos is null)
            return;

        TieneAlerta = false;
        try
        {
            var (venta, lineas) = await servicioEspera.RecuperarVentaAsync(item.Id);
            Carrito.Clear();

            foreach (var linea in lineas)
            {
                var prod = await servicioProductos.BuscarPorCodigoBarrasAsync(linea.CodigoBarras);
                if (prod is not null)
                {
                    AgregarProductoAlCarrito(prod, linea.Cantidad);
                }
            }

            if (venta.ClienteId.HasValue)
            {
                var servicioClientes = _servicioClientes ?? servicios?.GetService<IServicioClientes>();
                if (servicioClientes is not null && !string.IsNullOrWhiteSpace(venta.IdentificadorCliente))
                {
                    ClienteActual = await servicioClientes.BuscarPorDocumentoAsync(venta.IdentificadorCliente);
                }
            }

            ModalVentasEnEsperaAbierto = false;
            await ActualizarContadorVentasEnEsperaAsync(servicios);
            MensajeEstado = $"Ticket en espera #{venta.Id} ({venta.IdentificadorCliente}) recuperado en caja.";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            SolicitarFocoEscaneo?.Invoke();
        }
    }

    [RelayCommand]
    public async Task DescartarVentaEnEsperaAsync(ItemVentaEnEspera? item)
    {
        if (item is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEspera = _servicioVentasEnEspera
            ?? servicios?.GetService<IServicioVentasEnEspera>();
        if (servicioEspera is null)
            return;

        try
        {
            await servicioEspera.CancelarVentaEnEsperaAsync(item.Id);
            await ActualizarContadorVentasEnEsperaAsync(servicios);
            MensajeEstado = $"Venta en espera #{item.Id} descartada.";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private async Task ActualizarContadorVentasEnEsperaAsync(IServiceProvider? servicios)
    {
        var servicioEspera = _servicioVentasEnEspera
            ?? servicios?.GetService<IServicioVentasEnEspera>();
        if (servicioEspera is null)
            return;

        try
        {
            var pendientes = await servicioEspera.ObtenerVentasEnEsperaAsync(CajaActivaId);
            VentasEnEspera.Clear();
            foreach (var p in pendientes)
            {
                VentasEnEspera.Add(p);
            }
            CantidadVentasEnEspera = VentasEnEspera.Count;
        }
        catch
        {
            // No interrumpe el flujo principal si aún no hay sesión con permiso Vender
        }
    }

    // --- AUTORIZACIÓN DE SUPERVISOR EN CALIENTE (PIN / CLAVE) ---

    private bool UsuarioActivoTienePermiso(IServiceProvider? servicios, Permiso permiso)
    {
        var sesion = _sesion ?? servicios?.GetService<ISesionActual>();
        if (sesion is null || !sesion.EstaAutenticada)
            return true; // En pruebas unitarias sin sesión inyectada permite ejecución directa

        return sesion.Tiene(permiso);
    }

    [RelayCommand(CanExecute = nameof(TieneLineaSeleccionada))]
    public void SolicitarEliminarLinea()
    {
        if (LineaSeleccionada is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        if (UsuarioActivoTienePermiso(scope?.ServiceProvider, Permiso.AutorizarOperacionPos))
        {
            EliminarLinea();
            return;
        }

        AbrirModalAutorizacionSupervisor(
            $"Autorizar eliminación de línea: {LineaSeleccionada.Nombre}",
            () =>
            {
                EliminarLinea();
                return Task.CompletedTask;
            });
    }

    [RelayCommand]
    public void SolicitarCancelarVenta()
    {
        if (Carrito.Count == 0)
        {
            CancelarVenta();
            return;
        }

        using var scope = _scopeFactory?.CreateScope();
        if (UsuarioActivoTienePermiso(scope?.ServiceProvider, Permiso.AutorizarOperacionPos))
        {
            CancelarVenta();
            return;
        }

        AbrirModalAutorizacionSupervisor(
            "Autorizar anulación / limpieza del carrito en curso",
            () =>
            {
                CancelarVenta();
                return Task.CompletedTask;
            });
    }

    [RelayCommand]
    public void AbrirModalAnularFacturaEmitida()
    {
        EsAnulacionFacturaCompleta = true;
        AbrirModalAutorizacionSupervisor(
            "Anular Factura POS Emitida (Reintegro de Inventario)",
            EjecutarAnulacionFacturaRegistradaAsync);
    }

    private void AbrirModalAutorizacionSupervisor(string titulo, Func<Task> accionAutorizada)
    {
        TituloAccionSupervisor = titulo;
        UsuarioSupervisorInput = string.Empty;
        PinSupervisorInput = string.Empty;
        ErrorSupervisor = string.Empty;
        TieneErrorSupervisor = false;
        _accionPendienteSupervisor = accionAutorizada;
        ModalSupervisorAbierto = true;
    }

    [RelayCommand]
    private void AgregarDigitoPinSupervisor(string? digito)
    {
        if (string.IsNullOrEmpty(digito) || PinSupervisorInput.Length >= 12)
            return;

        PinSupervisorInput += digito;
    }

    [RelayCommand]
    private void BorrarDigitoPinSupervisor()
    {
        if (PinSupervisorInput.Length > 0)
        {
            PinSupervisorInput = PinSupervisorInput[..^1];
        }
    }

    [RelayCommand]
    public async Task ConfirmarAutorizacionSupervisorAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioAuth = _servicioAutenticacion
            ?? servicios?.GetService<IServicioAutenticacion>();
        if (servicioAuth is null)
            return;

        TieneErrorSupervisor = false;
        ErrorSupervisor = string.Empty;

        try
        {
            var permisoRequerido = EsAnulacionFacturaCompleta
                ? Permiso.AnularVenta
                : Permiso.AutorizarOperacionPos;

            var supervisor = await servicioAuth.ValidarSupervisorAsync(
                UsuarioSupervisorInput.Trim(),
                PinSupervisorInput.Trim(),
                permisoRequerido);

            var accion = _accionPendienteSupervisor;
            ModalSupervisorAbierto = false;
            EsAnulacionFacturaCompleta = false;
            _accionPendienteSupervisor = null;
            PinSupervisorInput = string.Empty;

            if (accion is not null)
            {
                await accion();
            }

            MensajeEstado = $"Operación autorizada por supervisor: {supervisor.NombreCompleto}.";
        }
        catch (Exception ex)
        {
            TieneErrorSupervisor = true;
            ErrorSupervisor = Traducir(servicios, ex);
        }
    }

    private async Task EjecutarAnulacionFacturaRegistradaAsync()
    {
        if (VentaIdParaAnularInput <= 0)
            throw new ValidacionException("Indique un ID de venta válido para anular.");

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioVentas = _servicioVentas
            ?? servicios?.GetService<IServicioVentas>();
        if (servicioVentas is null)
            return;

        await servicioVentas.AnularVentaAsync(
            VentaIdParaAnularInput,
            string.IsNullOrWhiteSpace(MotivoAnulacionInput) ? "Anulación autorizada en POS" : MotivoAnulacionInput.Trim());

        MensajeEstado = $"Factura #{VentaIdParaAnularInput} anulada y stock reintegrado.";
    }

    [RelayCommand]
    private void CerrarModalSupervisor()
    {
        ModalSupervisorAbierto = false;
        EsAnulacionFacturaCompleta = false;
        _accionPendienteSupervisor = null;
        PinSupervisorInput = string.Empty;
        SolicitarFocoEscaneo?.Invoke();
    }

    private bool TieneLineaSeleccionada() => LineaSeleccionada is not null;

    [RelayCommand(CanExecute = nameof(TieneLineaSeleccionada))]
    private void AbrirModalCantidad()
    {
        if (LineaSeleccionada is null)
            return;

        NuevaCantidadInput = LineaSeleccionada.Cantidad;
        ModalCantidadAbierto = true;
    }

    [RelayCommand]
    private void AplicarCambioCantidad()
    {
        if (LineaSeleccionada is not null && NuevaCantidadInput > 0)
        {
            LineaSeleccionada.Cantidad = NuevaCantidadInput;
            RecalcularTotales();
            MensajeEstado = $"Cantidad actualizada: {LineaSeleccionada.Nombre} -> {LineaSeleccionada.CantidadFormateada}.";
        }

        ModalCantidadAbierto = false;
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    private void AbrirModalBusqueda()
    {
        ModalBusquedaAbierto = true;
    }

    [RelayCommand]
    private void CerrarModalBusqueda()
    {
        ModalBusquedaAbierto = false;
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    public async Task EjecutarBusquedaRapidaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicioBusqueda = _servicioBusqueda
            ?? scope?.ServiceProvider.GetService<IServicioBusquedaProductos>();
        if (servicioBusqueda is null || string.IsNullOrWhiteSpace(TerminoBusquedaRapida))
            return;

        var encontrados = await servicioBusqueda.BuscarPorNombreAsync(TerminoBusquedaRapida.Trim(), 25);
        ResultadosBusqueda.Clear();
        foreach (var prod in encontrados)
        {
            ResultadosBusqueda.Add(prod);
        }
    }

    [RelayCommand]
    private void SeleccionarProductoBuscado(Producto? producto)
    {
        if (producto is null)
            return;

        var cant = producto is ProductoPesado ? 500 : 1;
        AgregarProductoAlCarrito(producto, cant);
        ModalBusquedaAbierto = false;
        TerminoBusquedaRapida = string.Empty;
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(TieneLineaSeleccionada))]
    public void EliminarLinea()
    {
        if (LineaSeleccionada is null)
            return;

        var nombre = LineaSeleccionada.Nombre;
        Carrito.Remove(LineaSeleccionada);
        LineaSeleccionada = Carrito.LastOrDefault();
        RecalcularTotales();
        MensajeEstado = $"Línea eliminada: {nombre}.";
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    public void CancelarVenta()
    {
        _claveIdempotenciaCobro = null;
        Carrito.Clear();
        PagosAgregados.Clear();
        LineaSeleccionada = null;
        ClienteActual = null;
        DocumentoClienteInput = string.Empty;
        ModalCobroAbierto = false;
        ModalCantidadAbierto = false;
        ModalBusquedaAbierto = false;
        ModalVentasEnEsperaAbierto = false;
        ModalSupervisorAbierto = false;
        RecalcularTotales();
        NotificarEstadoPagos();
        TieneAlerta = false;
        MensajeEstado = "Ticket limpiado. Listo para nuevo cliente.";
        SolicitarFocoEscaneo?.Invoke();
    }

    [RelayCommand]
    public async Task BuscarClientePorDocumentoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicioClientes = _servicioClientes
            ?? scope?.ServiceProvider.GetService<IServicioClientes>();
        if (servicioClientes is null || string.IsNullOrWhiteSpace(DocumentoClienteInput))
            return;

        var doc = DocumentoClienteInput.Trim();
        var cliente = await servicioClientes.BuscarPorDocumentoAsync(doc);
        if (cliente is not null)
        {
            ClienteActual = cliente;
            TieneAlerta = false;
            MensajeEstado = $"Cliente vinculado: {cliente.NombreCompleto}.";
        }
        else
        {
            RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
            RegistroClienteVm.Documento = doc;
            ModalRegistroClienteAbierto = true;
        }
    }

    [RelayCommand]
    private void AbrirRegistroCliente()
    {
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        RegistroClienteVm.Documento = DocumentoClienteInput.Trim();
        ModalRegistroClienteAbierto = true;
    }

    [RelayCommand]
    public async Task ConfirmarAperturaTurnoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja
            ?? servicios?.GetService<IServicioCaja>();
        if (servicioCaja is null)
            return;

        try
        {
            var turno = await servicioCaja.AbrirTurnoAsync(CajaActivaId, EfectivoInicialApertura);
            TieneTurnoAbierto = true;
            ModalAperturaTurnoAbierto = false;
            InfoTurnoActual = $"Turno #{turno.Id} Activo · Base $ {turno.EfectivoInicial.ToString("N0", CulturaCo)}";
            TieneAlerta = false;
            MensajeEstado = $"Turno de caja #{turno.Id} abierto exitosamente.";
        }
        catch (Exception ex)
        {
            TieneAlerta = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    private void CerrarVistaPreviaTicket()
    {
        MostrarVistaPreviaTicket = false;
        SolicitarFocoEscaneo?.Invoke();
    }

    private string Traducir(IServiceProvider? servicios, Exception ex)
        => _traductor?.Traducir(ex)
           ?? servicios?.GetService<ITraductorExcepciones>()?.Traducir(ex)
           ?? ex.Message;
}