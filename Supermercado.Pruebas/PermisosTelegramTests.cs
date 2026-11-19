using Supermercado.Datos;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

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
