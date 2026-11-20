using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public sealed class ComandosCuentaTelegramTests
{
    [Fact]
    public async Task Desvincular_EliminaElVinculoYLoAudita()
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("gerente");
        escenario.LimpiarMensajes();

        await escenario.EnviarAsync("/desvincular");

        Assert.Contains("Desvinculación completada", escenario.UltimoMensaje());
        Assert.Null(escenario.Vinculos.Buscar(EscenarioTelegram.ChatId));
        var registro = escenario.Auditoria.Registros[^1];
        Assert.Contains("Origen: Telegram", registro.Detalle);
        Assert.Contains("desvinculado", registro.Detalle);
        Assert.Equal(2, escenario.Usuarios.Guardados);
    }

    [Fact]
    public async Task Quien_RespondeUsuarioYRolSinDatosSensibles()
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("gerente");
        escenario.LimpiarMensajes();

        await escenario.EnviarAsync("/quien");

        var mensaje = escenario.UltimoMensaje();
        Assert.Contains("gerente", mensaje);
        Assert.Contains("Gerente", mensaje);
        Assert.DoesNotContain("hash-secreto", mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DOC-SECRETO", mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@correo-secreto.local", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartSinCodigo_VinculadoInformaUsuarioYRol()
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("gerente");
        escenario.LimpiarMensajes();

        await escenario.EnviarAsync("/start");

        var mensaje = escenario.UltimoMensaje();
        Assert.Contains("gerente", mensaje);
        Assert.Contains("Gerente", mensaje);
        Assert.Contains("/start CODIGO", mensaje);
        Assert.DoesNotContain("hash-secreto", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartSinCodigo_SinVinculoExplicaComoVincular()
    {
        using var escenario = new EscenarioTelegram();

        await escenario.EnviarAsync("/start");

        var mensaje = escenario.UltimoMensaje();
        Assert.Contains("Configuración", mensaje);
        Assert.Contains("/start CODIGO", mensaje);
    }
}
