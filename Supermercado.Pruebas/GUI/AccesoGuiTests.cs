namespace Supermercado.Pruebas.GUI;

public sealed class AccesoGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    [Fact]
    public void Login_CabeEn600DeAltoInclusoConTecladoPinYPanelDemo()
    {
        var login = Leer("Supermercado.GUI/Views/LoginView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", login);
        Assert.Contains("MaxWidth=\"480\"", login);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", login);
        Assert.Contains("<UniformGrid Columns=\"2\">", login);
        Assert.Contains("MinHeight=\"44\"", login);
        Assert.DoesNotContain("ColumnDefinitions=\"*,480,*\"", login);
    }

    [Fact]
    public void Registro_UsaScrollYCamposFlexiblesDentroDelModalDelPos()
    {
        var registro = Leer("Supermercado.GUI/Views/RegistroUsuarioView.axaml");
        var pos = Leer("Supermercado.GUI/Views/PosView.axaml");

        Assert.Contains("MaxHeight=\"460\"", registro);
        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"160\" />", registro);
        Assert.Contains("TextWrapping=\"Wrap\"", registro);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", registro);
        Assert.Contains("ToolTip.Tip=\"{Binding SubtituloFormulario}\"", registro);
        Assert.Contains("MaxHeight=\"520\"", pos);
    }
}
