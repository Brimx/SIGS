using System.Security.Cryptography;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public class ServicioUsuarios(
    IUsuarioRepositorio usuarios,
    IHasheadorClave hasheador,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioUsuarios
{
    private const string CaracteresClave =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
    {
        sesion.Exigir(Permiso.GestionarPersonal);
        return await usuarios.ObtenerTodosAsync();
    }

    public async Task DesbloquearAsync(int usuarioId)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId)
            ?? throw new ValidacionException("El usuario no existe.");

        usuario.Desbloquear();
        usuarios.Actualizar(usuario);

        await auditoria.RegistrarAsync(
            AccionAuditoria.UsuarioDesbloqueado,
            nameof(Usuario),
            usuario.Id,
            $"Usuario {usuario.NombreUsuario} desbloqueado");
        await trabajo.GuardarAsync();
    }

    public async Task<string> RestablecerClaveTemporalAsync(int usuarioId)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId)
            ?? throw new ValidacionException("El usuario no existe.");

        var claveTemporal = GenerarClaveTemporal();
        usuario.HashClave = hasheador.Hashear(claveTemporal);
        usuario.DebeCambiarClave = true;
        usuario.Desbloquear();

        usuarios.Actualizar(usuario);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ClaveRestablecida,
            nameof(Usuario),
            usuario.Id,
            $"Clave restablecida para {usuario.NombreUsuario}");
        await trabajo.GuardarAsync();

        return claveTemporal;
    }

    private static string GenerarClaveTemporal(int longitud = 8)
    {
        var chars = new char[longitud];
        for (var i = 0; i < longitud; i++)
        {
            var indice = RandomNumberGenerator.GetInt32(CaracteresClave.Length);
            chars[i] = CaracteresClave[indice];
        }
        return new string(chars);
    }
}
