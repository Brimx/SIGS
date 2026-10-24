using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Finanzas;

namespace Supermercado.Pruebas.Falsos;

public class ParametrosEnMemoria
    : RepositorioEnMemoria<Parametro>, IParametroRepositorio
{
    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Task.FromResult(Datos.FirstOrDefault(p => p.Clave == clave));
}

public class GastosEnMemoria : RepositorioEnMemoria<Gasto>, IGastoRepositorio
{
    public Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(
        DateTime desde,
        DateTime hasta)
        => Task.FromResult<IReadOnlyList<Gasto>>(Datos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToList());
}
