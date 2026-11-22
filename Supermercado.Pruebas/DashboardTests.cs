using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Dashboards.Widgets;
using Supermercado.Logica.Inventario;
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Contratos;
using Supermercado.Logica.Ventas;

namespace Supermercado.Pruebas;

public class WidgetsTests
{
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    [Fact]
    public async Task IvaGeneradoWidget_CalculaTotalIvaHoy()
    {
        var ventas = new VentasEnMemoria();
        var hoy = reloj.GetUtcNow().UtcDateTime;

        var v1 = new Venta { Fecha = hoy };
        v1.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 1,
            PrecioUnitario = 11900,
            Subtotal = 11900,
            Iva = 1900,
            PuntosBasicosIva = 1900
        });
        v1.AgregarPago(new PagoEfectivo { Monto = 11900 });
        v1.Cerrar();
        await ventas.AgregarAsync(v1);

        var widget = new IvaGeneradoWidget(ventas, reloj);
        var datos = await widget.ObtenerDatosAsync();

        var punto = Assert.Single(datos.Puntos);
        Assert.Equal(1900, punto.Valor);
    }

    [Fact]
    public async Task LotesPorVencerWidget_DevuelveConteo()
    {
        var alertas =
            new ServicioReabastecimientoTests.AlertasInventarioFalsas();
        alertas.LotesPorVencer.Add(new Lote { Id = 1, NumeroLote = "L1" });
        alertas.LotesPorVencer.Add(new Lote { Id = 2, NumeroLote = "L2" });

        var widget = new LotesPorVencerWidget(alertas);
        var datos = await widget.ObtenerDatosAsync();

        var punto = Assert.Single(datos.Puntos);
        Assert.Equal(2, punto.Valor);
    }

    [Fact]
    public async Task StockBajoMinimoWidget_DevuelveConteo()
    {
        var alertas =
            new ServicioReabastecimientoTests.AlertasInventarioFalsas();
        var prod = new ProductoUnitario { Id = 1, Nombre = "Arroz" };
        alertas.AlertasBajo.Add(new AlertaStockMinimo(prod, 3));

        var widget = new StockBajoMinimoWidget(alertas);
        var datos = await widget.ObtenerDatosAsync();

        var punto = Assert.Single(datos.Puntos);
        Assert.Equal(1, punto.Valor);
    }

    [Fact]
    public async Task VentasPorCategoriaWidget_AgrupaPorCategoria()
    {
        using var contexto = new ContextoPruebasSqlite(reloj);
        var ventas = new VentasEnMemoria();
        var productos = new ProductosEnMemoria(contexto);
        var categorias = new CategoriasEnMemoria(contexto);

        var catLacteos = new Categoria { Id = 1, Nombre = "Lácteos" };
        var catGranos = new Categoria { Id = 2, Nombre = "Granos" };
        await categorias.AgregarAsync(catLacteos);
        await categorias.AgregarAsync(catGranos);

        var pLeche = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Leche",
            CategoriaId = 1
        };
        var pArroz = new ProductoUnitario
        {
            Id = 2,
            Nombre = "Arroz",
            CategoriaId = 2
        };
        await productos.AgregarAsync(pLeche);
        await productos.AgregarAsync(pArroz);

        var hoy = reloj.GetUtcNow().UtcDateTime;
        var v = new Venta { Fecha = hoy };
        v.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 2,
            Subtotal = 8000
        });
        v.Detalles.Add(new DetalleVenta
        {
            ProductoId = 2,
            Cantidad = 1,
            Subtotal = 4500
        });
        v.AgregarPago(new PagoEfectivo { Monto = 12500 });
        v.Cerrar();
        await ventas.AgregarAsync(v);

        var widget = new VentasPorCategoriaWidget(
            ventas, productos, categorias, reloj);
        var datos = await widget.ObtenerDatosAsync();

        Assert.Equal(2, datos.Puntos.Count);
        var puntoLacteos = datos.Puntos.First(p => p.Etiqueta == "Lácteos");
        var puntoGranos = datos.Puntos.First(p => p.Etiqueta == "Granos");
        Assert.Equal(8000, puntoLacteos.Valor);
        Assert.Equal(4500, puntoGranos.Valor);
    }

    [Fact]
    public async Task MargenPorCategoriaWidget_CalculaMargenCorrecto()
    {
        using var contexto = new ContextoPruebasSqlite(reloj);
        var ventas = new VentasEnMemoria();
        var movs = new MovimientosEnMemoria(contexto);
        var lotes = new LotesEnMemoria(contexto);
        var productos = new ProductosEnMemoria(contexto);
        var categorias = new CategoriasEnMemoria(contexto);

        var cat = new Categoria { Id = 1, Nombre = "Bebidas" };
        await categorias.AgregarAsync(cat);

        var prod = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Jugo",
            CategoriaId = 1
        };
        await productos.AgregarAsync(prod);

        var lote = new Lote
        {
            Id = 1,
            ProductoId = 1,
            CostoUnitario = 2000
        };
        await lotes.AgregarAsync(lote);

        var hoy = reloj.GetUtcNow().UtcDateTime;
        var v = new Venta { Id = 1, Fecha = hoy };
        v.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 2,
            Subtotal = 6000,
            Iva = 0
        });
        v.AgregarPago(new PagoEfectivo { Monto = 6000 });
        v.Cerrar();
        await ventas.AgregarAsync(v);

        await movs.AgregarAsync(new MovimientoInventario
        {
            ProductoId = 1,
            LoteId = 1,
            VentaId = 1,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 2,
            Fecha = hoy
        });

        var widget = new MargenPorCategoriaWidget(
            ventas, movs, lotes, productos, categorias, reloj);
        var datos = await widget.ObtenerDatosAsync();

        var punto = Assert.Single(datos.Puntos);
        Assert.Equal("Bebidas", punto.Etiqueta);
        // Ingreso: 6000, Costo: 2 * 2000 = 4000. Margen = 2000.
        Assert.Equal(2000, punto.Valor);
    }

    [Fact]
    public async Task MargenPorCategoriaWidget_PesadosConvierteYNoDuplicaIngresoPorLote()
    {
        using var contexto = new ContextoPruebasSqlite(reloj);
        var ventas = new VentasEnMemoria();
        var movs = new MovimientosEnMemoria(contexto);
        var lotes = new LotesEnMemoria(contexto);
        var productos = new ProductosEnMemoria(contexto);
        var categorias = new CategoriasEnMemoria(contexto);
        await categorias.AgregarAsync(new Categoria { Id = 1, Nombre = "Fruver" });

        await productos.AgregarAsync(new ProductoPesado
        {
            Id = 1,
            Nombre = "Manzana",
            CategoriaId = 1
        });
        await lotes.AgregarAsync(new Lote { Id = 1, ProductoId = 1, CostoUnitario = 4000 });
        await lotes.AgregarAsync(new Lote { Id = 2, ProductoId = 1, CostoUnitario = 6000 });

        var hoy = reloj.GetUtcNow().UtcDateTime;
        var venta = new Venta { Id = 1, Fecha = hoy };
        venta.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 1500,
            Subtotal = 15000,
            Iva = 0
        });
        venta.AgregarPago(new PagoEfectivo { Monto = 15000 });
        venta.Cerrar();
        await ventas.AgregarAsync(venta);

        await movs.AgregarAsync(new MovimientoInventario
        {
            ProductoId = 1, LoteId = 1, VentaId = 1,
            Tipo = TipoMovimiento.Venta, Cantidad = 1000, Fecha = hoy
        });
        await movs.AgregarAsync(new MovimientoInventario
        {
            ProductoId = 1, LoteId = 2, VentaId = 1,
            Tipo = TipoMovimiento.Venta, Cantidad = 500, Fecha = hoy
        });

        var widget = new MargenPorCategoriaWidget(
            ventas, movs, lotes, productos, categorias, reloj);
        var punto = Assert.Single((await widget.ObtenerDatosAsync()).Puntos);

        Assert.Equal(8_000, punto.Valor);
    }

    [Fact]
    public async Task ArqueoTurnoWidget_ConTurnoAbierto_CalculaEsperado()
    {
        using var contexto = new ContextoPruebasSqlite(reloj);
        var turnos = new TurnosEnMemoria(contexto);
        var ventas = new VentasEnMemoria();
        var sesion = Sesiones.Con(Permiso.AbrirCerrarTurno);

        var turno = TurnoCaja.Abrir(
            cajaId: 1,
            empleadoId: 1,
            efectivoInicial: 50000,
            ahora: reloj.GetUtcNow().UtcDateTime);
        await turnos.AgregarAsync(turno);

        var v = new Venta { TurnoCajaId = turno.Id };
        v.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 1,
            Subtotal = 20000
        });
        v.AgregarPago(new PagoEfectivo { Monto = 20000 });
        v.Cerrar();
        await ventas.AgregarAsync(v);

        var widget = new ArqueoTurnoWidget(turnos, ventas, sesion);
        var datos = await widget.ObtenerDatosAsync();

        Assert.Equal(2, datos.Puntos.Count);
        var pEsperado = datos.Puntos
            .First(p => p.Etiqueta == "Efectivo esperado");
        Assert.Equal(70000, pEsperado.Valor);
    }
}

