using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;

namespace Supermercado.Logica.Dashboards.Widgets;

public class StockBajoMinimoWidget(
    IServicioAlertasInventario alertas) : IWidget
{
    public string Id => "inventario.stock_bajo_minimo";
    public string Titulo => "Stock bajo el mínimo";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(3);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var deficit = await alertas.ObtenerBajoMinimoAsync();
        return new DatoWidget(
        [
            new PuntoDato("Productos bajo mínimo", deficit.Count)
        ]);
    }
}
