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
}
