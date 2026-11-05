using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Inventario;

public class ServicioInventario(
    IStockRepositorio<StockEstante> estante,
    IStockRepositorio<StockBodega> bodega,
    IMovimientoRepositorio movimientos,
    IRepositorio<Merma> mermas,
    IEstrategiaSalida estrategia,
    TimeProvider reloj,
    IProductoRepositorio? productos = null,
    ILoteRepositorio? lotes = null,
    IReabastecimientoRepositorio? solicitudes = null,
    IServicioParametros? parametros = null,
    IServicioAuditoria? auditoria = null,
    ISesionActual? sesion = null,
    IUnidadDeTrabajo? trabajo = null) : IServicioInventario,
        IServicioConsultaInventario,
        IServicioAlertasInventario,
        IServicioMermas,
        IServicioReabastecimiento
{
    // --- Operaciones núcleo de stock ---

    public async Task DescontarVentaAsync(int productoId, int cantidad, Venta venta)
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
            var destino = await ObtenerOCrearEstanteAsync(salida.ProductoId, salida.LoteId);
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
        var filas = await OrdenarAsyncParaTrasladoAsync(bodega, productoId);

        foreach (var (origen, tomar) in PlanDeSalida.Repartir(filas, cantidad))
        {
            origen.Disminuir(tomar);
            var destino = await ObtenerOCrearEstanteAsync(origen.ProductoId, origen.LoteId);
            destino.Aumentar(tomar);
            await RegistrarAsync(origen, TipoMovimiento.Traslado, tomar);
        }
    }

    public async Task RegistrarIngresoAsync(Lote lote, int cantidad)
    {
        if (lote.ProductoId <= 0)
            throw new ValidacionException("El lote no está asociado a un producto.");

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
        if (fila.ProductoId != productoId)
            throw new ValidacionException("El lote pertenece a otro producto.");
        fila.Disminuir(cantidad);

        await mermas.AgregarAsync(new Merma
        {
            ProductoId = productoId,
            LoteId = loteId,
            Cantidad = cantidad,
            Motivo = motivo,
            Fecha = reloj.AhoraUtc()
        });
        await RegistrarAsync(fila, TipoMovimiento.Merma, cantidad, motivo: motivo);
    }

    // --- Mermas con auditoría y persistencia ---

    public async Task RegistrarAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo)
    {
        sesion?.Exigir(Permiso.AjustarInventario);

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ValidacionException("Falta el motivo de la merma.");

        await RegistrarMermaAsync(productoId, loteId, ubicacion, cantidad, motivo);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(AccionAuditoria.Merma, nameof(Merma), loteId, motivo);

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    // --- Alertas de inventario ---

    public async Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync()
    {
        sesion?.Exigir(Permiso.VerInventario);

        if (productos is null)
            return [];

        var totales = await estante.ObtenerTotalesPorProductoAsync();
        var todos = await productos.ObtenerTodosAsync();

        return todos
            .Where(p => p.Activo)
            .Select(p => new AlertaStockMinimo(p, totales.GetValueOrDefault(p.Id)))
            .Where(a => a.CantidadEnEstante < a.Producto.StockMinimoEstante)
            .ToList();
    }

    public async Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
    {
        sesion?.Exigir(Permiso.VerInventario);

        if (lotes is null || parametros is null)
            return [];

        var dias = await parametros.ObtenerEnteroAsync(ClavesParametro.DiasAlertaVencimiento);
        var limite = reloj.HoyLocal().AddDays(dias);
        return await lotes.ObtenerPorVencerAsync(limite);
    }

    // --- Reabastecimiento ---

    public async Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync()
    {
        sesion?.Exigir(Permiso.Reabastecer);

        if (solicitudes is null)
            return [];

        var pendientes = await solicitudes.ObtenerPendientesAsync();
        var conSolicitud = pendientes.Select(s => s.ProductoId).ToHashSet();
        var nuevas = new List<Reabastecimiento>();

        foreach (var alerta in await ObtenerBajoMinimoAsync())
        {
            if (conSolicitud.Contains(alerta.Producto.Id))
                continue;

            nuevas.Add(new Reabastecimiento
            {
                ProductoId = alerta.Producto.Id,
                CantidadSolicitada = alerta.Producto.StockMinimoEstante - alerta.CantidadEnEstante,
                FechaSolicitud = reloj.AhoraUtc()
            });
        }

        foreach (var solicitud in nuevas)
            await solicitudes.AgregarAsync(solicitud);

        if (trabajo is not null)
            await trabajo.GuardarAsync();

        return nuevas;
    }

    public Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
    {
        sesion?.Exigir(Permiso.Reabastecer);
        return solicitudes is not null
            ? solicitudes.ObtenerPendientesAsync()
            : Task.FromResult<IReadOnlyList<Reabastecimiento>>([]);
    }

    public async Task AtenderAsync(int reabastecimientoId)
    {
        sesion?.Exigir(Permiso.Reabastecer);

        if (solicitudes is null)
            return;

        var solicitud = await solicitudes.ObtenerPorIdAsync(reabastecimientoId)
            ?? throw new ValidacionException("La solicitud no existe.");

        await TrasladarAEstanteAsync(solicitud.ProductoId, solicitud.CantidadSolicitada);
        solicitud.Atender(reloj.AhoraUtc());
        solicitudes.Actualizar(solicitud);

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    // --- Consultas y Kardex ---

    public async Task<IReadOnlyList<StockConsolidado>> ObtenerStockConsolidadoAsync(int? productoId = null)
    {
        sesion?.Exigir(Permiso.VerInventario);

        if (productos is null || lotes is null)
            return [];

        var listaProductos = await productos.ObtenerTodosAsync();
        if (productoId.HasValue)
            listaProductos = listaProductos.Where(p => p.Id == productoId.Value).ToList();

        var todosLotes = await lotes.ObtenerTodosAsync();
        var stockEstante = await estante.ObtenerTodosAsync();
        var stockBodega = await bodega.ObtenerTodosAsync();

        var estantePorLote = stockEstante.ToDictionary(s => s.LoteId, s => s.Cantidad);
        var bodegaPorLote = stockBodega.ToDictionary(s => s.LoteId, s => s.Cantidad);

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
        sesion?.Exigir(Permiso.VerInventario);

        if (lotes is null)
            return [];

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
        var saldo = todosMovimientos
            .Where(m => m.ProductoId == productoId && m.Fecha < desde)
            .Sum(m => m.Tipo switch
            {
                TipoMovimiento.Ingreso or TipoMovimiento.Devolucion => m.Cantidad,
                TipoMovimiento.Venta or TipoMovimiento.Merma => -m.Cantidad,
                _ => 0
            });

        foreach (var m in filtrados)
        {
            saldo += m.Tipo switch
            {
                TipoMovimiento.Ingreso or TipoMovimiento.Devolucion => m.Cantidad,
                TipoMovimiento.Venta or TipoMovimiento.Merma => -m.Cantidad,
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

    // --- Métodos auxiliares privados ---

    private async Task<StockLote> BuscarFilaAsync(int loteId, UbicacionStock ubicacion)
    {
        StockLote? fila = ubicacion == UbicacionStock.Estante
            ? await estante.ObtenerPorLoteAsync(loteId)
            : await bodega.ObtenerPorLoteAsync(loteId);

        return fila ?? throw new StockInsuficienteException($"lote {loteId}");
    }

    private async Task<List<T>> OrdenarAsyncParaVentaAsync<T>(
        IStockRepositorio<T> repositorio,
        int productoId) where T : StockLote
    {
        var filas = await repositorio.ObtenerPorProductoAsync(productoId);
        return estrategia.Ordenar(filas, reloj.HoyLocal()).ToList();
    }

    private async Task<List<T>> OrdenarAsyncParaTrasladoAsync<T>(
        IStockRepositorio<T> repositorio,
        int productoId) where T : StockLote
    {
        var filas = await repositorio.ObtenerPorProductoAsync(productoId);
        return filas
            .Where(s => !s.Lote.EstaVencido(reloj.HoyLocal()))
            .OrderBy(s => s.Lote.FechaVencimiento)
            .ToList();
    }

    private async Task<StockEstante> ObtenerOCrearEstanteAsync(int productoId, int loteId)
    {
        var existente = await estante.ObtenerPorLoteAsync(loteId);
        if (existente is not null)
        {
            if (existente.ProductoId != productoId)
                throw new ValidacionException("El lote pertenece a otro producto.");

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

// Adaptadores para compatibilidad directa con pruebas existentes

public class ServicioAlertasInventario(
    IProductoRepositorio productos,
    IStockRepositorio<StockEstante> estante,
    ILoteRepositorio lotes,
    IServicioParametros parametros,
    ISesionActual sesion,
    TimeProvider reloj) : ServicioInventario(
        estante,
        null!,
        null!,
        null!,
        null!,
        reloj,
        productos: productos,
        lotes: lotes,
        parametros: parametros,
        sesion: sesion);

public class ServicioReabastecimiento(
    IReabastecimientoRepositorio solicitudes,
    IServicioAlertasInventario alertas,
    IServicioInventario inventario,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioReabastecimiento
{
    public async Task<IReadOnlyList<Reabastecimiento>> GenerarSolicitudesAsync()
    {
        sesion.Exigir(Permiso.Reabastecer);

        var pendientes = await solicitudes.ObtenerPendientesAsync();
        var conSolicitud = pendientes.Select(s => s.ProductoId).ToHashSet();
        var nuevas = new List<Reabastecimiento>();

        foreach (var alerta in await alertas.ObtenerBajoMinimoAsync())
        {
            if (conSolicitud.Contains(alerta.Producto.Id))
                continue;

            nuevas.Add(new Reabastecimiento
            {
                ProductoId = alerta.Producto.Id,
                CantidadSolicitada = alerta.Producto.StockMinimoEstante - alerta.CantidadEnEstante,
                FechaSolicitud = reloj.AhoraUtc()
            });
        }

        foreach (var solicitud in nuevas)
            await solicitudes.AgregarAsync(solicitud);

        await trabajo.GuardarAsync();
        return nuevas;
    }

    public Task<IReadOnlyList<Reabastecimiento>> ObtenerPendientesAsync()
    {
        sesion.Exigir(Permiso.Reabastecer);
        return solicitudes.ObtenerPendientesAsync();
    }

    public async Task AtenderAsync(int reabastecimientoId)
    {
        sesion.Exigir(Permiso.Reabastecer);

        var solicitud = await solicitudes.ObtenerPorIdAsync(reabastecimientoId)
            ?? throw new ValidacionException("La solicitud no existe.");

        await inventario.TrasladarAEstanteAsync(solicitud.ProductoId, solicitud.CantidadSolicitada);
        solicitud.Atender(reloj.AhoraUtc());
        solicitudes.Actualizar(solicitud);
        await trabajo.GuardarAsync();
    }
}

public class ServicioConsultaInventario(
    IStockRepositorio<StockEstante> estante,
    IStockRepositorio<StockBodega> bodega,
    IProductoRepositorio productos,
    ILoteRepositorio lotes,
    IMovimientoRepositorio movimientos,
    ISesionActual sesion) : ServicioInventario(
        estante,
        bodega,
        movimientos,
        null!,
        null!,
        TimeProvider.System,
        productos: productos,
        lotes: lotes,
        sesion: sesion);

public class ServicioMermas(
    IServicioInventario inventario,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioMermas
{
    public Task RegistrarAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo)
    {
        sesion.Exigir(Permiso.AjustarInventario);
        return inventario.RegistrarMermaAsync(productoId, loteId, ubicacion, cantidad, motivo)
            .ContinueWith(async _ =>
            {
                await auditoria.RegistrarAsync(AccionAuditoria.Merma, nameof(Merma), loteId, motivo);
                await trabajo.GuardarAsync();
            }).Unwrap();
    }
}
