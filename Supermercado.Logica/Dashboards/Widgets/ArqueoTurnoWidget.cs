using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Dashboards.Widgets;

public class ArqueoTurnoWidget(
    ITurnoCajaRepositorio turnos,
    IVentaRepositorio ventas,
    ISesionActual sesion) : IWidget
{
    public string Id => "caja.arqueo_turno";
    public string Titulo => "Arqueo del turno";
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(1);
    public Permiso PermisoRequerido => Permiso.AbrirCerrarTurno;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        if (!sesion.EstaAutenticada)
            return new DatoWidget([new PuntoDato("Sin sesión", 0)]);

        var turno = await turnos
            .ObtenerAbiertoPorEmpleadoAsync(sesion.EmpleadoId);

        if (turno is null)
            return new DatoWidget([new PuntoDato("Sin turno abierto", 0)]);

        var delTurno = await ventas.ObtenerPorTurnoAsync(turno.Id);
        var esperado = turno.CalcularEfectivoEsperado(delTurno);

        return new DatoWidget(
        [
            new PuntoDato("Fondo inicial", turno.EfectivoInicial),
            new PuntoDato("Efectivo esperado", esperado)
        ]);
    }
}