public sealed class ResumenOperativoWidgetTests
{
    [Fact]
    public async Task WidgetCombinaIndicadoresYRequiereTodosLosPermisos()
    {
        var widget = new ResumenOperativoWidget(
            new VentasVacias(),
            new CajaVacia(),
            new AlertasVacias(),
            TimeProvider.System);

        var datos = await widget.ObtenerDatosAsync();

        Assert.Equal(
            Permiso.VerReportes | Permiso.VerFinanzas | Permiso.VerInventario,
            widget.PermisoRequerido);
        Assert.Collection(datos.Puntos,
            ventas => Assert.Equal("Ventas de hoy", ventas.Etiqueta),
            tickets => Assert.Equal("Tickets", tickets.Etiqueta),
            turno => Assert.Equal(0, turno.Valor),
            stock => Assert.Equal(0, stock.Valor));
    }

    private sealed class VentasVacias : IVentaRepositorio
    {
        public Task<Venta?> ObtenerPorIdAsync(int id) => Task.FromResult<Venta?>(null);
        public Task<IReadOnlyList<Venta>> ObtenerTodosAsync() => Task.FromResult<IReadOnlyList<Venta>>([]);
        public Task AgregarAsync(Venta entidad) => Task.CompletedTask;
        public void Actualizar(Venta entidad) { }
        public Task<IReadOnlyList<Venta>> ObtenerPorTurnoAsync(int turnoId) => Task.FromResult<IReadOnlyList<Venta>>([]);
        public Task<IReadOnlyList<Venta>> ObtenerPorRangoAsync(DateTime desde, DateTime hasta) => Task.FromResult<IReadOnlyList<Venta>>([]);
        public Task<Venta?> ObtenerPorClaveIdempotenciaAsync(string claveIdempotencia) => Task.FromResult<Venta?>(null);
        public Task<int> GuardarAsync() => Task.FromResult(0);
    }

    private sealed class CajaVacia : ServicioCaja
    {
        public CajaVacia() : base(null!, null!, null!, null!, null!, null!, null!)
        {
        }

        public override Task<TurnoCaja?> ObtenerTurnoActualAsync(int? cajaId = null) => Task.FromResult<TurnoCaja?>(null);
        public override Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial) => throw new NotSupportedException();
        public override Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado, int? cajaId = null) => throw new NotSupportedException();
        public override Task<RetiroCaja> RegistrarRetiroAsync(long monto, string motivo, int supervisorId, int? cajaId = null) => throw new NotSupportedException();
        public override Task<IReadOnlyList<RetiroCaja>> ObtenerRetirosTurnoActualAsync(int? cajaId = null) => throw new NotSupportedException();
    }

    private sealed class AlertasVacias : ServicioInventario
    {
        public AlertasVacias() : base(null!, null!, null!, null!, null!, null!)
        {
        }

        public override Task<IReadOnlyList<AlertaStockMinimo>> ObtenerBajoMinimoAsync()
            => Task.FromResult<IReadOnlyList<AlertaStockMinimo>>([]);
        public override Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
            => Task.FromResult<IReadOnlyList<Lote>>([]);
    }
}
