using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Inventario;

public class ServicioConsultaInventario(
    IStockRepositorio<StockEstante> estante,
    IStockRepositorio<StockBodega> bodega,
    IProductoRepositorio productos,
    ILoteRepositorio lotes,
    IMovimientoRepositorio movimientos,
    ISesionActual sesion) : IServicioConsultaInventario
{
    public async Task<IReadOnlyList<StockConsolidado>>
        ObtenerStockConsolidadoAsync(int? productoId = null)
    {
        sesion.Exigir(Permiso.VerInventario);

        var listaProductos = await productos.ObtenerTodosAsync();
        if (productoId.HasValue)
            listaProductos = listaProductos
                .Where(p => p.Id == productoId.Value)
                .ToList();

        var todosLotes = await lotes.ObtenerTodosAsync();
        var stockEstante = await estante.ObtenerTodosAsync();
        var stockBodega = await bodega.ObtenerTodosAsync();

        var estantePorLote = stockEstante
            .ToDictionary(s => s.LoteId, s => s.Cantidad);
        var bodegaPorLote = stockBodega
            .ToDictionary(s => s.LoteId, s => s.Cantidad);

        var resultado = new List<StockConsolidado>();

        foreach (var p in listaProductos)
        {
            var lotesDelProducto = todosLotes
                .Where(l => l.ProductoId == p.Id)
                .OrderBy(l => l.FechaVencimiento);

            foreach (var l in lotesDelProducto)
            {
                var cantEstante = estantePorLote.GetValueOrDefault(l.Id, 0);
                var cantBodega = bodegaPorLote.GetValueOrDefault(l.Id, 0);

                resultado.Add(new StockConsolidado(
                    p.Id,
                    p.Nombre,
                    p.CodigoBarras,
                    l.Id,
                    l.NumeroLote,
                    l.FechaVencimiento,
                    cantEstante,
                    cantBodega,
                    cantEstante + cantBodega));
            }
        }

        return resultado;
    }

    public async Task<IReadOnlyList<KardexMovimiento>> ObtenerKardexAsync(
        int productoId,
        DateTime desde,
        DateTime hasta)
    {
        sesion.Exigir(Permiso.VerInventario);

        var todosLotes = await lotes.ObtenerTodosAsync();
        var lotesDict = todosLotes.ToDictionary(l => l.Id, l => l.NumeroLote);

        var todosMovimientos = await movimientos.ObtenerTodosAsync();
        var filtrados = todosMovimientos
            .Where(m => m.ProductoId == productoId
                && m.Fecha >= desde
                && m.Fecha <= hasta)
            .OrderBy(m => m.Fecha)
            .ThenBy(m => m.Id)
            .ToList();

        var resultado = new List<KardexMovimiento>();
        var saldo = 0;

        foreach (var m in filtrados)
        {
            saldo += m.Tipo switch
            {
                TipoMovimiento.Ingreso or TipoMovimiento.Devolucion
                    => m.Cantidad,
                TipoMovimiento.Venta or TipoMovimiento.Merma
                    => -m.Cantidad,
                _ => 0
            };

            var numLote = m.Lote?.NumeroLote
                ?? lotesDict.GetValueOrDefault(m.LoteId, string.Empty);

            resultado.Add(new KardexMovimiento(
                m.Id,
                m.Fecha,
                m.Tipo,
                m.LoteId,
                numLote,
                m.Cantidad,
                saldo,
                m.Motivo));
        }

        return resultado;
    }
}
