using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Dashboards;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioDashboardTests
{
    private class WidgetPrueba(
        string id,
        string titulo,
        Permiso permiso = Permiso.Ninguno) : IWidget
    {
        public string Id => id;
        public string Titulo => titulo;
        public TamanoWidget Tamano => TamanoWidget.Mediano;
        public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
        public Permiso PermisoRequerido => permiso;

        public Task<DatoWidget> ObtenerDatosAsync()
            => Task.FromResult(new DatoWidget([]));
    }

    private readonly ConfiguracionesWidgetEnMemoria configuraciones = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioDashboard CrearServicio(
        IEnumerable<IWidget> widgets,
        Permiso permisos = Permiso.VerReportes)
        => new(
            widgets,
            configuraciones,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public void ObtenerDisponibles_FiltraPorPermisosDeLaSesion()
    {
        var w1 = new WidgetPrueba("w1", "Ventas", Permiso.VerReportes);
        var w2 = new WidgetPrueba("w2", "Finanzas", Permiso.VerFinanzas);

        var servicio = CrearServicio([w1, w2], Permiso.VerReportes);

        var disponibles = servicio.ObtenerDisponibles();

        var item = Assert.Single(disponibles);
        Assert.Equal("w1", item.Id);
    }

    [Fact]
    public async Task AgregarAsync_ConPermiso_GuardaConfiguracion()
    {
        var w1 = new WidgetPrueba("w1", "Ventas", Permiso.VerReportes);
        var servicio = CrearServicio([w1], Permiso.VerReportes);

        await servicio.AgregarAsync("w1");

        Assert.Single(configuraciones.Datos);
        Assert.True(configuraciones.Datos[0].Visible);
        Assert.Equal("w1", configuraciones.Datos[0].WidgetId);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task AgregarAsync_WidgetNoDisponible_LanzaValidacionException()
    {
        var w1 = new WidgetPrueba("w1", "Finanzas", Permiso.VerFinanzas);
        // No tiene VerFinanzas
        var servicio = CrearServicio([w1], Permiso.VerReportes);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.AgregarAsync("w1"));
    }

    [Fact]
    public async Task QuitarAsync_OcultaElWidget()
    {
        var w1 = new WidgetPrueba("w1", "Ventas", Permiso.VerReportes);
        var servicio = CrearServicio([w1], Permiso.VerReportes);

        await servicio.AgregarAsync("w1");
        await servicio.QuitarAsync("w1");

        Assert.False(configuraciones.Datos[0].Visible);
        Assert.Equal(2, trabajo.Guardados);
    }

    [Fact]
    public async Task ReordenarAsync_ActualizaPosiciones()
    {
        var w1 = new WidgetPrueba("w1", "Ventas", Permiso.VerReportes);
        var w2 = new WidgetPrueba("w2", "Stock", Permiso.VerReportes);
        var servicio = CrearServicio([w1, w2], Permiso.VerReportes);

        await servicio.AgregarAsync("w1");
        await servicio.AgregarAsync("w2");

        await servicio.ReordenarAsync(["w2", "w1"]);

        var c1 = configuraciones.Datos.First(c => c.WidgetId == "w1");
        var c2 = configuraciones.Datos.First(c => c.WidgetId == "w2");

        Assert.Equal(1, c1.Posicion);
        Assert.Equal(0, c2.Posicion);
    }
}
