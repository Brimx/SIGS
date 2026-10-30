using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Inventario;

public class ServicioInventario(
    IStockRepositorio<StockEstante> estante,
    IStockRepositorio<StockBodega> bodega,
    IMovimientoRepositorio movimientos,
    IRepositorio<Merma> mermas,
    IEstrategiaSalida estrategia,
    TimeProvider reloj) : IServicioInventario
{
    public async Task DescontarVentaAsync(
        int productoId,
        int cantidad,
        Venta venta)
    {
        var filas = await OrdenarAsyncParaVentaAsync(estante, productoId);

        foreach (var (fila, tomar) in PlanDeSalida.Repartir(filas, cantidad))
        {
            fila.Disminuir(tomar);
            await RegistrarAsync(fila, TipoMovimiento.Venta, tomar, venta);
        }
    }

    public async Task ReintegrarVentaAsync(int ventaId)
    {
        var salidas = await movimientos.ObtenerSalidasDeVentaAsync(ventaId);

        foreach (var salida in salidas)
        {
            var destino = await ObtenerOCrearEstanteAsync(
                salida.ProductoId, salida.LoteId);
            destino.Aumentar(salida.Cantidad);

            await movimientos.AgregarAsync(new MovimientoInventario
            {
                ProductoId = salida.ProductoId,
                LoteId = salida.LoteId,
                VentaId = ventaId,
                Tipo = TipoMovimiento.Devolucion,
                Cantidad = salida.Cantidad,
                Fecha = reloj.AhoraUtc(),
                Motivo = "Anulación de venta"
            });
        }
    }

    public async Task TrasladarAEstanteAsync(int productoId, int cantidad)
    {
        var filas = await OrdenarAsyncSinFiltrarVencidosAsync(
            bodega, productoId);

        foreach (var (origen, tomar) in PlanDeSalida.Repartir(filas, cantidad))
        {
            origen.Disminuir(tomar);
            var destino = await ObtenerOCrearEstanteAsync(
                origen.ProductoId, origen.LoteId);
            destino.Aumentar(tomar);
            await RegistrarAsync(origen, TipoMovimiento.Traslado, tomar);
        }
    }

    public async Task RegistrarIngresoAsync(Lote lote, int cantidad)
    {
        if (lote.ProductoId <= 0)
            throw new ValidacionException(
                "El lote no está asociado a un producto.");

        var fila = new StockBodega
        {
            ProductoId = lote.ProductoId,
            Lote = lote
        };
        fila.Aumentar(cantidad);

        await bodega.AgregarAsync(fila);
        await RegistrarAsync(fila, TipoMovimiento.Ingreso, cantidad);
    }

    public async Task RegistrarMermaAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo)
    {
        var fila = await BuscarFilaAsync(loteId, ubicacion);
        fila.Disminuir(cantidad);

        await mermas.AgregarAsync(new Merma
        {
            ProductoId = productoId,
            LoteId = loteId,
            Cantidad = cantidad,
            Motivo = motivo,
            Fecha = reloj.AhoraUtc()
        });
        await RegistrarAsync(
            fila, TipoMovimiento.Merma, cantidad, motivo: motivo);
    }

    private async Task<StockLote> BuscarFilaAsync(
        int loteId,
        UbicacionStock ubicacion)
    {
        StockLote? fila = ubicacion == UbicacionStock.Estante
            ? await estante.ObtenerPorLoteAsync(loteId)
            : await bodega.ObtenerPorLoteAsync(loteId);

        return fila
            ?? throw new StockInsuficienteException($"lote {loteId}");
    }

    private async Task<List<T>> OrdenarAsyncParaVentaAsync<T>(
        IStockRepositorio<T> repositorio,
        int productoId) where T : StockLote
    {
        var filas = await repositorio.ObtenerPorProductoAsync(productoId);
        return estrategia.Ordenar(filas, reloj.HoyLocal()).ToList();
    }

    private async Task<List<T>> OrdenarAsyncSinFiltrarVencidosAsync<T>(
        IStockRepositorio<T> repositorio,
        int productoId) where T : StockLote
    {
        var filas = await repositorio.ObtenerPorProductoAsync(productoId);
        return filas
            .OrderBy(s => s.Lote.FechaVencimiento)
            .ToList();
    }

    private async Task<StockEstante> ObtenerOCrearEstanteAsync(
        int productoId,
        int loteId)
    {
        var existente = await estante.ObtenerPorLoteAsync(loteId);
        if (existente is not null)
        {
            if (existente.ProductoId != productoId)
                throw new ValidacionException(
                    "El lote pertenece a otro producto.");

            return existente;
        }

        var nuevo = new StockEstante
        {
            ProductoId = productoId,
            LoteId = loteId
        };
        await estante.AgregarAsync(nuevo);
        return nuevo;
    }

    private Task RegistrarAsync(
        StockLote fila,
        TipoMovimiento tipo,
        int cantidad,
        Venta? venta = null,
        string motivo = "")
        => movimientos.AgregarAsync(new MovimientoInventario
        {
            ProductoId = fila.ProductoId,
            LoteId = fila.LoteId,
            Lote = fila.Lote,
            VentaId = venta?.Id,
            Venta = venta,
            Tipo = tipo,
            Cantidad = cantidad,
            Fecha = reloj.AhoraUtc(),
            Motivo = motivo
        });
}
