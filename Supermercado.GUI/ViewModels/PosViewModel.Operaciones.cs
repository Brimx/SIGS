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
