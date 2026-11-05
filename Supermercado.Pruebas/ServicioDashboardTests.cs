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
        Permiso permiso = Permiso.Ninguno,
        string grupo = GruposWidget.Ventas,
        TamanoWidget tamano = TamanoWidget.Mediano) : IWidget
    {
        public string Id => id;
        public string Titulo => titulo;
        public string Grupo => grupo;
        public TamanoWidget Tamano => tamano;
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

    private static WidgetPrueba Ventas()
        => new("w1", "Ventas", Permiso.VerReportes);

    private static WidgetPrueba Stock()
        => new("w2", "Stock", Permiso.VerReportes, GruposWidget.Inventario);

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
    public void ObtenerDisponibles_OrdenaPorGrupoDelCatalogo()
    {
        var finanzas = new WidgetPrueba("a", "IVA", Permiso.VerReportes, GruposWidget.Finanzas);
        var inventario = new WidgetPrueba("b", "Stock", Permiso.VerReportes, GruposWidget.Inventario);

        var servicio = CrearServicio([finanzas, inventario]);

        Assert.Equal(["b", "a"], servicio.ObtenerDisponibles().Select(w => w.Id));
    }

    [Fact]
    public async Task ObtenerPanel_SinDisposicionGuardada_DevuelveTodosLosDisponibles()
    {
        var servicio = CrearServicio([Ventas(), Stock()]);

        var panel = await servicio.ObtenerPanelAsync();

        Assert.Equal(2, panel.Count);
        Assert.All(panel, item => Assert.Equal(TamanoWidget.Mediano, item.Tamano));
    }

    [Fact]
    public async Task AgregarAsync_ConPermiso_GuardaConfiguracion()
    {
        var servicio = CrearServicio([Ventas()]);

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
        var servicio = CrearServicio([Ventas()]);

        await servicio.AgregarAsync("w1");
        await servicio.QuitarAsync("w1");

        Assert.False(configuraciones.Datos[0].Visible);
        Assert.Equal(2, trabajo.Guardados);
    }

    [Fact]
    public async Task ReordenarAsync_ActualizaPosiciones()
    {
        var servicio = CrearServicio([Ventas(), Stock()]);

        await servicio.AgregarAsync("w1");
        await servicio.AgregarAsync("w2");

        await servicio.ReordenarAsync(["w2", "w1"]);

        var c1 = configuraciones.Datos.First(c => c.WidgetId == "w1");
        var c2 = configuraciones.Datos.First(c => c.WidgetId == "w2");

        Assert.Equal(1, c1.Posicion);
        Assert.Equal(0, c2.Posicion);
    }

    [Fact]
    public async Task CambiarTamanoAsync_ConConfiguracionNueva_NoLaMarcaComoModificada()
    {
        // Una configuración recién agregada ya está en estado Added: marcarla
        // como Modified hace que EF rechace la clave temporal. Regresión real
        // del tamaño y el renombrado del panel.
        var servicio = CrearServicio([Ventas()]);

        await servicio.CambiarTamanoAsync("w1", TamanoWidget.Grande);

        Assert.Equal(0, configuraciones.Actualizaciones);
        Assert.Single(configuraciones.Datos);
        Assert.Equal(TamanoWidget.Grande, configuraciones.Datos[0].Tamano);
    }

    [Fact]
    public async Task RenombrarAsync_ConConfiguracionNueva_NoLaMarcaComoModificada()
    {
        var servicio = CrearServicio([Ventas()]);

        await servicio.RenombrarAsync("w1", "Mi panel");

        Assert.Equal(0, configuraciones.Actualizaciones);
        Assert.Equal("Mi panel", configuraciones.Datos[0].TituloPersonalizado);
    }

    [Fact]
    public async Task CambiarTamanoAsync_PersisteYApareceEnElPanel()
    {
        var servicio = CrearServicio([Ventas()]);

        await servicio.CambiarTamanoAsync("w1", TamanoWidget.Grande);

        var panel = await servicio.ObtenerPanelAsync();
        Assert.Equal(TamanoWidget.Grande, Assert.Single(panel).Tamano);
        Assert.True(configuraciones.Datos[0].Visible);
    }

    [Fact]
    public async Task CambiarTamanoAsync_WidgetNoDisponible_LanzaValidacionException()
    {
        var servicio = CrearServicio([Ventas()]);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarTamanoAsync("otro", TamanoWidget.Grande));
    }

    [Fact]
    public async Task RenombrarAsync_GuardaTituloRecortado()
    {
        var servicio = CrearServicio([Ventas()]);
        var largo = new string('a', 80);

        await servicio.RenombrarAsync("w1", largo);

        var panel = await servicio.ObtenerPanelAsync();
        Assert.Equal(60, Assert.Single(panel).Titulo.Length);
    }

    [Fact]
    public async Task RenombrarAsync_TituloVacio_RestauraElDelWidget()
    {
        var servicio = CrearServicio([Ventas()]);

        await servicio.RenombrarAsync("w1", "Mi panel");
        await servicio.RenombrarAsync("w1", "   ");

        var panel = await servicio.ObtenerPanelAsync();
        Assert.Equal("Ventas", Assert.Single(panel).Titulo);
        Assert.Null(configuraciones.Datos[0].TituloPersonalizado);
    }

    [Fact]
    public async Task RestablecerAsync_VuelveALaDisposicionDelRol()
    {
        var servicio = CrearServicio([Ventas(), Stock()]);

        await servicio.AgregarAsync("w2");
        await servicio.CambiarTamanoAsync("w2", TamanoWidget.Grande);
        await servicio.RenombrarAsync("w2", "Mi stock");

        await servicio.RestablecerAsync();

        var panel = await servicio.ObtenerPanelAsync();
        var stock = panel.First(w => w.Id == "w2");

        Assert.Equal(2, panel.Count);
        Assert.Equal(["w1", "w2"], panel.Select(w => w.Id));
        Assert.Equal(TamanoWidget.Mediano, stock.Tamano);
        Assert.Equal("Stock", stock.Titulo);
        Assert.DoesNotContain(configuraciones.Datos, c => !c.Visible);
    }

    [Fact]
    public async Task ExportarAsync_SinDisposicionGuardada_ExportaLosDisponibles()
    {
        var servicio = CrearServicio([Ventas(), Stock()]);

        var json = await servicio.ExportarAsync();

        var disposicion = DisposicionPanelJson.Deserializar(json);
        Assert.Equal(DisposicionPanel.VersionActual, disposicion.Version);
        Assert.Equal(["w1", "w2"], disposicion.Widgets.Select(w => w.WidgetId));
    }

    [Fact]
    public async Task ExportarEImportar_ConservaOrdenTamanoTituloYVisibilidad()
    {
        var origen = CrearServicio([Ventas(), Stock()]);
        await origen.AgregarAsync("w1");
        await origen.AgregarAsync("w2");
        await origen.ReordenarAsync(["w2", "w1"]);
        await origen.CambiarTamanoAsync("w2", TamanoWidget.Grande);
        await origen.RenombrarAsync("w2", "Stock crítico");

        var json = await origen.ExportarAsync();

        // Otro empleado importa el mismo archivo
        var configuracionesDestino = new ConfiguracionesWidgetEnMemoria();
        var destino = new ServicioDashboard(
            [Ventas(), Stock()],
            configuracionesDestino,
            Sesiones.Con(Permiso.VerReportes),
            new UnidadDeTrabajoFalsa());

        var resultado = await destino.ImportarAsync(json);
        var panel = await destino.ObtenerPanelAsync();

        Assert.Equal(2, resultado.Aplicados);
        Assert.Empty(resultado.Omitidos);
        Assert.Equal(["w2", "w1"], panel.Select(w => w.Id));
        Assert.Equal(TamanoWidget.Grande, panel[0].Tamano);
        Assert.Equal("Stock crítico", panel[0].Titulo);
    }

    [Fact]
    public async Task ImportarAsync_OmiteWidgetsQueElRolNoPuedeVer()
    {
        var origen = CrearServicio(
            [Ventas(), new WidgetPrueba("fin", "IVA", Permiso.VerFinanzas, GruposWidget.Finanzas)],
            Permiso.VerReportes | Permiso.VerFinanzas);
        await origen.AgregarAsync("w1");
        await origen.AgregarAsync("fin");

        var json = await origen.ExportarAsync();

        var destino = CrearServicio([Ventas()], Permiso.VerReportes);
        var resultado = await destino.ImportarAsync(json);
        var panel = await destino.ObtenerPanelAsync();

        Assert.Equal(1, resultado.Aplicados);
        Assert.Equal(["fin"], resultado.Omitidos);
        Assert.Equal("w1", Assert.Single(panel).Id);
    }

    [Fact]
    public async Task ImportarAsync_ReemplazaLaDisposicionAnterior()
    {
        var servicio = CrearServicio([Ventas(), Stock()]);
        await servicio.AgregarAsync("w1");
        await servicio.AgregarAsync("w2");

        var json = DisposicionPanelJson.Serializar(new DisposicionPanel(
            DisposicionPanel.VersionActual,
            [new EntradaDisposicion("w2", 0, true, TamanoWidget.Pequeno, null)]));

        await servicio.ImportarAsync(json);

        var panel = await servicio.ObtenerPanelAsync();
        Assert.Equal("w2", Assert.Single(panel).Id);
        Assert.False(configuraciones.Datos.First(c => c.WidgetId == "w1").Visible);
    }

    [Fact]
    public async Task ImportarAsync_JsonInvalido_LanzaValidacionException()
    {
        var servicio = CrearServicio([Ventas()]);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.ImportarAsync("{ esto no es json"));
    }

    [Fact]
    public async Task ImportarAsync_VersionDesconocida_LanzaValidacionException()
    {
        var servicio = CrearServicio([Ventas()]);
        var json = DisposicionPanelJson.Serializar(new DisposicionPanel(
            DisposicionPanel.VersionActual + 1, []));

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.ImportarAsync(json));
    }
}
