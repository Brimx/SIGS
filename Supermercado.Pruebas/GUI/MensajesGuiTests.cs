namespace Supermercado.Pruebas.GUI;

public sealed class MensajesGuiTests
{
    private static readonly string Raiz = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string Leer(string rutaRelativa) =>
        File.ReadAllText(Path.Combine(Raiz, rutaRelativa));

    private static bool Envuelve(string xaml, string enlace)
    {
        var inicio = xaml.IndexOf($"Text=\"{{Binding {enlace}}}\"", StringComparison.Ordinal);
        if (inicio < 0)
            return false;

        var fin = xaml.IndexOf("TextWrapping=\"Wrap\"", inicio, StringComparison.Ordinal);
        return fin >= 0 && fin - inicio < 400;
    }

    private static int Cantidad(string texto, string fragmento)
    {
        var total = 0;
        var indice = 0;
        while ((indice = texto.IndexOf(fragmento, indice, StringComparison.Ordinal)) >= 0)
        {
            total++;
            indice += fragmento.Length;
        }

        return total;
    }

    [Fact]
    public void AlertasDeLoginEnvuelvenTextoDentroDeUnGridConAnchoUtil()
    {
        var login = Leer("Supermercado.GUI/Views/LoginView.axaml");

        Assert.Equal(2, Cantidad(login, "<Grid ColumnDefinitions=\"Auto,8,*\">"));
        Assert.True(Envuelve(login, "MensajeError"));
        Assert.True(Envuelve(login, "MensajeExito"));
    }

    [Fact]
    public void MensajesDeEstadoYErrorDeOtrasVistasEnvuelvenTexto()
    {
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/RegistroUsuarioView.axaml"), "MensajeEstado"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/ConfiguracionView.axaml"), "MensajeVinculacion"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/ConfiguracionView.axaml"), "InstruccionVinculacion"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/ShellView.axaml"), "MensajeEstado"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/DashboardView.axaml"), "MensajeEstado"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/CajaTurnoView.axaml"), "MensajeEstado"));
        Assert.True(Envuelve(Leer("Supermercado.GUI/Views/PosView.axaml"), "ErrorSupervisor"));
    }
}
