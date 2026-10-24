using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioAutenticacionTests
{
    private const string Clave = "clave-segura-1";

    private class Escenario
    {
        public UsuariosEnMemoria Usuarios { get; } = new();
        public SesionActual Sesion { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public ServicioAutenticacion Servicio { get; }
        public Usuario Ana { get; }

        public Escenario(int maximoIntentos = 3)
        {
            var hasheador = new HasheadorFalso();
            var parametros = new ParametrosFalsos();
            parametros.Valores[ClavesParametro.MaxIntentosFallidos]
                = maximoIntentos.ToString();

            Ana = new Usuario
            {
                Id = 1,
                NombreUsuario = "ana",
                HashClave = hasheador.Hashear(Clave),
                Empleado = new Empleado
                {
                    Activo = true,
                    Rol = new Rol { Permisos = Permiso.Vender }
                }
            };
            Usuarios.Datos.Add(Ana);

            Servicio = new ServicioAutenticacion(
                Usuarios,
                new RepositorioEnMemoria<IntentoAcceso>(),
                new AuditoriaFalsa(),
                parametros,
                hasheador,
                Sesion,
                Trabajo,
                new RelojFijo(EscenarioInventario.Ahora));
        }
    }

    [Fact]
    public async Task ClaveCorrecta_IniciaSesion()
    {
        var e = new Escenario();

        await e.Servicio.IniciarSesionAsync("ana", Clave);

        Assert.True(e.Sesion.EstaAutenticada);
        Assert.True(e.Sesion.Tiene(Permiso.Vender));
    }

    [Fact]
    public async Task ElUsuarioSeBloqueaTrasLosIntentosConfigurados()
    {
        var e = new Escenario(maximoIntentos: 3);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-1"));
        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-2"));
        await Assert.ThrowsAsync<CuentaBloqueadaException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-3"));

        // Ya bloqueada, ni la clave correcta entra.
        await Assert.ThrowsAsync<CuentaBloqueadaException>(
            () => e.Servicio.IniciarSesionAsync("ana", Clave));
        Assert.False(e.Sesion.EstaAutenticada);
    }

    [Fact]
    public async Task UnExitoReiniciaElContadorDeFallos()
    {
        var e = new Escenario(maximoIntentos: 3);
        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala"));

        await e.Servicio.IniciarSesionAsync("ana", Clave);

        Assert.Equal(0, e.Ana.IntentosFallidos);
    }

    [Fact]
    public async Task UsuarioInexistente_NoRevelaQueNoExiste()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("nadie", Clave));
    }

    [Fact]
    public async Task EmpleadoInactivo_NoEntraYCuentaElIntento()
    {
        var e = new Escenario();
        e.Ana.Empleado!.Activo = false;

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", Clave));

        Assert.False(e.Sesion.EstaAutenticada);
        Assert.Equal(1, e.Ana.IntentosFallidos);
    }

    [Fact]
    public async Task PinCorrecto_IniciaSesion()
    {
        var e = new Escenario();
        e.Ana.HashPin = "h:1234";

        await e.Servicio.IniciarSesionConPinAsync("ana", "1234");

        Assert.True(e.Sesion.EstaAutenticada);
        Assert.True(e.Sesion.Tiene(Permiso.Vender));
    }

    [Fact]
    public async Task CambiarClave_LimpiaElCambioObligatorio()
    {
        var e = new Escenario();
        e.Ana.DebeCambiarClave = true;
        await e.Servicio.IniciarSesionAsync("ana", Clave);

        await e.Servicio.CambiarClaveAsync(Clave, "nueva-clave-1");

        Assert.False(e.Ana.DebeCambiarClave);
        Assert.True(new HasheadorFalso()
            .Verificar("nueva-clave-1", e.Ana.HashClave));
    }
}
