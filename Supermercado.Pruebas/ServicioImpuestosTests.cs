using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Finanzas;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioImpuestosTests
{
    private readonly RepositorioEnMemoria<Impuesto> impuestos = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioImpuestos CrearServicio(
        Permiso permisos = Permiso.GestionarTarifas)
        => new(
            impuestos,
            auditoria,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public async Task ObtenerActivosAsync_FiltraSoloLosActivos()
    {
        var servicio = CrearServicio();
        await impuestos.AgregarAsync(new Iva
        {
            Id = 1,
            Nombre = "IVA 19",
            Activo = true
        });
        await impuestos.AgregarAsync(new Iva
        {
            Id = 2,
            Nombre = "IVA Obsoleto",
            Activo = false
        });

        var activos = await servicio.ObtenerActivosAsync();

        var item = Assert.Single(activos);
        Assert.Equal("IVA 19", item.Nombre);
    }

    [Fact]
    public async Task CambiarTarifaAsync_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var iva = new Iva
        {
            Id = 1,
            Nombre = "IVA General",
            PuntosBasicos = 1900
        };
        await impuestos.AgregarAsync(iva);

        await servicio.CambiarTarifaAsync(1, 1600);

        Assert.Equal(1600, iva.PuntosBasicos);
        Assert.Contains(AccionAuditoria.CambioTarifa, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CambiarTarifaAsync_TarifaFueraDeRango_LanzaError()
    {
        var servicio = CrearServicio();
        var iva = new Iva { Id = 1, PuntosBasicos = 1900 };
        await impuestos.AgregarAsync(iva);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarTarifaAsync(1, -1));
        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarTarifaAsync(1, 10001));
    }

    [Fact]
    public async Task CambiarTarifaAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CambiarTarifaAsync(1, 1900));
    }
}
