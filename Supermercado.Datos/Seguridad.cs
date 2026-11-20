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

public interface IRolRepositorio
{
    Task<Rol?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Rol>> ObtenerTodosAsync();
    Task AgregarAsync(Rol entidad);
    void Actualizar(Rol entidad);
    Task<int> GuardarAsync();

    Task<Rol?> ObtenerPorNombreAsync(string nombre);
}

public interface IParametroRepositorio
{
    Task<Parametro?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Parametro>> ObtenerTodosAsync();
    Task AgregarAsync(Parametro entidad);
    void Actualizar(Parametro entidad);
    Task<int> GuardarAsync();

    Task<Parametro?> ObtenerPorClaveAsync(string clave);
}

public interface IIntentoAccesoRepositorio
{
    Task<IntentoAcceso?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<IntentoAcceso>> ObtenerTodosAsync();
    Task AgregarAsync(IntentoAcceso entidad);
    void Actualizar(IntentoAcceso entidad);
    Task<int> GuardarAsync();
}

public interface IRegistroAuditoriaRepositorio
{
    Task<RegistroAuditoria?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<RegistroAuditoria>> ObtenerTodosAsync();
    Task AgregarAsync(RegistroAuditoria entidad);
    void Actualizar(RegistroAuditoria entidad);
    Task<int> GuardarAsync();
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

    public async Task AgregarAsync(Usuario entidad)
        => await Db.Set<Usuario>().AddAsync(entidad);

    public void Actualizar(Usuario entidad) => Db.Set<Usuario>().Update(entidad);

    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
        => Db.Usuarios
            .Include("Empleado.Rol")
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
}

public class RolRepositorio(SigsDbContext db)
    : RepositorioBase(db), IRolRepositorio
{
    public async Task<Rol?> ObtenerPorIdAsync(int id)
        => await Db.Set<Rol>().FindAsync(id);

    public async Task<IReadOnlyList<Rol>> ObtenerTodosAsync()
        => await Db.Set<Rol>().ToListAsync();

    public async Task AgregarAsync(Rol entidad)
        => await Db.Set<Rol>().AddAsync(entidad);

    public void Actualizar(Rol entidad) => Db.Set<Rol>().Update(entidad);

    public Task<Rol?> ObtenerPorNombreAsync(string nombre)
        => Db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);
}

public class ParametroRepositorio(SigsDbContext db)
    : RepositorioBase(db), IParametroRepositorio
{
    public async Task<Parametro?> ObtenerPorIdAsync(int id)
        => await Db.Set<Parametro>().FindAsync(id);

    public async Task<IReadOnlyList<Parametro>> ObtenerTodosAsync()
        => await Db.Set<Parametro>().ToListAsync();

    public async Task AgregarAsync(Parametro entidad)
        => await Db.Set<Parametro>().AddAsync(entidad);

    public void Actualizar(Parametro entidad)
        => Db.Set<Parametro>().Update(entidad);

    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Db.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
}

public class IntentoAccesoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IIntentoAccesoRepositorio
{
    public async Task<IntentoAcceso?> ObtenerPorIdAsync(int id)
        => await Db.Set<IntentoAcceso>().FindAsync(id);

    public async Task<IReadOnlyList<IntentoAcceso>> ObtenerTodosAsync()
        => await Db.Set<IntentoAcceso>().ToListAsync();

    public async Task AgregarAsync(IntentoAcceso entidad)
        => await Db.Set<IntentoAcceso>().AddAsync(entidad);

    public void Actualizar(IntentoAcceso entidad)
        => Db.Set<IntentoAcceso>().Update(entidad);
}

public class RegistroAuditoriaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IRegistroAuditoriaRepositorio
{
    public async Task<RegistroAuditoria?> ObtenerPorIdAsync(int id)
        => await Db.Set<RegistroAuditoria>().FindAsync(id);

    public async Task<IReadOnlyList<RegistroAuditoria>> ObtenerTodosAsync()
        => await Db.Set<RegistroAuditoria>().ToListAsync();

    public async Task AgregarAsync(RegistroAuditoria entidad)
        => await Db.Set<RegistroAuditoria>().AddAsync(entidad);

    public void Actualizar(RegistroAuditoria entidad)
        => Db.Set<RegistroAuditoria>().Update(entidad);
}
}
