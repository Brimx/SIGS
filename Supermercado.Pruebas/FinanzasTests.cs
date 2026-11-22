using Supermercado.Entidades.Base;
using Xunit;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Finanzas;
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Configuracion;

namespace Supermercado.Pruebas;

public class DineroTests
{
    [Fact]
    public void Dividir_RedondeaAlEnteroMasCercano()
    {
        Assert.Equal(3L, Dinero.DividirRedondeando(5, 2));
        Assert.Equal(2L, Dinero.DividirRedondeando(5, 3));
        Assert.Equal(1L, Dinero.DividirRedondeando(4, 3));
    }

    [Fact]
    public void Porcentaje_UsaPuntosBasicos()
    {
        Assert.Equal(25L, Dinero.Porcentaje(1000, 250));
    }
}

public class ImpuestosTests
{
    [Fact]
    public void Iva_SeExtraeDelPrecioQueYaLoIncluye()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(1900L, iva.Calcular(11900));
    }

    [Fact]
    public void Iva_RedondeaAlPesoMasCercano()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(479L, iva.Calcular(3000));
    }

    [Fact]
    public void Iva_CincoPorCiento()
    {
        var iva = new Iva { PuntosBasicos = 500 };

        Assert.Equal(500L, iva.Calcular(10500));
    }

    [Fact]
    public void Iva_ExentoNoCobraNada()
    {
        var iva = new Iva { PuntosBasicos = 0 };

        Assert.Equal(0L, iva.Calcular(10000));
    }

    [Fact]
    public void Retencion_NoAplicaPorDebajoDeLaBaseMinima()
    {
        var retencion = new Retencion
        {
            PuntosBasicos = 250,
            BaseMinima = 1_000_000
        };

        Assert.Equal(0L, retencion.Calcular(999_999));
    }

    [Fact]
    public void Retencion_AplicaLaTarifaSobreLaBase()
    {
        var retencion = new Retencion
        {
            PuntosBasicos = 250,
            BaseMinima = 1_000_000
        };

        Assert.Equal(25000L, retencion.Calcular(1_000_000));
    }

    [Fact]
    public void Porcentaje_SeMuestraDesdeLosPuntosBasicos()
    {
        var iva = new Iva { PuntosBasicos = 1900 };

        Assert.Equal(19m, iva.Porcentaje);
    }
}

public class NominaTests
{
    private static readonly DateTime Ahora
        = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    // Cifras inventadas solo para la prueba; las reales van en la base.
    private static ParametrosNomina Parametros(long salarioMinimo = 1_000_000)
        => new(salarioMinimo, 150_000, 2, 400, 400);

    [Fact]
    public void Calcula_AuxilioDeduccionesYNeto()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        var nomina = Nomina.Calcular(empleado, 2026, 10, Parametros(), Ahora);

        Assert.Equal(150_000L, nomina.AuxilioTransporte);
        Assert.Equal(60_000L, nomina.DeduccionSalud);
        Assert.Equal(60_000L, nomina.DeduccionPension);
        Assert.Equal(1_530_000L, nomina.Neto);
    }

    [Fact]
    public void SinAuxilioPorEncimaDelTope()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 3_000_000 };

        var nomina = Nomina.Calcular(empleado, 2026, 10, Parametros(), Ahora);

        Assert.Equal(0L, nomina.AuxilioTransporte);
    }

    [Fact]
    public void ExigeQueElSalarioMinimoEsteConfigurado()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ParametroFaltanteException>(
            () => Nomina.Calcular(empleado, 2026, 10, Parametros(0), Ahora));
    }

    [Fact]
    public void RechazaUnMesInvalido()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 2026, 13, Parametros(), Ahora));
    }

    [Fact]
    public void RechazaUnAnioInvalido()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 0, 10, Parametros(), Ahora));
    }

    [Fact]
    public void RechazaParametrosNegativos()
    {
        var empleado = new Empleado { Id = 1, SalarioBase = 1_500_000 };
        var negativos = new ParametrosNomina(
            1_000_000, -1, 2, 400, 400);

        Assert.Throws<ValidacionException>(
            () => Nomina.Calcular(empleado, 2026, 10, negativos, Ahora));
    }
}

public class ServicioFinanzasTests
{
    private static readonly RelojFijo Reloj = new(EscenarioInventario.Ahora);

    private static ServicioFinanzas Crear(
        Permiso permisos, ContextoPruebasSqlite contexto)
        => new(
            new VentasEnMemoria(),
            new GastosEnMemoria(contexto),
            new AuditoriaFalsa(),
            Sesiones.Con(permisos),
            Reloj);

    [Fact]
    public async Task RegistrarGasto_ExigeVerFinanzas()
    {
        using var contexto = new ContextoPruebasSqlite(Reloj);
        var servicio = Crear(Permiso.Vender, contexto);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.RegistrarGastoAsync("aseo", 50000));
    }

    [Fact]
    public async Task RegistrarGasto_ConPermiso_Audita()
    {
        using var contexto = new ContextoPruebasSqlite(Reloj);
        var auditoria = new AuditoriaFalsa();
        var gastos = new GastosEnMemoria(contexto);
        var servicio = new ServicioFinanzas(
            new VentasEnMemoria(),
            gastos,
            auditoria,
            Sesiones.Con(Permiso.VerFinanzas),
            Reloj);

        await servicio.RegistrarGastoAsync("aseo", 50000);

        Assert.Equal(
            AccionAuditoria.GastoRegistrado,
            Assert.Single(auditoria.Acciones));
        Assert.Equal(1, gastos.Guardados);
    }
}

public class ServicioImpuestosTests
{
    private readonly ContextoPruebasSqlite Contexto = new();
    private readonly ImpuestosEnMemoria impuestos;
    private readonly AuditoriaFalsa auditoria = new();

    public ServicioImpuestosTests()
    {
        impuestos = new ImpuestosEnMemoria(Contexto);
    }

    private ServicioFinanzas CrearServicio(
        Permiso permisos = Permiso.GestionarTarifas)
        => new(
            null!,
            null!,
            auditoria: auditoria,
            sesion: Sesiones.Con(permisos),
            impuestos: impuestos);

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
        Assert.Equal(1, impuestos.Guardados);
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

public class ServicioNominaTests
{
    private readonly EmpleadosEnMemoria empleados;
    private readonly NominasEnMemoria nominas;
    private readonly ParametrosFalsos parametros = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);
    private readonly ContextoPruebasSqlite Contexto;

    public ServicioNominaTests()
    {
        Contexto = new ContextoPruebasSqlite(reloj);
        empleados = new EmpleadosEnMemoria(Contexto);
        nominas = new NominasEnMemoria(Contexto);
    }

    private ServicioFinanzas CrearServicio(
        Permiso permisos = Permiso.GestionarPersonal)
    {
        parametros.Valores[ClavesParametro.SalarioMinimo] = "1300000";
        parametros.Valores[ClavesParametro.AuxilioTransporte] = "162000";
        parametros.Valores[ClavesParametro.TopeAuxilioEnSmmlv] = "2";
        parametros.Valores[ClavesParametro.SaludPuntosBasicos] = "400";
        parametros.Valores[ClavesParametro.PensionPuntosBasicos] = "400";

        return new ServicioFinanzas(
            null!,
            null!,
            sesion: Sesiones.Con(permisos),
            reloj: reloj,
            empleados: empleados,
            nominas: nominas,
            parametros: parametros);
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
        Assert.Equal(1, nominas.Guardados);
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
