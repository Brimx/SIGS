using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Ventas;
using Supermercado.GUI.Servicios.Impresion;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Finanzas;
using Supermercado.Logica.Reportes;

namespace Supermercado.GUI.ViewModels;

public sealed class ResumenIvaTarifaItemViewModel
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public string CodigoTarifa { get; init; } = string.Empty;
    public string DescripcionTarifa { get; init; } = string.Empty;
    public int PuntosBasicos { get; init; }
    public int CantidadOperaciones { get; init; }
    public long BaseGravable { get; init; }
    public long ImpuestoGenerado { get; init; }
    public long TotalFacturado { get; init; }

    public string BaseFormateada => $"$ {BaseGravable.ToString("N0", CulturaCo)}";
    public string ImpuestoFormateado => $"$ {ImpuestoGenerado.ToString("N0", CulturaCo)}";
    public string TotalFormateado => $"$ {TotalFacturado.ToString("N0", CulturaCo)}";
}

public sealed class ArqueoFiscalItemViewModel
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public TurnoCaja Turno { get; init; } = null!;
    public int Id => Turno.Id;
    public int CajaId => Turno.CajaId;
    public string AperturaTexto => Turno.FechaApertura.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string CierreTexto => Turno.FechaCierre?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "ABIERTO";
    public string BaseFormateada => $"$ {Turno.EfectivoInicial.ToString("N0", CulturaCo)}";
    public string EsperadoFormateado => Turno.EfectivoEsperado.HasValue
        ? $"$ {Turno.EfectivoEsperado.Value.ToString("N0", CulturaCo)}"
        : "-";
    public string ContadoFormateado => Turno.EfectivoContado.HasValue
        ? $"$ {Turno.EfectivoContado.Value.ToString("N0", CulturaCo)}"
        : "-";
    public string DiferenciaFormateada => Turno.Diferencia.HasValue
        ? $"$ {Turno.Diferencia.Value.ToString("N0", CulturaCo)}"
        : "$ 0";
}

