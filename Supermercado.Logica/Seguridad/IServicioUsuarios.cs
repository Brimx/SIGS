using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Personal;

namespace Supermercado.Logica.Seguridad;

public interface IServicioUsuarios
{
    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync();
    Task DesbloquearAsync(int usuarioId);
    Task<string> RestablecerClaveTemporalAsync(int usuarioId);
}

// Adaptador para compatibilidad de pruebas existentes
public class ServicioUsuarios(
    IUsuarioRepositorio usuarios,
    IHasheadorClave hasheador,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : Personal.ServicioPersonal(
        null!,
        null!,
        null!,
        usuarios,
        null!,
        null!,
        hasheador,
        auditoria,
        sesion,
        trabajo,
        TimeProvider.System);
