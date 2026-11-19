namespace Supermercado.Pruebas.GUI;

public sealed class ComprasContabilidadGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    [Fact]
    public void Compras_UsaColumnasFlexiblesYListaVirtualizada()
    {
        var compras = Leer("Supermercado.GUI/Views/ComprasProveedoresView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"300\" />", compras);
        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"300\" />", compras);
        Assert.Contains("ScrollViewer Grid.Row=\"2\"", compras);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", compras);
        Assert.Contains("MinHeight=\"520\"", compras);
        Assert.Contains("MaxWidth=\"220\"", compras);
        Assert.Contains("VirtualizingStackPanel", compras);
        Assert.Contains("MinWidth=\"160\"", compras);
        Assert.Contains("ToolTip.Tip=\"{Binding NombreProducto}\"", compras);
        Assert.DoesNotContain("Text=\"{Binding MensajeEstado}\"", compras);
        Assert.DoesNotContain("ColumnDefinitions=\"*,16,460\"", compras);
        Assert.DoesNotContain("ItemsControl ItemsSource=\"{Binding SolicitudesPendientes}\"", compras);
    }

    [Fact]
    public void Contabilidad_UsaKPIsEnDosFilasYCamposFlexibles()
    {
        var contabilidad = Leer("Supermercado.GUI/Views/ContabilidadDianView.axaml");

        Assert.Contains("RowDefinitions=\"Auto,12,Auto\" ColumnDefinitions=\"*,12,*\"", contabilidad);
        Assert.Contains("Grid.Row=\"2\" Grid.Column=\"2\"", contabilidad);
        Assert.Contains("ScrollViewer Grid.Row=\"4\"", contabilidad);
        Assert.Contains("MinHeight=\"520\"", contabilidad);
        Assert.Contains("<WrapPanel>", contabilidad);
        Assert.Contains("MinWidth=\"130\"", contabilidad);
        Assert.Contains("MinWidth=\"560\"", contabilidad);
        Assert.Contains("TextWrapping=\"Wrap\"", contabilidad);
        Assert.Contains("ToolTip.Tip=\"{Binding IngresosTotalesFormateado}\"", contabilidad);
        Assert.DoesNotContain("Text=\"{Binding MensajeEstado}\"", contabilidad);
        Assert.DoesNotContain("ColumnDefinitions=\"*,16,Auto,8,110,12,Auto,8,Auto,8,Auto\"", contabilidad);
        Assert.DoesNotContain("ColumnDefinitions=\"*,14,*,14,*,14,*\"", contabilidad);
    }
}
