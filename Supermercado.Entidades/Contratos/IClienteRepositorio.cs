using Supermercado.Entidades.Clientes;

namespace Supermercado.Entidades.Contratos;

public interface IClienteRepositorio : IRepositorio<Cliente>
{
    Task<Cliente?> ObtenerPorDocumentoAsync(string documento);
}
