using Supermercado.Entidades.Configuracion;

namespace Supermercado.Entidades.Contratos;

public interface IParametroRepositorio : IRepositorio<Parametro>
{
    Task<Parametro?> ObtenerPorClaveAsync(string clave);
}