public partial class ContabilidadDianViewModel : ViewModelBase
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    private readonly IServicioFinanzas? _servicioFinanzas;
    private readonly IServicioReportes? _servicioReportes;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    private IReadOnlyList<Venta> _ultimasVentasConsultadas = [];

    public ObservableCollection<ResumenIvaTarifaItemViewModel> DesgloseIvaTarifas { get; } = [];
    public ObservableCollection<ArqueoFiscalItemViewModel> HistorialArqueos { get; } = [];

    [ObservableProperty]
    public partial int DiasRangoConsulta { get; set; } = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IngresosTotalesFormateado))]
    public partial long IngresosTotales { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EgresosTotalesFormateado))]
    public partial long EgresosTotales { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IvaTotalGeneradoFormateado))]
    public partial long IvaTotalGenerado { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MargenOperativoFormateado))]
    public partial long MargenOperativo { get; set; }

    [ObservableProperty]
    public partial string DescripcionGastoInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial long MontoGastoInput { get; set; } = 50000;

    [ObservableProperty]
    public partial string UltimaRutaExportada { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Reporte fiscal DIAN, discriminación de tarifas IVA (0%, 5%, 19%) y exportación contable.";

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    public string IngresosTotalesFormateado => $"$ {IngresosTotales.ToString("N0", CulturaCo)}";
    public string EgresosTotalesFormateado => $"$ {EgresosTotales.ToString("N0", CulturaCo)}";
    public string IvaTotalGeneradoFormateado => $"$ {IvaTotalGenerado.ToString("N0", CulturaCo)}";
    public string MargenOperativoFormateado => $"$ {MargenOperativo.ToString("N0", CulturaCo)}";

    public ContabilidadDianViewModel()
    {
    }

    public ContabilidadDianViewModel(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public ContabilidadDianViewModel(
        IServicioFinanzas servicioFinanzas,
        IServicioReportes? servicioReportes = null,
        ITraductorExcepciones? traductor = null)
    {
        _servicioFinanzas = servicioFinanzas;
        _servicioReportes = servicioReportes;
        _traductor = traductor;
    }

    [RelayCommand]
    public async Task ActualizarConsolidadoFiscalAsync()
    {
        if (Cargando)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var finanzas = _servicioFinanzas ?? servicios?.GetService<IServicioFinanzas>();
        var repoVentas = servicios?.GetService<IVentaRepositorio>();
        var repoTurnos = servicios?.GetService<ITurnoCajaRepositorio>();

        Cargando = true;
        TieneError = false;
        try
        {
            var hasta = DateTime.UtcNow.AddDays(1);
            var desde = DateTime.UtcNow.AddDays(-Math.Max(1, DiasRangoConsulta));

            if (finanzas is not null)
            {
                var resumen = await finanzas.ObtenerResumenAsync(desde, hasta);
                IngresosTotales = resumen.Ingresos;
                EgresosTotales = resumen.Egresos;
                IvaTotalGenerado = resumen.IvaGenerado;
                MargenOperativo = resumen.Margen;
            }

            if (repoVentas is not null)
            {
                var ventas = (await repoVentas.ObtenerPorRangoAsync(desde, hasta))
                    .Where(v => v.Estado == EstadoVenta.Completada)
                    .ToList();
                _ultimasVentasConsultadas = ventas;
                ConstruirTablaTarifasIva(ventas);
            }

            if (repoTurnos is not null)
            {
                var turnos = await repoTurnos.ObtenerTodosAsync();
                HistorialArqueos.Clear();
                foreach (var t in turnos.OrderByDescending(x => x.FechaApertura).Take(30))
                {
                    HistorialArqueos.Add(new ArqueoFiscalItemViewModel { Turno = t });
                }
            }

            MensajeEstado = $"Consolidado fiscal calculado para los últimos {DiasRangoConsulta} días ({_ultimasVentasConsultadas.Count} facturas POS).";
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

    private void ConstruirTablaTarifasIva(IReadOnlyList<Venta> ventas)
    {
        var detalles = ventas.SelectMany(v => v.Detalles).ToList();

        DesgloseIvaTarifas.Clear();
        AgregarFilaTarifa(detalles, 0, "C", "Excluido / Exento (0%)");
        AgregarFilaTarifa(detalles, 500, "M", "Bienes / Canasta Básica (5%)");
        AgregarFilaTarifa(detalles, 1900, "J", "Tarifa General IVA (19%)");
    }

    private void AgregarFilaTarifa(List<DetalleVenta> detalles, int puntosBasicos, string codigo, string descripcion)
    {
        var grupo = detalles.Where(d => d.PuntosBasicosIva == puntosBasicos).ToList();
        var total = grupo.Sum(d => d.Subtotal);
        var iva = grupo.Sum(d => d.Iva);
        var baseGravable = Math.Max(0, total - iva);

        DesgloseIvaTarifas.Add(new ResumenIvaTarifaItemViewModel
        {
            CodigoTarifa = codigo,
            DescripcionTarifa = descripcion,
            PuntosBasicos = puntosBasicos,
            CantidadOperaciones = grupo.Count,
            BaseGravable = baseGravable,
            ImpuestoGenerado = iva,
            TotalFacturado = total
        });
    }

    [RelayCommand]
    public async Task RegistrarGastoOperativoAsync()
    {
        if (string.IsNullOrWhiteSpace(DescripcionGastoInput) || MontoGastoInput <= 0)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var finanzas = _servicioFinanzas ?? servicios?.GetService<IServicioFinanzas>();
        if (finanzas is null)
            return;

        TieneError = false;
        try
        {
            await finanzas.RegistrarGastoAsync(DescripcionGastoInput.Trim(), MontoGastoInput);
            DescripcionGastoInput = string.Empty;
            await ActualizarConsolidadoFiscalAsync();
            MensajeEstado = $"Gasto operativo por $ {MontoGastoInput.ToString("N0", CulturaCo)} registrado y descontado del margen.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task ExportarCsvContableAsync()
    {
        TieneError = false;
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("SECCION;CODIGO_TARIFA;DESCRIPCION;CANTIDAD_ITEMS;BASE_GRAVABLE_COP;IVA_GENERADO_COP;TOTAL_FACTURADO_COP");

            foreach (var fila in DesgloseIvaTarifas)
            {
                sb.AppendLine($"IVA_DIAN;{fila.CodigoTarifa};{fila.DescripcionTarifa};{fila.CantidadOperaciones};{fila.BaseGravable};{fila.ImpuestoGenerado};{fila.TotalFacturado}");
            }

            sb.AppendLine();
            sb.AppendLine("SECCION;TURNO_ID;CAJA_ID;APERTURA;CIERRE;BASE_INICIAL;EFECTIVO_ESPERADO;EFECTIVO_CONTADO;DIFERENCIA");
            foreach (var arq in HistorialArqueos)
            {
                var t = arq.Turno;
                sb.AppendLine($"ARQUEO_Z;{t.Id};{t.CajaId};{arq.AperturaTexto};{arq.CierreTexto};{t.EfectivoInicial};{t.EfectivoEsperado ?? 0};{t.EfectivoContado ?? 0};{t.Diferencia ?? 0}");
            }

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Exportaciones_SIGS");
            Directory.CreateDirectory(carpeta);

            var ruta = Path.Combine(carpeta, $"Declaracion_Fiscal_DIAN_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            await File.WriteAllTextAsync(ruta, sb.ToString(), Encoding.UTF8);

            UltimaRutaExportada = ruta;
            MensajeEstado = $"Archivo contable CSV exportado en: {ruta}";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = ex.Message;
        }
    }

    [RelayCommand]
    public async Task ExportarPdfVentasDianAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioReportes = _servicioReportes ?? servicios?.GetService<IServicioReportes>();
        if (servicioReportes is null)
            return;

        TieneError = false;
        try
        {
            var hasta = DateTime.UtcNow;
            var desde = DateTime.UtcNow.AddDays(-Math.Max(1, DiasRangoConsulta));
            var bytesPdf = await servicioReportes.GenerarReporteVentasAsync(desde, hasta, _ultimasVentasConsultadas);

            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Exportaciones_SIGS");
            Directory.CreateDirectory(carpeta);

            var ruta = Path.Combine(carpeta, $"Reporte_Fiscal_Ventas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(ruta, bytesPdf);

            UltimaRutaExportada = ruta;
            MensajeEstado = $"Reporte PDF fiscal generado en: {ruta}";
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