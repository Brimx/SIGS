using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Dashboards.Widgets;

public class VentasDelDiaWidget(
    IVentaRepositorio ventas,
    TimeProvider reloj) : IWidget
{
    public string Id => "ventas.del_dia";
    public string Titulo => "Ventas del día";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(1);
    public Permiso PermisoRequerido => Permiso.VerReportes;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        return new DatoWidget(
        [
            new PuntoDato("Total vendido", completadas.Sum(v => v.Total)),
            new PuntoDato("Tickets", completadas.Count)
        ]);
    }
}
