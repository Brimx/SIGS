using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioUsuariosTests
{
    private readonly UsuariosEnMemoria usuarios = new();
    private readonly HasheadorFalso hasheador = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioUsuarios CrearServicio(
        Permiso permisos = Permiso.GestionarPersonal)
        => new(
            usuarios,
            hasheador,
            auditoria,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public async Task ObtenerTodosAsync_ConPermiso_RetornaUsuarios()
    {
        var servicio = CrearServicio();
        await usuarios.AgregarAsync(new Usuario
        {
            Id = 1,
            NombreUsuario = "u1"
        });
        await usuarios.AgregarAsync(new Usuario
        {
            Id = 2,
            NombreUsuario = "u2"
        });

        var lista = await servicio.ObtenerTodosAsync();

        Assert.Equal(2, lista.Count);
    }

    [Fact]
    public async Task ObtenerTodosAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.ObtenerTodosAsync());
    }

    [Fact]
    public async Task DesbloquearAsync_ConPermiso_DesbloqueaYAudita()
    {
        var servicio = CrearServicio();
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "bloqueado",
            Bloqueado = true,
            IntentosFallidos = 5
        };
        await usuarios.AgregarAsync(usuario);

        await servicio.DesbloquearAsync(1);

        Assert.False(usuario.Bloqueado);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Contains(
            AccionAuditoria.UsuarioDesbloqueado,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task DesbloquearAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Ninguno);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.DesbloquearAsync(1));
    }

    [Fact]
    public async Task DesbloquearAsync_UsuarioInexistente_LanzaError()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.DesbloquearAsync(999));
    }

    [Fact]
    public async Task RestablecerClaveTemporal_AsignaHashObligaCambioYAudita()
    {
        var servicio = CrearServicio();
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = "juan",
            HashClave = "antiguo",
            Bloqueado = true,
            DebeCambiarClave = false
        };
        await usuarios.AgregarAsync(usuario);

        var temporal = await servicio.RestablecerClaveTemporalAsync(1);

        Assert.NotEmpty(temporal);
        Assert.Equal("h:" + temporal, usuario.HashClave);
        Assert.True(usuario.DebeCambiarClave);
        Assert.False(usuario.Bloqueado);
        Assert.Contains(
            AccionAuditoria.ClaveRestablecida,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task RestablecerClaveTemporal_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.VerInventario);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.RestablecerClaveTemporalAsync(1));
    }

    [Fact]
    public async Task RestablecerClaveTemporal_Inexistente_LanzaError()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RestablecerClaveTemporalAsync(999));
    }
}
