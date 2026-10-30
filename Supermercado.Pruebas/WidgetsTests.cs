using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Dashboards.Widgets;
using Supermercado.Logica.Inventario;
using Supermercado.Pruebas.Falsos;

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
        var ventas = new VentasEnMemoria();
        var productos = new ProductosEnMemoria();
        var categorias = new RepositorioEnMemoria<Categoria>();

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
        var ventas = new VentasEnMemoria();
        var movs = new MovimientosEnMemoria();
        var lotes = new LotesEnMemoria();
        var productos = new ProductosEnMemoria();
        var categorias = new RepositorioEnMemoria<Categoria>();

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
    public async Task ArqueoTurnoWidget_ConTurnoAbierto_CalculaEsperado()
    {
        var turnos = new TurnosEnMemoria();
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
