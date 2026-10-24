using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Dashboards.Widgets;

public class TrmDelDiaWidget(IServicioTrm trm) : IWidget
{
    public string Id => "externo.trm";
    public string Titulo => "TRM del día";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromHours(1);
    public Permiso PermisoRequerido => Permiso.Ninguno;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var valor = await trm.ObtenerAsync();

        return valor is null
            ? new DatoWidget([])
            : new DatoWidget([new PuntoDato("TRM (centavos)", valor.Centavos)]);
    }
}
