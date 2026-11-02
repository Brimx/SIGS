using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Dian;
using Supermercado.Logica.Ventas;

namespace Supermercado.GUI.ViewModels;

public partial class CajaTurnoViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    private readonly IServicioCaja? _servicioCaja;
    private readonly IServicioDian? _servicioDian;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

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

    public CajaTurnoViewModel()
    {
    }

    public CajaTurnoViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public CajaTurnoViewModel(
        IServicioCaja servicioCaja,
        IServicioDian? servicioDian = null,
        ITraductorExcepciones? traductor = null)
    {
        _servicioCaja = servicioCaja;
        _servicioDian = servicioDian;
        _traductor = traductor;
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
                MensajeEstado = $"Turno #{turno.Id} abierto y operativo.";
            }
            else
            {
                TurnoActualId = 0;
                EfectivoInicialActual = 0;
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
