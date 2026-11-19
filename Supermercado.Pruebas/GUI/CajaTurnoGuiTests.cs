namespace Supermercado.Pruebas.GUI;

public sealed class CajaTurnoGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    [Fact]
    public void CajaTurno_UsaScrollHorizontalYCamposQueSeAcomodan()
    {
        var vista = File.ReadAllText(Path.Combine(
            Raiz, "Supermercado.GUI/Views/CajaTurnoView.axaml"));

        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", vista);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", vista);
        Assert.Contains("<WrapPanel>", vista);
        Assert.DoesNotContain("ColumnDefinitions=\"190,12,*,12,170,12,170,12,Auto\"", vista);
    }

    [Fact]
    public void CajaTurno_HistorialVirtualizadoYMotivoLargoLegible()
    {
        var vista = File.ReadAllText(Path.Combine(
            Raiz, "Supermercado.GUI/Views/CajaTurnoView.axaml"));

        Assert.Contains("<VirtualizingStackPanel />", vista);
        Assert.Contains("ToolTip.Tip=\"{Binding Motivo}\"", vista);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", vista);
        Assert.Contains("TextWrapping=\"Wrap\"", vista);
        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding RetirosTurnoActual}\"", vista);
    }
}
