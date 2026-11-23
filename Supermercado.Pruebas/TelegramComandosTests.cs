using Supermercado.Pruebas.Falsos;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supermercado.GUI;
using Supermercado.Logica.Servicios;
using Supermercado.Datos;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

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

public sealed class ComposicionTelegramTests
{
    [Fact]
    public void ConfiguracionServicios_ResuelveWorkerYClientesSinSecreto()
    {
        var servicios = new ServiceCollection();
        App.ConfigurarServicios(servicios);
        using var proveedor = servicios.BuildServiceProvider();

        var workers = proveedor.GetServices<IHostedService>().ToArray();
        var worker = Assert.Single(workers);

        Assert.IsType<TelegramWorker>(worker);
        Assert.IsAssignableFrom<IClienteTelegram>(
            proveedor.GetRequiredService<IClienteTelegram>());
        Assert.IsAssignableFrom<Asistente>(
            proveedor.GetRequiredService<Asistente>());
    }
}

public sealed class PermisosTelegramTests
{
    [Fact]
    public void GerenteDeLaSemilla_TieneLosPermisosDeLosCuatroComandos()
    {
        var gerente = ValoresIniciales.Roles()
            .Single(rol => rol.Nombre == NombresRol.Gerente);

        Assert.True(gerente.Tiene(
            Permiso.VerInventario | Permiso.VerReportes | Permiso.VerFinanzas));
    }

    [Theory]
    [InlineData("/stock", "No hay productos por debajo del mínimo.")]
    [InlineData("/ventas", "Ventas de hoy: 0")]
    [InlineData("/caja", "No tienes un turno de caja abierto.")]
    [InlineData("/resumen", "Stock bajo: 0 productos.")]
    public async Task ComandoDeConsulta_GerenteLoResuelveSinRechazoDeAutorizacion(
        string comando,
        string esperado)
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("gerente");
        escenario.LimpiarMensajes();

        await escenario.EnviarAsync(comando);

        var mensaje = escenario.UltimoMensaje();
        Assert.Contains(esperado, mensaje);
        Assert.DoesNotContain(
            "no tiene autorización", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resumen_GerenteIncluyeVentasCajaYStock()
    {
        using var escenario = new EscenarioTelegram();
        await escenario.VincularAsync("gerente");
        escenario.LimpiarMensajes();

        await escenario.EnviarAsync("/resumen");

        var mensaje = escenario.UltimoMensaje();
        Assert.Contains("Ventas de hoy", mensaje);
        Assert.Contains("Caja:", mensaje);
        Assert.Contains("Stock bajo:", mensaje);
    }
}

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

public sealed class SesionContextoTelegramTests
{
    [Fact]
    public async Task UsuarioContextual_NoAlteraLaSesionGlobalYSeRestauraTrasAwait()
    {
        var global = CrearUsuario(1);
        var vinculado = CrearUsuario(2);
        var sesion = new SesionActual();
        sesion.Iniciar(global);

        using (sesion.UsarUsuario(vinculado))
        {
            Assert.Equal(2, sesion.EmpleadoId);
            await Task.Yield();
            Assert.Equal(2, sesion.EmpleadoId);
        }

        Assert.Equal(1, sesion.EmpleadoId);
    }

    private static Usuario CrearUsuario(int id)
        => new()
        {
            EmpleadoId = id,
            Empleado = new Empleado
            {
                Id = id,
                Activo = true,
                Rol = new Rol { Nombre = "Gerente", Permisos = Permiso.VerAuditoria }
            }
        };
}

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

            Assert.Equal(ResultadoVinculacion.Vinculado, store.Vincular(codigo, 500, 900));
            Assert.Equal(ResultadoVinculacion.CodigoInvalido, store.Vincular(codigo, 501, 900));

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
    public void CodigoNuevo_ReemplazaElVinculoDelMismoChatYSeConsume()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"telegram-{Guid.NewGuid():N}.json");
        try
        {
            var reloj = new RelojVariable();
            var vinculos = new VinculosTelegram(reloj, ruta);
            var primerCodigo = vinculos.GenerarCodigo(1, "cajero");
            var segundoCodigo = vinculos.GenerarCodigo(2, "gerente");

            Assert.Equal(ResultadoVinculacion.Vinculado, vinculos.Vincular(primerCodigo, 500, 900));
            Assert.Equal(ResultadoVinculacion.Vinculado, vinculos.Vincular(segundoCodigo, 500, 900));

            Assert.Equal(2, vinculos.Buscar(500)?.EmpleadoId);
            Assert.Equal("gerente", vinculos.Buscar(500)?.NombreUsuario);
            Assert.Equal(ResultadoVinculacion.CodigoInvalido, vinculos.Vincular(segundoCodigo, 501, 900));
            Assert.Single(new VinculosTelegram(reloj, ruta).ObtenerTodos());
        }
        finally
        {
            if (File.Exists(ruta))
                File.Delete(ruta);
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
            Assert.Equal(ResultadoVinculacion.Vinculado, vinculos.Vincular(codigo, 123, 456));

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

        Assert.Equal(ResultadoVinculacion.CodigoVencido, store.Vincular(codigo, 500, 900));
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
