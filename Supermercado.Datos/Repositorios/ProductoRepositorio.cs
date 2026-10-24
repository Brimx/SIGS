using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Productos;

namespace Supermercado.Datos.Repositorios;

public class ProductoRepositorio(SigsDbContext db)
    : Repositorio<Producto>(db), IProductoRepositorio
{
    public Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
        => Db.Productos
            .Include(p => p.Impuesto)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigo);
}
