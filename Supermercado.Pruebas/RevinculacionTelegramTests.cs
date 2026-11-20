using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public sealed class RevinculacionTelegramTests
{
    [Fact]
    public async Task StartConCodigoNuevo_RevinculaChatYaVinculadoConsumeCodigoYAudita()
    {
        using var escenario = new EscenarioTelegram();
        var codigoCajero = escenario.Vinculos.GenerarCodigo(1, "cajero");
        await escenario.EnviarAsync($"/start {codigoCajero}");
        Assert.Contains("Vinculación completada", escenario.UltimoMensaje());

        escenario.LimpiarMensajes();
        var codigoGerente = escenario.Vinculos.GenerarCodigo(2, "gerente");
        await escenario.EnviarAsync($"/start {codigoGerente}");

        Assert.Contains("Vinculación completada", escenario.UltimoMensaje());
        Assert.Equal(2, escenario.Vinculos.Buscar(EscenarioTelegram.ChatId)?.EmpleadoId);
        Assert.Equal(
            "gerente",
            escenario.Vinculos.Buscar(EscenarioTelegram.ChatId)?.NombreUsuario);
        Assert.Equal(
            ResultadoVinculacion.CodigoInvalido,
            escenario.Vinculos.Vincular(codigoGerente, 501, 901));
        Assert.Equal(2, escenario.Auditoria.Registros.Count);
        Assert.All(
            escenario.Auditoria.Registros,
            registro => Assert.Contains("Origen: Telegram", registro.Detalle));
        Assert.Contains("revinculado", escenario.Auditoria.Registros[^1].Detalle);
        Assert.Equal(2, escenario.Usuarios.Guardados);
    }

    [Fact]
    public async Task StartConCodigoInvalido_DaMensajeClaroYConservaElVinculo()
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("cajero");
        escenario.LimpiarMensajes();
        var cantidadAuditorias = escenario.Auditoria.Registros.Count;

        await escenario.EnviarAsync("/start no-existe");

        Assert.Contains("inválido", escenario.UltimoMensaje());
        Assert.Equal(
            1,
            escenario.Vinculos.Buscar(EscenarioTelegram.ChatId)?.EmpleadoId);
        Assert.Equal(cantidadAuditorias, escenario.Auditoria.Registros.Count);
    }

    [Fact]
    public async Task StartConCodigoVencido_DaMensajeClaroYNoVincula()
    {
        using var escenario = new EscenarioTelegram();
        var codigo = escenario.Vinculos.GenerarCodigo(2, "gerente");
        escenario.Reloj.Ahora = escenario.Reloj.Ahora.AddMinutes(11);

        await escenario.EnviarAsync($"/start {codigo}");

        Assert.Contains("venció", escenario.UltimoMensaje());
        Assert.Null(escenario.Vinculos.Buscar(EscenarioTelegram.ChatId));
    }
}
