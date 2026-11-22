using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;

namespace Supermercado.Datos.Repositorios
{
public class ProductoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Producto?> ObtenerPorIdAsync(int id)
        => await Db.Set<Producto>().FindAsync(id);

    public async Task<IReadOnlyList<Producto>> ObtenerTodosAsync()
        => await Db.Set<Producto>().ToListAsync();

    public virtual async Task AgregarAsync(Producto entidad)
        => await Db.Set<Producto>().AddAsync(entidad);

    public void Actualizar(Producto entidad) => Db.Set<Producto>().Update(entidad);

    public virtual Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
        => Db.Productos
            .Include(p => p.Impuesto)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigo);
}

public class CategoriaRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Categoria?> ObtenerPorIdAsync(int id)
        => await Db.Set<Categoria>().FindAsync(id);

    public async Task<IReadOnlyList<Categoria>> ObtenerTodosAsync()
        => await Db.Set<Categoria>().ToListAsync();

    public virtual async Task AgregarAsync(Categoria entidad)
        => await Db.Set<Categoria>().AddAsync(entidad);

    public void Actualizar(Categoria entidad)
        => Db.Set<Categoria>().Update(entidad);
}

public class ProveedorRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
        => await Db.Set<Proveedor>().FindAsync(id);

    public async Task<IReadOnlyList<Proveedor>> ObtenerTodosAsync()
        => await Db.Set<Proveedor>().ToListAsync();

    public virtual async Task AgregarAsync(Proveedor entidad)
        => await Db.Set<Proveedor>().AddAsync(entidad);

    public void Actualizar(Proveedor entidad)
        => Db.Set<Proveedor>().Update(entidad);
}

public class ImpuestoRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Impuesto?> ObtenerPorIdAsync(int id)
        => await Db.Set<Impuesto>().FindAsync(id);

    public async Task<IReadOnlyList<Impuesto>> ObtenerTodosAsync()
        => await Db.Set<Impuesto>().ToListAsync();

    public virtual async Task AgregarAsync(Impuesto entidad)
        => await Db.Set<Impuesto>().AddAsync(entidad);

    public void Actualizar(Impuesto entidad)
        => Db.Set<Impuesto>().Update(entidad);
}
}
