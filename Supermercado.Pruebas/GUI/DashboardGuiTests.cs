using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Dashboards;

namespace Supermercado.Pruebas.GUI;

public class DashboardGuiTests
{
    private class WidgetFalso(string id, string titulo) : IWidget
    {
        public string Id => id;
        public string Titulo => titulo;
        public string Grupo => GruposWidget.Ventas;
        public TamanoWidget Tamano => TamanoWidget.Mediano;
        public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
        public Permiso PermisoRequerido => Permiso.Ninguno;

        public Task<DatoWidget> ObtenerDatosAsync()
            => Task.FromResult(new DatoWidget([]));
    }

    [Fact]
    public void Tarjeta_UnPunto_MuestraCifraYEtiqueta()
    {
        var item = new WidgetEnPanel(
            new WidgetFalso("ventas.del_dia", "Ventas del día"),
            TamanoWidget.Pequeno,
            "Ventas del día");

        var tarjeta = WidgetCardViewModel.Desde(
            item,
            new DatoWidget([new PuntoDato("Total vendido", 1234567, FormatoDato.Moneda)]));

        Assert.False(tarjeta.TieneVariasFilas);
        Assert.Equal("Total vendido", tarjeta.Subtitulo);
        Assert.Contains("1", tarjeta.ValorPrincipal);
        Assert.Contains("$", tarjeta.ValorPrincipal);
        Assert.Equal(TamanoWidget.Pequeno, tarjeta.Tamano);
    }

    [Fact]
    public void Tarjeta_TrmEnCentavos_SeFormateaComoPesos()
    {
        var item = new WidgetEnPanel(
            new WidgetFalso("externo.trm", "TRM del día"),
            TamanoWidget.Pequeno,
            "TRM del día");

        var tarjeta = WidgetCardViewModel.Desde(
            item,
            new DatoWidget([new PuntoDato("TRM (centavos)", 412345, FormatoDato.MonedaEnCentavos)]));

        Assert.Contains("4", tarjeta.ValorPrincipal);
        Assert.Contains(",", tarjeta.ValorPrincipal);
    }

    [Fact]
    public void Tarjeta_VariosPuntos_GeneraBarrasProporcionales()
    {
        var item = new WidgetEnPanel(
            new WidgetFalso("reportes.ventas_categoria", "Ventas por categoría"),
            TamanoWidget.Grande,
            "Ventas por categoría");

        var tarjeta = WidgetCardViewModel.Desde(
            item,
            new DatoWidget(
            [
                new PuntoDato("Lácteos", 100, FormatoDato.Moneda),
                new PuntoDato("Aseo", 50, FormatoDato.Moneda)
            ]));

        Assert.True(tarjeta.TieneVariasFilas);
        Assert.Equal(2, tarjeta.Filas.Count);
        Assert.Equal(1, tarjeta.Filas[0].Fraccion);
        Assert.Equal(0.5, tarjeta.Filas[1].Fraccion);
        Assert.True(tarjeta.EsGrande);
        Assert.Equal(660, tarjeta.Ancho);
    }

    [Fact]
    public void Tarjeta_TituloPersonalizado_SeDistingueDelOriginal()
    {
        var item = new WidgetEnPanel(
            new WidgetFalso("ventas.del_dia", "Ventas del día"),
            TamanoWidget.Mediano,
            "Caja de hoy");

        var tarjeta = WidgetCardViewModel.Desde(item, new DatoWidget([]));

        Assert.Equal("Caja de hoy", tarjeta.Titulo);
        Assert.Equal("Ventas del día", tarjeta.TituloOriginal);
        Assert.Equal("Caja de hoy", tarjeta.TituloEditable);
    }

    [Fact]
    public void Tarjeta_SinPersonalizar_DejaElEditorVacio()
    {
        var item = new WidgetEnPanel(
            new WidgetFalso("ventas.del_dia", "Ventas del día"),
            TamanoWidget.Mediano,
            "Ventas del día");

        var tarjeta = WidgetCardViewModel.Desde(item, new DatoWidget([]));

        Assert.Equal("Ventas del día", tarjeta.Titulo);
        Assert.Empty(tarjeta.TituloEditable);
    }

    [Fact]
    public void Catalogo_MarcaDisponibilidadSegunElPanel()
    {
        var item = new WidgetCatalogoViewModel { Id = "w1", EnPanel = true };

        Assert.False(item.DisponibleParaAgregar);
        item.EnPanel = false;
        Assert.True(item.DisponibleParaAgregar);
    }
}
