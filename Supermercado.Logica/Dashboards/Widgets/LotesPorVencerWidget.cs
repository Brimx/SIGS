using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;

namespace Supermercado.Logica.Dashboards.Widgets;

public class LotesPorVencerWidget(
    IServicioAlertasInventario alertas) : IWidget
{
    public string Id => "inventario.lotes_por_vencer";
    public string Titulo => "Lotes por vencer";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var lotes = await alertas.ObtenerLotesPorVencerAsync();
        return new DatoWidget(
        [
            new PuntoDato("Lotes en riesgo", lotes.Count)
        ]);
    }
}
