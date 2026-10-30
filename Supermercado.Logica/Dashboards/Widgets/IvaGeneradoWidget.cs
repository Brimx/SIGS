using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Dashboards.Widgets;

public class IvaGeneradoWidget(
    IVentaRepositorio ventas,
    TimeProvider reloj) : IWidget
{
    public string Id => "finanzas.iva_generado";
    public string Titulo => "IVA generado";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(2);
    public Permiso PermisoRequerido => Permiso.VerFinanzas;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        var totalIva = completadas.Sum(v => v.TotalIva);

        return new DatoWidget(
        [
            new PuntoDato("IVA generado hoy", totalIva)
        ]);
    }
}
