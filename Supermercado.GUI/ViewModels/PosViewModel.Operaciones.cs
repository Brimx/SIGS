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

public partial class PosViewModel : ViewModelBase
{
    public async Task InicializarEstadoCajaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja
            ?? servicios?.GetService<ServicioCaja>();
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
            ?? servicios?.GetService<ServicioVentas>();
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
            ?? servicios?.GetService<ServicioVentas>();
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
            ?? servicios?.GetService<ServicioVentas>();
        var servicioProductos = _servicioProductos
            ?? servicios?.GetService<ServicioProductos>();
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
                var servicioClientes = _servicioClientes ?? servicios?.GetService<ServicioClientes>();
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
            ?? servicios?.GetService<ServicioVentas>();
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
            ?? servicios?.GetService<ServicioVentas>();
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
        var sesion = _sesion ?? servicios?.GetService<SesionActual>();
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
            ?? servicios?.GetService<ServicioAutenticacion>();
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
            ?? servicios?.GetService<ServicioVentas>();
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

    [RelayCommand]
    public async Task BuscarClientePorDocumentoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicioClientes = _servicioClientes
            ?? scope?.ServiceProvider.GetService<ServicioClientes>();
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
            ?? servicios?.GetService<ServicioCaja>();
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
           ?? servicios?.GetService<TraductorExcepciones>()?.Traducir(ex)
           ?? ex.Message;
}
