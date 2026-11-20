using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Clientes;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface IClienteRepositorio
{
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Cliente>> ObtenerTodosAsync();
    Task AgregarAsync(Cliente entidad);
    void Actualizar(Cliente entidad);
    Task<int> GuardarAsync();

    Task<Cliente?> ObtenerPorDocumentoAsync(string documento);
}
}

namespace Supermercado.Datos.Repositorios
{
public class ClienteRepositorio(SigsDbContext db)
    : RepositorioBase(db), IClienteRepositorio
{
    public async Task<Cliente?> ObtenerPorIdAsync(int id)
        => await Db.Set<Cliente>().FindAsync(id);

    public async Task<IReadOnlyList<Cliente>> ObtenerTodosAsync()
        => await Db.Set<Cliente>().ToListAsync();

    public async Task AgregarAsync(Cliente entidad)
        => await Db.Set<Cliente>().AddAsync(entidad);

    public void Actualizar(Cliente entidad) => Db.Set<Cliente>().Update(entidad);

    public Task<Cliente?> ObtenerPorDocumentoAsync(string documento)
        => Db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento);
}
}
