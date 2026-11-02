using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class VentasEnEsperaYSupervisionTests
{
    private const string ClaveSupervisor = "supervisor123";
    private const string PinSupervisor = "9988";

    [Fact]
    public async Task ValidarSupervisor_ConCredencialesYPermisoCorrectos_RetornaEmpleadoSinAfectarSesion()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesionCajero = new SesionActual();
        var trabajo = new UnidadDeTrabajoFalsa();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var cajero = new Usuario
        {
            Id = 1,
            NombreUsuario = "cajero1",
            HashClave = hasheador.Hashear("clave123"),
            Empleado = new Empleado { Id = 10, Activo = true, Rol = new Rol { Permisos = Permiso.Vender } }
        };
        usuarios.Datos.Add(cajero);
        sesionCajero.Iniciar(cajero);

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashClave = hasheador.Hashear(ClaveSupervisor),
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Supervisor",
                    Permisos = Permiso.AutorizarOperacionPos | Permiso.AnularVenta
                }
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new RepositorioEnMemoria<IntentoAcceso>(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesionCajero,
            trabajo,
            new RelojFijo(EscenarioInventario.Ahora));

        var empAutorizado = await servicioAuth.ValidarSupervisorAsync(
            "super1", PinSupervisor, Permiso.AutorizarOperacionPos);

        Assert.NotNull(empAutorizado);
        Assert.Equal(20, empAutorizado.Id);
        // La sesión activa debe seguir perteneciendo al cajero:
        Assert.Equal("cajero1", sesionCajero.Usuario?.NombreUsuario);
    }

    [Fact]
    public async Task ValidarSupervisor_ConPinInvalido_LanzaCredencialesInvalidas()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesion = new SesionActual();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol { Permisos = Permiso.AutorizarOperacionPos }
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new RepositorioEnMemoria<IntentoAcceso>(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesion,
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora));

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            servicioAuth.ValidarSupervisorAsync("super1", "0000", Permiso.AutorizarOperacionPos));
    }

    [Fact]
    public async Task ValidarSupervisor_SinPermisoRequerido_LanzaAccesoDenegado()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesion = new SesionActual();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol { Permisos = Permiso.Vender } // No tiene AutorizarOperacionPos
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new RepositorioEnMemoria<IntentoAcceso>(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesion,
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora));

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicioAuth.ValidarSupervisorAsync("super1", PinSupervisor, Permiso.AutorizarOperacionPos));
    }

    [Fact]
    public async Task RegistrarRetiro_ConMontoValido_ReduceEfectivoEsperado()
    {
        var turnos = new TurnosEnMemoria();
        var ventas = new VentasEnMemoria();
        var cajas = new RepositorioEnMemoria<Caja>();
        var retiros = new RepositorioEnMemoria<RetiroCaja>();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.ToleranciaArqueo] = "1000";

        cajas.Datos.Add(new Caja { Id = 1, Nombre = "Caja 1", Activa = true });

        var servicioCaja = new ServicioCaja(
            turnos,
            ventas,
            cajas,
            parametros,
            new AuditoriaFalsa(),
            Sesiones.Con(Permiso.AbrirCerrarTurno),
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora),
            retiros);

        var turno = await servicioCaja.AbrirTurnoAsync(1, 200000); // Base $200.000

        var retiro = await servicioCaja.RegistrarRetiroAsync(150000, "Recogida de seguridad a caja fuerte", supervisorId: 99);

        Assert.Equal(150000, retiro.Monto);
        Assert.Single(turno.Retiros);
        Assert.Equal(50000, turno.CalcularEfectivoEsperado([])); // 200.000 - 150.000 = 50.000

        var resultado = await servicioCaja.CerrarTurnoAsync(50000);
        Assert.Equal(50000, resultado.Esperado);
        Assert.Equal(0, resultado.Diferencia);
        Assert.True(resultado.DentroDeTolerancia);
    }

    [Fact]
    public async Task RegistrarRetiro_MayorAEfectivoDisponible_LanzaValidacionException()
    {
        var turnos = new TurnosEnMemoria();
        var ventas = new VentasEnMemoria();
        var cajas = new RepositorioEnMemoria<Caja>();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.ToleranciaArqueo] = "1000";

        cajas.Datos.Add(new Caja { Id = 1, Nombre = "Caja 1", Activa = true });

        var servicioCaja = new ServicioCaja(
            turnos,
            ventas,
            cajas,
            parametros,
            new AuditoriaFalsa(),
            Sesiones.Con(Permiso.AbrirCerrarTurno),
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora));

        await servicioCaja.AbrirTurnoAsync(1, 50000);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            servicioCaja.RegistrarRetiroAsync(80000, "Excede saldo", supervisorId: 99));
    }

    [Fact]
    public async Task VentasEnEspera_SuspenderYRecuperar_RestauraLineasCorrectamente()
    {
        var repo = new RepositorioEnMemoria<VentaEnEspera>();
        var servicio = new ServicioVentasEnEspera(
            repo,
            Sesiones.Con(Permiso.Vender),
            new AuditoriaFalsa(),
            new UnidadDeTrabajoFalsa(),
            new RelojFijo(EscenarioInventario.Ahora));

        var lineas = new List<LineaVenta>
        {
            new("7701234567890", 2),
            new("7709876543210", 1)
        };

        var suspendida = await servicio.SuspenderVentaAsync(
            cajaId: 1,
            lineas: lineas,
            clienteId: null,
            puntosARedimir: 0,
            identificadorCliente: "Cliente Canasta Roja",
            totalEstimado: 45000,
            nota: "Fue a traer más dinero");

        Assert.NotNull(suspendida);
        Assert.False(suspendida.Recuperada);

        var pendientes = await servicio.ObtenerVentasEnEsperaAsync(1);
        Assert.Single(pendientes);
        Assert.Equal("Cliente Canasta Roja", pendientes[0].IdentificadorCliente);
        Assert.Equal(3, pendientes[0].TotalItems);

        var (venta, lineasRecuperadas) = await servicio.RecuperarVentaAsync(suspendida.Id);
        Assert.True(venta.Recuperada);
        Assert.Equal(2, lineasRecuperadas.Count);
        Assert.Equal("7701234567890", lineasRecuperadas[0].CodigoBarras);
        Assert.Equal(2, lineasRecuperadas[0].Cantidad);

        // No debe poder recuperarse dos veces
        await Assert.ThrowsAsync<ValidacionException>(() =>
            servicio.RecuperarVentaAsync(suspendida.Id));
    }
}
