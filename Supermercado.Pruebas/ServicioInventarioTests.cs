using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Ventas;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas;

public class ServicioInventarioTests
{
    [Fact]
    public async Task Venta_DescuentaPrimeroDelLoteQueVenceAntes()
    {
        var e = new EscenarioInventario();
        var largo = e.AgregarEstante(1, 10, 20, 5);
        var corto = e.AgregarEstante(1, 11, 5, 3);

        await e.Servicio.DescontarVentaAsync(1, 4, new Venta());

        Assert.Equal(0, corto.Cantidad);
        Assert.Equal(4, largo.Cantidad);
        Assert.Equal(2, e.Movimientos.Datos.Count);
        Assert.True(e.Movimientos.Datos
            .All(m => m.Tipo == TipoMovimiento.Venta));
    }

    [Fact]
    public async Task Venta_SinStockSuficienteNoTocaNada()
    {
        var e = new EscenarioInventario();
        var fila = e.AgregarEstante(1, 10, 20, 3);

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Servicio.DescontarVentaAsync(1, 4, new Venta()));

        Assert.Equal(3, fila.Cantidad);
        Assert.Empty(e.Movimientos.Datos);
    }

    [Fact]
    public async Task Venta_IgnoraLosLotesVencidos()
    {
        var e = new EscenarioInventario();
        e.AgregarEstante(1, 10, -1, 50);

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Servicio.DescontarVentaAsync(1, 1, new Venta()));
    }

    [Fact]
    public async Task Traslado_MueveDeBodegaAEstanteYLoRegistra()
    {
        var e = new EscenarioInventario();
        var bodega = e.AgregarBodega(1, 10, 20, 10);

        await e.Servicio.TrasladarAEstanteAsync(1, 4);

        Assert.Equal(6, bodega.Cantidad);
        Assert.Equal(4, e.Estante.Datos.Single().Cantidad);
        var movimiento = e.Movimientos.Datos.Single();
        Assert.Equal(TipoMovimiento.Traslado, movimiento.Tipo);
    }

    [Fact]
    public async Task Anulacion_DevuelveElStockALosMismosLotes()
    {
        var e = new EscenarioInventario();
        var corto = e.AgregarEstante(1, 11, 5, 3);
        var largo = e.AgregarEstante(1, 10, 20, 5);
        var venta = new Venta { Id = 7 };
        await e.Servicio.DescontarVentaAsync(1, 4, venta);

        await e.Servicio.ReintegrarVentaAsync(7);

        Assert.Equal(3, corto.Cantidad);
        Assert.Equal(5, largo.Cantidad);
        Assert.Equal(2, e.Movimientos.Datos
            .Count(m => m.Tipo == TipoMovimiento.Devolucion));
    }

    [Fact]
    public async Task Merma_DescuentaYQuedaRegistrada()
    {
        var e = new EscenarioInventario();
        var fila = e.AgregarBodega(1, 10, 20, 10);

        await e.Servicio.RegistrarMermaAsync(
            1, 10, UbicacionStock.Bodega, 2, "empaque roto");

        Assert.Equal(8, fila.Cantidad);
        Assert.Equal(2, e.Mermas.Datos.Single().Cantidad);
        Assert.Equal(TipoMovimiento.Merma, e.Movimientos.Datos.Single().Tipo);
    }

    [Fact]
    public async Task Traslado_MueveAunqueElLoteEsteVencido()
    {
        var e = new EscenarioInventario();
        var bodega = e.AgregarBodega(1, 10, -1, 5);

        await e.Servicio.TrasladarAEstanteAsync(1, 3);

        Assert.Equal(2, bodega.Cantidad);
        Assert.Equal(3, e.Estante.Datos.Single().Cantidad);
    }

    [Fact]
    public async Task Reintegrar_LoteDeOtroProducto_NoMezclaStock()
    {
        var e = new EscenarioInventario();
        e.AgregarEstante(1, 10, 20, 5);
        e.Movimientos.Datos.Add(new MovimientoInventario
        {
            ProductoId = 2,
            LoteId = 10,
            VentaId = 99,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 1
        });

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.Servicio.ReintegrarVentaAsync(99));

        Assert.Single(e.Estante.Datos);
        Assert.Equal(5, e.Estante.Datos.Single().Cantidad);
    }

    [Fact]
    public async Task Ingreso_ExigeUnLoteConProducto()
    {
        var e = new EscenarioInventario();
        var lote = new Supermercado.Entidades.Productos.Lote
        {
            Id = 10,
            ProductoId = 0
        };

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.Servicio.RegistrarIngresoAsync(lote, 5));
    }
}
