using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync();
    Task AgregarAsync(Usuario entidad);
    void Actualizar(Usuario entidad);
    Task<int> GuardarAsync();

    // Debe incluir Empleado y su Rol.
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario);
}

}

namespace Supermercado.Datos.Repositorios
{
public class UsuarioRepositorio(SigsDbContext db)
    : RepositorioBase(db), IUsuarioRepositorio
{
    public async Task<Usuario?> ObtenerPorIdAsync(int id)
        => await Db.Set<Usuario>().FindAsync(id);

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
        => await Db.Set<Usuario>().ToListAsync();

    public virtual async Task AgregarAsync(Usuario entidad)
        => await Db.Set<Usuario>().AddAsync(entidad);

    public void Actualizar(Usuario entidad) => Db.Set<Usuario>().Update(entidad);

    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Db.Usuarios
            .Include("Empleado.Rol")
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
}

public class RolRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Rol?> ObtenerPorIdAsync(int id)
        => await Db.Set<Rol>().FindAsync(id);

    public async Task<IReadOnlyList<Rol>> ObtenerTodosAsync()
        => await Db.Set<Rol>().ToListAsync();

    public virtual async Task AgregarAsync(Rol entidad)
        => await Db.Set<Rol>().AddAsync(entidad);

    public void Actualizar(Rol entidad) => Db.Set<Rol>().Update(entidad);

    public Task<Rol?> ObtenerPorNombreAsync(string nombre)
        => Db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);
}

public class ParametroRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Parametro?> ObtenerPorIdAsync(int id)
        => await Db.Set<Parametro>().FindAsync(id);

    public async Task<IReadOnlyList<Parametro>> ObtenerTodosAsync()
        => await Db.Set<Parametro>().ToListAsync();

    public virtual async Task AgregarAsync(Parametro entidad)
        => await Db.Set<Parametro>().AddAsync(entidad);

    public void Actualizar(Parametro entidad)
        => Db.Set<Parametro>().Update(entidad);

    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Db.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
}

public class IntentoAccesoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<IntentoAcceso?> ObtenerPorIdAsync(int id)
        => await Db.Set<IntentoAcceso>().FindAsync(id);

    public async Task<IReadOnlyList<IntentoAcceso>> ObtenerTodosAsync()
        => await Db.Set<IntentoAcceso>().ToListAsync();

    public virtual async Task AgregarAsync(IntentoAcceso entidad)
        => await Db.Set<IntentoAcceso>().AddAsync(entidad);

    public void Actualizar(IntentoAcceso entidad)
        => Db.Set<IntentoAcceso>().Update(entidad);
}

public class RegistroAuditoriaRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<RegistroAuditoria?> ObtenerPorIdAsync(int id)
        => await Db.Set<RegistroAuditoria>().FindAsync(id);

    public async Task<IReadOnlyList<RegistroAuditoria>> ObtenerTodosAsync()
        => await Db.Set<RegistroAuditoria>().ToListAsync();

    public virtual async Task AgregarAsync(RegistroAuditoria entidad)
        => await Db.Set<RegistroAuditoria>().AddAsync(entidad);

    public void Actualizar(RegistroAuditoria entidad)
        => Db.Set<RegistroAuditoria>().Update(entidad);
}
}
