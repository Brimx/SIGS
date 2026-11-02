using Supermercado.Entidades.Clientes;

namespace Supermercado.Logica.Clientes;

public interface IServicioClientes
{
    Task<Cliente> CrearAsync(
        string documento,
        string nombres,
        string apellidos,
        string telefono,
        string correo,
        bool autorizaTratamientoDatos = true);

    Task EditarAsync(
        int id,
        string documento,
        string nombres,
        string apellidos,
        string telefono,
        string correo,
        bool autorizaTratamientoDatos = true);

    Task DesactivarAsync(int id);

    // Caja: busca al cliente por documento (null si no existe).
    Task<Cliente?> BuscarPorDocumentoAsync(string documento);

    Task<IReadOnlyList<Cliente>> ObtenerTodosAsync();
}
