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
            ?? scope?.ServiceProvider.GetService<ServicioProductos>();
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
}
