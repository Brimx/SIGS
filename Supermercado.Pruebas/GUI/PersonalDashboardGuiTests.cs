namespace Supermercado.Pruebas.GUI;

public sealed class PersonalDashboardGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    [Fact]
    public void Personal_UsaWrapPanelYListaDePermisosVirtualizada()
    {
        var personal = Leer("Supermercado.GUI/Views/PersonalRolesView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"300\" />", personal);
        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"280\" />", personal);
        Assert.Contains("VirtualizingStackPanel", personal);
        Assert.Contains("MinWidth=\"150\"", personal);
        Assert.Contains("MinWidth=\"{StaticResource SizeTouchTarget}\"", personal);
        Assert.Contains("TextWrapping=\"Wrap\"", personal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\" ToolTip.Tip=\"{Binding MensajeEstado}\"", personal);
        Assert.DoesNotContain("ColumnDefinitions=\"*,16,420\"", personal);
        Assert.DoesNotContain("ItemsControl ItemsSource=\"{Binding BanderasRolSeleccionado}\"", personal);
    }

    [Fact]
    public void Dashboard_LimitaTarjetasYHaceFlexiblesAccionesYCatalogo()
    {
        var dashboard = Leer("Supermercado.GUI/Views/DashboardView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"280\" />", dashboard);
        Assert.Contains("MaxWidth=\"360\"", dashboard);
        Assert.Contains("MinWidth=\"220\"", dashboard);
        Assert.Contains("MaxWidth=\"260\"", dashboard);
        Assert.Contains("ScrollViewer HorizontalScrollBarVisibility=\"Auto\"", dashboard);
        Assert.Contains("VirtualizingStackPanel", dashboard);
        Assert.Contains("MinWidth=\"{StaticResource SizeTouchTarget}\"", dashboard);
        Assert.Contains("ToolTip.Tip=\"{Binding ValorPrincipal}\"", dashboard);
        Assert.DoesNotContain("            <Grid ColumnDefinitions=\"*,Auto\">", dashboard);
        Assert.DoesNotMatch("(?<!Max|Min)Width=\"260\"", dashboard);
    }
}
