using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;

using Supermercado.Entidades.Contratos;

namespace Supermercado.Entidades.Contratos
{
public interface ILoteRepositorio
{
    Task<Lote?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Lote>> ObtenerTodosAsync();
    Task AgregarAsync(Lote entidad);
    void Actualizar(Lote entidad);
    Task<int> GuardarAsync();

    // Solo lotes que todavía tienen existencias.
    Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta);
}

public interface IMovimientoRepositorio
{
    Task<MovimientoInventario?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<MovimientoInventario>> ObtenerTodosAsync();
    Task AgregarAsync(MovimientoInventario entidad);
    void Actualizar(MovimientoInventario entidad);
    Task<int> GuardarAsync();

    Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(
        int ventaId);
}

public interface IReabastecimientoRepositorio
{
    Task<Reabastecimiento?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Reabastecimiento>> ObtenerTodosAsync();
    Task AgregarAsync(Reabastecimiento entidad);
    void Actualizar(Reabastecimiento entidad);
    Task<int> GuardarAsync();

    Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync();
}

public interface IStockEstanteRepositorio
{
    Task<StockEstante?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<StockEstante>> ObtenerTodosAsync();
    Task AgregarAsync(StockEstante entidad);
    void Actualizar(StockEstante entidad);
    Task<int> GuardarAsync();

    // Debe incluir el Lote: la estrategia de salida necesita su fecha.
    Task<IReadOnlyList<StockEstante>> ObtenerPorProductoAsync(int productoId);
    Task<StockEstante?> ObtenerPorLoteAsync(int loteId);
    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync();
}

public interface IStockBodegaRepositorio
{
    Task<StockBodega?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<StockBodega>> ObtenerTodosAsync();
    Task AgregarAsync(StockBodega entidad);
    void Actualizar(StockBodega entidad);
    Task<int> GuardarAsync();

    // Debe incluir el Lote: la estrategia de salida necesita su fecha.
    Task<IReadOnlyList<StockBodega>> ObtenerPorProductoAsync(int productoId);
    Task<StockBodega?> ObtenerPorLoteAsync(int loteId);
    Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync();
}

public interface IMermaRepositorio
{
    Task<Merma?> ObtenerPorIdAsync(int id);
    Task<IReadOnlyList<Merma>> ObtenerTodosAsync();
    Task AgregarAsync(Merma entidad);
    void Actualizar(Merma entidad);
    Task<int> GuardarAsync();
}
}

namespace Supermercado.Datos.Repositorios
{
public class LoteRepositorio(SigsDbContext db)
    : RepositorioBase(db), ILoteRepositorio
{
    public async Task<Lote?> ObtenerPorIdAsync(int id)
        => await Db.Set<Lote>().FindAsync(id);

    public async Task<IReadOnlyList<Lote>> ObtenerTodosAsync()
        => await Db.Set<Lote>().ToListAsync();

    public async Task AgregarAsync(Lote entidad)
        => await Db.Set<Lote>().AddAsync(entidad);

    public void Actualizar(Lote entidad) => Db.Set<Lote>().Update(entidad);

    public async Task<IReadOnlyList<Lote>> ObtenerPorVencerAsync(DateOnly hasta)
        => await Db.Lotes
            .Include(l => l.Producto)
            .Where(l => l.FechaVencimiento <= hasta
                && (Db.StockEstante.Any(s => s.LoteId == l.Id && s.Cantidad > 0)
                    || Db.StockBodega.Any(s => s.LoteId == l.Id && s.Cantidad > 0)))
            .OrderBy(l => l.FechaVencimiento)
            .ToListAsync();
}

public class MovimientoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IMovimientoRepositorio
{
    public async Task<MovimientoInventario?> ObtenerPorIdAsync(int id)
        => await Db.Set<MovimientoInventario>().FindAsync(id);

    public async Task<IReadOnlyList<MovimientoInventario>> ObtenerTodosAsync()
        => await Db.Set<MovimientoInventario>().ToListAsync();

    public async Task AgregarAsync(MovimientoInventario entidad)
        => await Db.Set<MovimientoInventario>().AddAsync(entidad);

    public void Actualizar(MovimientoInventario entidad)
        => Db.Set<MovimientoInventario>().Update(entidad);

