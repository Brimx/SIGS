using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos.Repositorios;

public class Repositorio<T>(SigsDbContext db) : IRepositorio<T>
    where T : EntidadBase
{
    protected SigsDbContext Db => db;

    public async Task<T?> ObtenerPorIdAsync(int id)
        => await db.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> ObtenerTodosAsync()
        => await db.Set<T>().ToListAsync();

    public async Task AgregarAsync(T entidad)
        => await db.Set<T>().AddAsync(entidad);

    public void Actualizar(T entidad) => db.Set<T>().Update(entidad);
}

// SaveChanges ya es una transacción: todo lo pendiente se guarda junto.
public class UnidadDeTrabajo(SigsDbContext db) : IUnidadDeTrabajo
{
    public Task<int> GuardarAsync() => db.SaveChangesAsync();
}
