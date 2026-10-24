using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Personal;

namespace Supermercado.Datos.Repositorios;

public class RolRepositorio(SigsDbContext db)
    : Repositorio<Rol>(db), IRolRepositorio
{
    public Task<Rol?> ObtenerPorNombreAsync(string nombre)
        => Db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);
}
