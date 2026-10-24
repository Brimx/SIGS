using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Personal;

namespace Supermercado.Datos.Repositorios;

public class UsuarioRepositorio(SigsDbContext db)
    : Repositorio<Usuario>(db), IUsuarioRepositorio
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Db.Usuarios
            .Include("Empleado.Rol")
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
}
