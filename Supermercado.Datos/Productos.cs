using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IProductoRepositorio
{
    Task<Producto?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Producto>> ObtenerTodosAsync();
    Task AgregarAsync(Producto entidad);
    void Actualizar(Producto entidad);
    Task<int> GuardarAsync();

    // Debe incluir el Impuesto: la venta lo necesita para el IVA.
    Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo);
}

public interface ICategoriaRepositorio
{
    Task<Categoria?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Categoria>> ObtenerTodosAsync();
    Task AgregarAsync(Categoria entidad);
    void Actualizar(Categoria entidad);
    Task<int> GuardarAsync();
}

public interface IProveedorRepositorio
{
    Task<Proveedor?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Proveedor>> ObtenerTodosAsync();
    Task AgregarAsync(Proveedor entidad);
    void Actualizar(Proveedor entidad);
    Task<int> GuardarAsync();
}

public interface IImpuestoRepositorio
{
    Task<Impuesto?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Impuesto>> ObtenerTodosAsync();
    Task AgregarAsync(Impuesto entidad);
    void Actualizar(Impuesto entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class ProductoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IProductoRepositorio
{
    public async Task<Producto?> ObtenerPorIdAsync(int id)
        => await db.Set<Producto>().FindAsync(id);

    public async Task<IReadOnlyList<Producto>> ObtenerTodosAsync()
        => await db.Set<Producto>().ToListAsync();

    public async Task AgregarAsync(Producto entidad)
        => await db.Set<Producto>().AddAsync(entidad);

    public void Actualizar(Producto entidad) => db.Set<Producto>().Update(entidad);

    public Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
        => Db.Productos
            .Include(p => p.Impuesto)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigo);
}

public class CategoriaRepositorio(SigsDbContext db)
    : RepositorioBase(db), ICategoriaRepositorio
{
    public async Task<Categoria?> ObtenerPorIdAsync(int id)
        => await db.Set<Categoria>().FindAsync(id);

    public async Task<IReadOnlyList<Categoria>> ObtenerTodosAsync()
        => await db.Set<Categoria>().ToListAsync();

    public async Task AgregarAsync(Categoria entidad)
        => await db.Set<Categoria>().AddAsync(entidad);

    public void Actualizar(Categoria entidad)
        => db.Set<Categoria>().Update(entidad);
}

public class ProveedorRepositorio(SigsDbContext db)
    : RepositorioBase(db), IProveedorRepositorio
{
    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
        => await db.Set<Proveedor>().FindAsync(id);

    public async Task<IReadOnlyList<Proveedor>> ObtenerTodosAsync()
        => await db.Set<Proveedor>().ToListAsync();

    public async Task AgregarAsync(Proveedor entidad)
        => await db.Set<Proveedor>().AddAsync(entidad);

    public void Actualizar(Proveedor entidad)
        => db.Set<Proveedor>().Update(entidad);
}

public class ImpuestoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IImpuestoRepositorio
{
    public async Task<Impuesto?> ObtenerPorIdAsync(int id)
        => await db.Set<Impuesto>().FindAsync(id);

    public async Task<IReadOnlyList<Impuesto>> ObtenerTodosAsync()
        => await db.Set<Impuesto>().ToListAsync();

    public async Task AgregarAsync(Impuesto entidad)
        => await db.Set<Impuesto>().AddAsync(entidad);

    public void Actualizar(Impuesto entidad)
        => db.Set<Impuesto>().Update(entidad);
}
}
