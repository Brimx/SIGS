using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Finanzas;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioNominaTests
{
    private readonly RepositorioEnMemoria<Empleado> empleados = new();
    private readonly RepositorioEnMemoria<Nomina> nominas = new();
    private readonly ParametrosFalsos parametros = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioNomina CrearServicio(
        Permiso permisos = Permiso.GestionarPersonal)
    {
        parametros.Valores[ClavesParametro.SalarioMinimo] = "1300000";
        parametros.Valores[ClavesParametro.AuxilioTransporte] = "162000";
        parametros.Valores[ClavesParametro.TopeAuxilioEnSmmlv] = "2";
        parametros.Valores[ClavesParametro.SaludPuntosBasicos] = "400";
        parametros.Valores[ClavesParametro.PensionPuntosBasicos] = "400";

        return new ServicioNomina(
            empleados,
            nominas,
            parametros,
            Sesiones.Con(permisos),
            trabajo,
            reloj);
    }

    [Fact]
    public async Task CalcularAsync_ConPermiso_CalculaGuardaYRetorna()
    {
        var servicio = CrearServicio();
        var emp = new Empleado
        {
            Id = 1,
            Documento = "123",
            Nombres = "Carlos",
            SalarioBase = 1300000
        };
        await empleados.AgregarAsync(emp);

        var nomina = await servicio.CalcularAsync(1, 2026, 10);

        Assert.NotNull(nomina);
        Assert.Equal(1, nomina.EmpleadoId);
        Assert.Equal(2026, nomina.Anio);
        Assert.Equal(10, nomina.Mes);
        Assert.Single(nominas.Datos);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CalcularAsync_NominaDuplicada_LanzaValidacionException()
    {
        var servicio = CrearServicio();
        var emp = new Empleado
        {
            Id = 1,
            Nombres = "Carlos",
            SalarioBase = 1300000
        };
        await empleados.AgregarAsync(emp);

        await servicio.CalcularAsync(1, 2026, 10);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CalcularAsync(1, 2026, 10));
    }

    [Fact]
    public async Task CalcularAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CalcularAsync(1, 2026, 10));
    }

    [Fact]
    public async Task CalcularAsync_MesInvalido_LanzaValidacionException()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CalcularAsync(1, 2026, 13));
    }
}
