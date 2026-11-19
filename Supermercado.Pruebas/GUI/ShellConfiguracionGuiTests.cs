namespace Supermercado.Pruebas.GUI;

public sealed class ShellConfiguracionGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    [Fact]
    public void MainWindow_AbreDentroDeLaPantallaYDejaMargenAlModal()
    {
        var ventana = Leer("Supermercado.GUI/Views/MainWindow.axaml");
        var controles = Leer("Supermercado.GUI/Resources/Controles.axaml");

        Assert.Contains("MinWidth=\"900\" MinHeight=\"600\"", ventana);
        Assert.Contains("Margin=\"{StaticResource Space16}\"", ventana);
        Assert.Contains("Padding\" Value=\"{StaticResource Space16}\"", controles);
        Assert.DoesNotMatch("(?<!Design)Width=\"1360\"", ventana);
        Assert.DoesNotMatch("(?<!Design)Height=\"820\"", ventana);
    }

    [Fact]
    public void Configuracion_EsFlexibleYCabeConDobleScrollEvitado()
    {
        var configuracion = Leer("Supermercado.GUI/Views/ConfiguracionView.axaml");

        Assert.Contains("MaxWidth=\"760\"", configuracion);
        Assert.Contains("MaxHeight=\"560\"", configuracion);
        Assert.Contains("Padding=\"{StaticResource PaddingDialog}\"", configuracion);
        Assert.Contains("TextWrapping=\"Wrap\"", configuracion);
        Assert.DoesNotContain("MaxHeight=\"680\"", configuracion);
        Assert.DoesNotContain("MaxHeight=\"180\"", configuracion);
        Assert.DoesNotMatch("(?<!Max|Min)Width=\"760\"", configuracion);
    }

    [Fact]
    public void Shell_NoDejaQueLaBarraCompactaColapseElTitulo()
    {
        var shell = Leer("Supermercado.GUI/Views/ShellView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"*\" MinWidth=\"140\" />", shell);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" MaxWidth=\"200\" />", shell);
        Assert.Contains("MaxWidth=\"480\"", shell);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", shell);
        Assert.Contains("ToolTip.Tip=\"{Binding SubtituloModulo}\"", shell);
        Assert.Contains("ToolTip.Tip=\"{Binding RolUsuarioActivo}\"", shell);
        Assert.DoesNotContain("ColumnDefinitions=\"*,Auto,10,Auto\"", shell);
    }

    [Fact]
    public void Shell_ReservaFilaParaMensajeEstadoComun()
    {
        var shell = Leer("Supermercado.GUI/Views/ShellView.axaml");
        var shellVm = Leer("Supermercado.GUI/ViewModels/ShellViewModel.cs");

        Assert.Contains("RowDefinitions=\"Auto,*,Auto\"", shell);
        Assert.Contains("<Border Grid.Row=\"2\"", shell);
        Assert.Contains("Text=\"{Binding MensajeEstado}\"", shell);
        Assert.Contains("TextWrapping=\"Wrap\"", shell);
        Assert.Contains("ToolTip.Tip=\"{Binding MensajeEstado}\"", shell);
        Assert.Contains("partial void OnPaginaActualChanged(ViewModelBase value)", shellVm);
        Assert.Contains("nameof(MensajeEstado)", shellVm);
    }
}
