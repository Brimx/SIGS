using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Finanzas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioFinanzasTests
{
    private static ServicioFinanzas Crear(Permiso permisos)
        => new(
            new VentasEnMemoria(),
            new GastosEnMemoria(),
            new AuditoriaFalsa(),
            Sesiones.Con(permisos),
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora));

    [Fact]
    public async Task RegistrarGasto_ExigeVerFinanzas()
    {
        var servicio = Crear(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.RegistrarGastoAsync("aseo", 50000));
    }

    [Fact]
    public async Task RegistrarGasto_ConPermiso_Audita()
    {
        var auditoria = new AuditoriaFalsa();
        var trabajo = new UnidadDeTrabajoFalsa();
        var servicio = new ServicioFinanzas(
            new VentasEnMemoria(),
            new GastosEnMemoria(),
            auditoria,
            Sesiones.Con(Permiso.VerFinanzas),
            trabajo,
            new RelojFijo(EscenarioInventario.Ahora));

        await servicio.RegistrarGastoAsync("aseo", 50000);

        Assert.Equal(
            AccionAuditoria.GastoRegistrado,
            Assert.Single(auditoria.Acciones));
        Assert.Equal(1, trabajo.Guardados);
    }
}
