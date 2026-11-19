using Supermercado.GUI.ViewModels;

namespace Supermercado.Pruebas.GUI;

public sealed class PosGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    [Fact]
    public void Pos_UsaColumnasFlexiblesYSscrollParaVentanasPequenas()
    {
        var pos = Leer("Supermercado.GUI/Views/PosView.axaml");

        Assert.Contains("<ColumnDefinition Width=\"2*\" MinWidth=\"300\" />", pos);
        Assert.Contains("<ColumnDefinition Width=\"3*\" MinWidth=\"260\" />", pos);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", pos);
        Assert.Contains("MinHeight=\"160\"", pos);
        Assert.DoesNotContain("ColumnDefinitions=\"*,16,360\"", pos);
        Assert.DoesNotContain("ColumnDefinitions=\"*,12,260,8,Auto\"", pos);
    }

    [Fact]
    public void Pos_ModalesYTextosLargosNoExcedenElViewport()
    {
        var pos = Leer("Supermercado.GUI/Views/PosView.axaml");

        Assert.Contains("MaxWidth=\"600\"", pos);
        Assert.Contains("MaxHeight=\"520\"", pos);
        Assert.Contains("MaxHeight=\"480\"", pos);
        Assert.Contains("Margin=\"16\"", pos);
        Assert.Contains("TextWrapping=\"Wrap\"", pos);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", pos);
        Assert.DoesNotContain("Text=\"{Binding MensajeEstado}\"", pos);
        Assert.Contains("TotalArticulos", pos);
        Assert.DoesNotMatch("(?<!Max)(?<!Min)Width=\"600\"", pos);
        Assert.DoesNotContain("MaxHeight=\"740\"", pos);
        Assert.DoesNotContain("MaxHeight=\"680\"", pos);
    }

    [Fact]
    public void Pos_ListasLargasUsanVirtualizacionExplicita()
    {
        var pos = Leer("Supermercado.GUI/Views/PosView.axaml");

        Assert.Equal(3, Cantidad(pos, "<VirtualizingStackPanel />"));
        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding PagosAgregados}\"", pos);
    }

    [Fact]
    public void Pos_MantieneFocoInicialYEnterParaAgregarProducto()
    {
        var pos = Leer("Supermercado.GUI/Views/PosView.axaml");
        var codeBehind = Leer("Supermercado.GUI/Views/PosView.axaml.cs");
        var vm = typeof(PosViewModel);

        Assert.Contains("x:Name=\"InputCodigoBarras\"", pos);
        Assert.Contains("Gesture=\"Enter\" Command=\"{Binding EscanearOAgregarCommand}\"", pos);
        Assert.Contains("AttachedToVisualTree", codeBehind);
        Assert.Contains("InputCodigoBarras?.Focus()", codeBehind);
        Assert.NotNull(vm.GetMethod("EscanearOAgregarAsync"));
    }

    private static int Cantidad(string texto, string fragmento)
    {
        var cantidad = 0;
        var indice = 0;
        while ((indice = texto.IndexOf(fragmento, indice, StringComparison.Ordinal)) >= 0)
        {
            cantidad++;
            indice += fragmento.Length;
        }

        return cantidad;
    }
}
