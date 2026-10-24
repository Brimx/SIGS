using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos.Repositorios;

public class ParametroRepositorio(SigsDbContext db)
    : Repositorio<Parametro>(db), IParametroRepositorio
{
    public Task<Parametro?> ObtenerPorClaveAsync(string clave)
        => Db.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
}
