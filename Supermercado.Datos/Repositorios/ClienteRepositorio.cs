using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos.Repositorios;

public class ClienteRepositorio(SigsDbContext db)
    : Repositorio<Cliente>(db), IClienteRepositorio
{
    public Task<Cliente?> ObtenerPorDocumentoAsync(string documento)
        => Db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento);
}
