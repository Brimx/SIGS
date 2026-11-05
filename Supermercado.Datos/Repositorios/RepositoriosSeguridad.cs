using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Configuracion;
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

public class RolRepositorio(SigsDbContext db)
    : Repositorio<Rol>(db), IRolRepositorio
{
    public Task<Rol?> ObtenerPorNombreAsync(string nombre)
        => Db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);
}

public class ParametroRepositorio(SigsDbContext db)
    : Repositorio<Parametro>(db), IParametroRepositorio
{
    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Db.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
}
