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
}
