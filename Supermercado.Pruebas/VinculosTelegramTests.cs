using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas;

public sealed class VinculosTelegramTests
{
    [Fact]
    public void CodigoVinculaUnaSolaVezYSeConservaAlReiniciar()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, "vinculos.json");
        try
        {
            var reloj = new RelojVariable();
            var store = new VinculosTelegram(reloj, ruta);
            var codigo = store.GenerarCodigo(17, "gerente");

            Assert.True(store.Vincular(codigo, 500, 900));
            Assert.False(store.Vincular(codigo, 501, 900));

            var recargado = new VinculosTelegram(reloj, ruta);
            Assert.Equal(17, recargado.Buscar(500)?.EmpleadoId);
            Assert.Equal(900, recargado.Buscar(500)?.UsuarioTelegramId);
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void Desvincular_QuitaElChatPersistido()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"telegram-{Guid.NewGuid():N}.json");
        try
        {
            var reloj = new RelojVariable();
            var vinculos = new VinculosTelegram(reloj, ruta);
            var codigo = vinculos.GenerarCodigo(4, "gerente");
            Assert.True(vinculos.Vincular(codigo, 123, 456));

            vinculos.Desvincular(123);

            Assert.Null(vinculos.Buscar(123));
            Assert.Empty(new VinculosTelegram(reloj, ruta).ObtenerTodos());
        }
        finally
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
    }

    [Fact]
    public void CodigoVencidoNoVincula()
    {
        var reloj = new RelojVariable();
        var store = new VinculosTelegram(reloj, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var codigo = store.GenerarCodigo(17, "gerente");
        reloj.Ahora = reloj.Ahora.AddMinutes(11);

        Assert.False(store.Vincular(codigo, 500, 900));
    }

    [Fact]
    public void CallbackValidaDestinoYSoloSeConsumeUnaVez()
    {
        var reloj = new RelojVariable();
        var store = new VinculosTelegram(reloj, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var aprobacion = store.CrearAprobacion(
            HerramientaAsistente.SolicitarAnulacion,
            22,
            "venta duplicada",
            500,
            900,
            reloj.Ahora.AddMinutes(3));

        Assert.False(store.TomarAprobacion(
            aprobacion.Token, 501, 900, reloj.Ahora, out _));
        Assert.True(store.TomarAprobacion(
            aprobacion.Token, 500, 900, reloj.Ahora, out var tomada));
        Assert.Equal(22, tomada?.Id);
        Assert.False(store.TomarAprobacion(
            aprobacion.Token, 500, 900, reloj.Ahora, out _));
    }

    [Fact]
    public void MensajesSeLimitanPorChatYVentanaDeUnMinuto()
    {
        var reloj = new RelojVariable();
        var store = new VinculosTelegram(reloj, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        for (var intento = 0; intento < 20; intento++)
            Assert.True(store.PermitirMensaje(500, reloj.Ahora));
        Assert.False(store.PermitirMensaje(500, reloj.Ahora));
        Assert.True(store.PermitirMensaje(501, reloj.Ahora));
        Assert.True(store.PermitirMensaje(500, reloj.Ahora.AddMinutes(1)));
    }

    private sealed class RelojVariable : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
