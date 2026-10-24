using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Dashboards.Widgets;

public class ReabastecimientosPendientesWidget(
    IReabastecimientoRepositorio solicitudes) : IWidget
{
    public string Id => "inventario.reabastecimientos";
    public string Titulo => "Reabastecimientos pendientes";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var pendientes = await solicitudes.ObtenerPendientesAsync();
        return new DatoWidget([new PuntoDato("Pendientes", pendientes.Count)]);
    }
}
