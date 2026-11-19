using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas.GUI;

public sealed class ConfiguracionTelegramTests
{
    [Fact]
    public void GenerarCodigo_UsaAlUsuarioDeLaSesionYPermiteVincularloUnaVez()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        try
        {
            var usuario = new Usuario
            {
                Id = 4,
                EmpleadoId = 12,
                NombreUsuario = "gerencia",
                Empleado = new Empleado
                {
                    Id = 12,
                    Activo = true,
                    Rol = new Rol { Nombre = NombresRol.Gerente }
                }
            };
            var sesion = new SesionActual();
            sesion.Iniciar(usuario);
            var reloj = new TimeProviderFalso();
            var vinculos = new VinculosTelegram(
                reloj,
                Path.Combine(carpeta, "telegram.json"));
            var vm = new ConfiguracionViewModel(
                null, null, sesion, vinculos, reloj);

            vm.GenerarCodigoVinculacionCommand.Execute(null);

            Assert.StartsWith("/start ", vm.InstruccionVinculacion);
            Assert.Contains("10 minutos", vm.MensajeVinculacion);
            var codigo = vm.InstruccionVinculacion[7..];
            Assert.Equal(ResultadoVinculacion.Vinculado, vinculos.Vincular(codigo, 50, 60));
            Assert.Equal(ResultadoVinculacion.CodigoInvalido, vinculos.Vincular(codigo, 51, 60));
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void GenerarCodigo_SinSesionActivaNoExponeCodigo()
    {
        var vm = new ConfiguracionViewModel(
            null,
            null,
            new SesionActual(),
            null,
            TimeProvider.System);

        vm.GenerarCodigoVinculacionCommand.Execute(null);

        Assert.Empty(vm.InstruccionVinculacion);
        Assert.Contains("Inicia sesión", vm.MensajeVinculacion);
    }

    private sealed class TimeProviderFalso : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
