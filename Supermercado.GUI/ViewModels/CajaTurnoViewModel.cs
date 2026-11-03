using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Dian;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;

namespace Supermercado.GUI.ViewModels;

public partial class CajaTurnoViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    private readonly IServicioCaja? _servicioCaja;
    private readonly IServicioDian? _servicioDian;
    private readonly IServicioAutenticacion? _servicioAutenticacion;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ObservableCollection<RetiroCaja> RetirosTurnoActual { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiereAperturaConBase))]
    public partial bool TurnoAbierto { get; set; }

    [ObservableProperty]
    public partial int TurnoActualId { get; set; }

    [ObservableProperty]
    public partial long EfectivoInicialActual { get; set; }

    [ObservableProperty]
    public partial string FechaAperturaTexto { get; set; } = "-";

    [ObservableProperty]
    public partial int CajaSeleccionadaId { get; set; } = 1;

    [ObservableProperty]
    public partial long EfectivoInicialInput { get; set; } = 100000;

    [ObservableProperty]
    public partial long EfectivoContadoInput { get; set; }

    // Campos de Retiro Parcial (Recogida a Caja Fuerte)
    [ObservableProperty]
    public partial long MontoRetiroInput { get; set; } = 50000;

    [ObservableProperty]
    public partial string MotivoRetiroInput { get; set; } = "Traslado parcial a caja fuerte";

    [ObservableProperty]
    public partial string UsuarioSupervisorRetiroInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PinSupervisorRetiroInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalRetirosFormateado))]
    public partial long TotalRetirosTurnoActual { get; set; }

    [ObservableProperty]
    public partial ResultadoArqueo? UltimoArqueo { get; set; }

    [ObservableProperty]
    public partial EstadoResolucionDian? EstadoDian { get; set; }

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    public bool RequiereAperturaConBase => !TurnoAbierto;

    public string EfectivoInicialFormateado => $"$ {EfectivoInicialActual.ToString("N0", CulturaCo)}";
    public string TotalRetirosFormateado => $"$ {TotalRetirosTurnoActual.ToString("N0", CulturaCo)}";

    public CajaTurnoViewModel()
    {
    }

    public CajaTurnoViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public CajaTurnoViewModel(
        IServicioCaja servicioCaja,
        IServicioDian? servicioDian = null,
        ITraductorExcepciones? traductor = null,
        IServicioAutenticacion? servicioAutenticacion = null)
    {
        _servicioCaja = servicioCaja;
        _servicioDian = servicioDian;
        _traductor = traductor;
        _servicioAutenticacion = servicioAutenticacion;
    }

    [RelayCommand]
    public async Task VerificarEstadoCajaAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja ?? servicios?.GetService<IServicioCaja>();
        var servicioDian = _servicioDian ?? servicios?.GetService<IServicioDian>();
        if (servicioCaja is null)
            return;

        TieneError = false;
        try
        {
            var turno = await servicioCaja.ObtenerTurnoActualAsync();
            TurnoAbierto = turno is not null;
            if (turno is not null)
            {
                TurnoActualId = turno.Id;
                EfectivoInicialActual = turno.EfectivoInicial;
                FechaAperturaTexto = turno.FechaApertura.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                await CargarRetirosDelTurnoAsync(servicioCaja);
                MensajeEstado = $"Turno #{turno.Id} abierto y operativo.";
            }
            else
            {
                TurnoActualId = 0;
                EfectivoInicialActual = 0;
                TotalRetirosTurnoActual = 0;
                RetirosTurnoActual.Clear();
                FechaAperturaTexto = "Sin turno activo";
                MensajeEstado = "La caja se encuentra cerrada. Debe realizar la apertura con base inicial.";
            }

            if (servicioDian is not null)
            {
                EstadoDian = await servicioDian.ConsultarEstadoResolucionAsync();
            }

            OnPropertyChanged(nameof(EfectivoInicialFormateado));
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private async Task CargarRetirosDelTurnoAsync(IServicioCaja servicioCaja)
    {
        var retiros = await servicioCaja.ObtenerRetirosTurnoActualAsync();
        RetirosTurnoActual.Clear();
        long acumulado = 0;
        foreach (var r in retiros)
        {
            RetirosTurnoActual.Add(r);
            acumulado += r.Monto;
        }
        TotalRetirosTurnoActual = acumulado;
    }

    [RelayCommand]
    public async Task AbrirTurnoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja ?? servicios?.GetService<IServicioCaja>();
        if (servicioCaja is null)
            return;

        TieneError = false;
        try
        {
            var turno = await servicioCaja.AbrirTurnoAsync(CajaSeleccionadaId, EfectivoInicialInput);
            TurnoAbierto = true;
            TurnoActualId = turno.Id;
            EfectivoInicialActual = turno.EfectivoInicial;
            TotalRetirosTurnoActual = 0;
            RetirosTurnoActual.Clear();
            FechaAperturaTexto = turno.FechaApertura.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            UltimoArqueo = null;
            MensajeEstado = $"Turno #{turno.Id} iniciado con base de $ {turno.EfectivoInicial.ToString("N0", CulturaCo)}.";
            OnPropertyChanged(nameof(EfectivoInicialFormateado));
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task RegistrarRetiroEfectivoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja ?? servicios?.GetService<IServicioCaja>();
        var servicioAuth = _servicioAutenticacion ?? servicios?.GetService<IServicioAutenticacion>();
        if (servicioCaja is null)
            return;

        TieneError = false;
        try
        {
            var supervisorId = 1;
            var nombreSupervisor = "Supervisor";

            if (servicioAuth is not null)
            {
                var supervisor = await servicioAuth.ValidarSupervisorAsync(
                    UsuarioSupervisorRetiroInput.Trim(),
                    PinSupervisorRetiroInput.Trim(),
                    Permiso.AutorizarOperacionPos);
                supervisorId = supervisor.Id;
                nombreSupervisor = supervisor.NombreCompleto;
            }

            var retiro = await servicioCaja.RegistrarRetiroAsync(
                MontoRetiroInput,
                MotivoRetiroInput.Trim(),
                supervisorId);

            PinSupervisorRetiroInput = string.Empty;
            await CargarRetirosDelTurnoAsync(servicioCaja);
            MensajeEstado = $"Retiro de $ {retiro.Monto.ToString("N0", CulturaCo)} registrado y autorizado por {nombreSupervisor}.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task CerrarTurnoConArqueoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioCaja = _servicioCaja ?? servicios?.GetService<IServicioCaja>();
        if (servicioCaja is null)
            return;

        TieneError = false;
        try
        {
            var resultado = await servicioCaja.CerrarTurnoAsync(EfectivoContadoInput);
            UltimoArqueo = resultado;
            TurnoAbierto = false;
            MensajeEstado = resultado.DentroDeTolerancia
                ? $"Cierre Z completado dentro de tolerancia (Diferencia: $ {resultado.Diferencia.ToString("N0", CulturaCo)})."
                : $"Cierre Z con descuadre fuera de tolerancia (Diferencia: $ {resultado.Diferencia.ToString("N0", CulturaCo)}). Auditado.";
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