using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Sincronizacion;

namespace Supermercado.Datos.Repositorios;

public class ProductoRepositorio(SigsDbContext db)
    : Repositorio<Producto>(db), IProductoRepositorio
{
    public Task<Producto?> ObtenerPorCodigoBarrasAsync(string codigo)
        => Db.Productos
            .Include(p => p.Impuesto)
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigo);
}

public class ClienteRepositorio(SigsDbContext db)
    : Repositorio<Cliente>(db), IClienteRepositorio
{
    public Task<Cliente?> ObtenerPorDocumentoAsync(string documento)
        => Db.Clientes.FirstOrDefaultAsync(c => c.Documento == documento);
}

public class GastoRepositorio(SigsDbContext db)
    : Repositorio<Gasto>(db), IGastoRepositorio
{
    public async Task<IReadOnlyList<Gasto>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta)
        => await Db.Gastos
            .Where(g => g.Fecha >= desde && g.Fecha < hasta)
            .ToListAsync();
}

public class ConfiguracionWidgetRepositorio(SigsDbContext db)
    : Repositorio<ConfiguracionWidget>(db), IConfiguracionWidgetRepositorio
{
    public async Task<IReadOnlyList<ConfiguracionWidget>> ObtenerPorEmpleadoAsync(int empleadoId)
        => await Db.Widgets
            .Where(c => c.EmpleadoId == empleadoId)
            .OrderBy(c => c.Posicion)
            .ToListAsync();
}

public class EventoOutboxRepositorio(SigsDbContext db)
    : Repositorio<EventoOutbox>(db), IEventoOutboxRepositorio
{
    public async Task<IReadOnlyList<EventoOutbox>> ObtenerPendientesAsync(int limite = 50)
        => await Db.EventosOutbox
            .Where(e => !e.Procesado)
            .OrderBy(e => e.Id)
            .Take(limite)
            .ToListAsync();
}