    public async Task<IReadOnlyList<MovimientoInventario>> ObtenerSalidasDeVentaAsync(int ventaId)
        => await Db.Movimientos
            .Where(m => m.VentaId == ventaId && m.Tipo == TipoMovimiento.Venta)
            .ToListAsync();
}

public class ReabastecimientoRepositorio(SigsDbContext db)
    : RepositorioBase(db), IReabastecimientoRepositorio
{
    public async Task<Reabastecimiento?> ObtenerPorIdAsync(int id)
        => await Db.Set<Reabastecimiento>().FindAsync(id);

    public async Task<IReadOnlyList<Reabastecimiento>> ObtenerTodosAsync()
        => await Db.Set<Reabastecimiento>().ToListAsync();

    public async Task AgregarAsync(Reabastecimiento entidad)
        => await Db.Set<Reabastecimiento>().AddAsync(entidad);

    public void Actualizar(Reabastecimiento entidad)
        => Db.Set<Reabastecimiento>().Update(entidad);

    public async Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
        => await Db.Reabastecimientos
            .Where(r => r.Estado == EstadoReabastecimiento.Pendiente)
            .ToListAsync();
}

public class StockEstanteRepositorio(SigsDbContext db)
    : RepositorioBase(db), IStockEstanteRepositorio
{
    public async Task<StockEstante?> ObtenerPorIdAsync(int id)
        => await Db.Set<StockEstante>().FindAsync(id);

    public async Task<IReadOnlyList<StockEstante>> ObtenerTodosAsync()
        => await Db.Set<StockEstante>().ToListAsync();

    public async Task AgregarAsync(StockEstante entidad)
        => await Db.Set<StockEstante>().AddAsync(entidad);

    public void Actualizar(StockEstante entidad)
        => Db.Set<StockEstante>().Update(entidad);

    public async Task<IReadOnlyList<StockEstante>> ObtenerPorProductoAsync(
        int productoId)
        => await Db.Set<StockEstante>()
            .Include(s => s.Lote)
            .Where(s => s.ProductoId == productoId && s.Cantidad > 0)
            .ToListAsync();

    public Task<StockEstante?> ObtenerPorLoteAsync(int loteId)
        => Db.Set<StockEstante>()
            .FirstOrDefaultAsync(s => s.LoteId == loteId);

    public async Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync()
        => await Db.Set<StockEstante>()
            .GroupBy(s => s.ProductoId)
            .Select(g => new { Id = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
}

public class StockBodegaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IStockBodegaRepositorio
{
    public async Task<StockBodega?> ObtenerPorIdAsync(int id)
        => await Db.Set<StockBodega>().FindAsync(id);

    public async Task<IReadOnlyList<StockBodega>> ObtenerTodosAsync()
        => await Db.Set<StockBodega>().ToListAsync();

    public async Task AgregarAsync(StockBodega entidad)
        => await Db.Set<StockBodega>().AddAsync(entidad);

    public void Actualizar(StockBodega entidad)
        => Db.Set<StockBodega>().Update(entidad);

    public async Task<IReadOnlyList<StockBodega>> ObtenerPorProductoAsync(
        int productoId)
        => await Db.Set<StockBodega>()
            .Include(s => s.Lote)
            .Where(s => s.ProductoId == productoId && s.Cantidad > 0)
            .ToListAsync();

    public Task<StockBodega?> ObtenerPorLoteAsync(int loteId)
        => Db.Set<StockBodega>()
            .FirstOrDefaultAsync(s => s.LoteId == loteId);

    public async Task<IReadOnlyDictionary<int, int>> ObtenerTotalesPorProductoAsync()
        => await Db.Set<StockBodega>()
            .GroupBy(s => s.ProductoId)
            .Select(g => new { Id = g.Key, Total = g.Sum(s => s.Cantidad) })
            .ToDictionaryAsync(x => x.Id, x => x.Total);
}

public class MermaRepositorio(SigsDbContext db)
    : RepositorioBase(db), IMermaRepositorio
{
    public async Task<Merma?> ObtenerPorIdAsync(int id)
        => await Db.Set<Merma>().FindAsync(id);

    public async Task<IReadOnlyList<Merma>> ObtenerTodosAsync()
        => await Db.Set<Merma>().ToListAsync();

    public async Task AgregarAsync(Merma entidad)
        => await Db.Set<Merma>().AddAsync(entidad);

    public void Actualizar(Merma entidad) => Db.Set<Merma>().Update(entidad);
}
}
