using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Clientes;

namespace Supermercado.Datos.Repositorios
{
public class ClienteRepositorio(SigsDbContext db)
    : RepositorioBase(db)
{
    public async Task<Cliente?> ObtenerPorIdAsync(int id)
        => await Db.Set<Cliente>().FindAsync(id);

    public async Task<IReadOnlyList<Cliente>> ObtenerTodosAsync()
        => await Db.Set<Cliente>().ToListAsync();

    public virtual async Task AgregarAsync(Cliente entidad)
        => await Db.Set<Cliente>().AddAsync(entidad);

    public void Actualizar(Cliente entidad) => Db.Set<Cliente>().Update(entidad);

    public Task<Cliente?> ObtenerPorDocumentoAsync(string documento)
        => Db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento);
}
}
