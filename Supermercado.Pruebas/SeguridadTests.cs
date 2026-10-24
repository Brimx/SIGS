using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Xunit;

namespace Supermercado.Pruebas;

public class SeguridadTests
{
    [Fact]
    public void Usuario_SeBloqueaAlLlegarAlMaximoDeIntentos()
    {
        var usuario = new Usuario();

        usuario.RegistrarFallo(2);
        Assert.False(usuario.Bloqueado);

        usuario.RegistrarFallo(2);
        Assert.True(usuario.Bloqueado);
    }

    [Fact]
    public void Usuario_DesbloquearReiniciaLosIntentos()
    {
        var usuario = new Usuario { Bloqueado = true, IntentosFallidos = 5 };

        usuario.Desbloquear();

        Assert.False(usuario.Bloqueado);
        Assert.Equal(0, usuario.IntentosFallidos);
    }

    [Fact]
    public void Rol_ExigeTodosLosPermisosPedidos()
    {
        var cajero = new Rol
        {
            Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno
        };

        Assert.True(cajero.Tiene(Permiso.Vender));
        Assert.False(cajero.Tiene(Permiso.AnularVenta));
        Assert.False(cajero.Tiene(Permiso.Vender | Permiso.AnularVenta));
    }

    [Fact]
    public void Sesion_SinPermisoLanzaAccesoDenegado()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);

        Assert.Throws<AccesoDenegadoException>(
            () => sesion.Exigir(Permiso.VerFinanzas));
    }

    [Fact]
    public void Sesion_SinIniciarNoTienePermisos()
    {
        Assert.False(new SesionActual().Tiene(Permiso.Vender));
    }

    [Fact]
    public void Sesion_EmpleadoInactivo_CierraLaSesion()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);
        sesion.Usuario!.Empleado!.Activo = false;

        Assert.False(sesion.Tiene(Permiso.Vender));
        Assert.False(sesion.EstaAutenticada);
    }

    [Fact]
    public void Sesion_ConCambioDeClavePendiente_ExigeCambiarla()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);
        sesion.Usuario!.DebeCambiarClave = true;

        Assert.True(sesion.DebeCambiarClave);
        Assert.Throws<DebeCambiarClaveException>(
            () => sesion.Exigir(Permiso.Vender));
    }

    [Fact]
    public void Usuario_RechazaUnMaximoDeIntentosNoPositivo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Usuario().RegistrarFallo(0));
    }

    [Fact]
    public void Usuario_RechazaUnaClaveVacia()
    {
        Assert.Throws<ArgumentException>(
            () => new Usuario().CambiarClave("  "));
    }

    [Fact]
    public void Bcrypt_GeneraHasheadosVerificables()
    {
        var hasheador = new HasheadorBCrypt();
        var hash = hasheador.Hashear("clave-segura-1");

        Assert.NotEqual("clave-segura-1", hash);
        Assert.True(hasheador.Verificar("clave-segura-1", hash));
        Assert.False(hasheador.Verificar("otra-clave", hash));
    }
}
