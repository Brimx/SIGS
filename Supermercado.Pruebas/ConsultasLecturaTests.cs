using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ConsultasLecturaTests
{
    [Fact]
    public async Task ConsultaAuditoria_FiltraYPagina()
    {
        var registros = new RepositorioEnMemoria<RegistroAuditoria>();
        var ahora = new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 1,
            Fecha = ahora.AddHours(-3),
            Accion = AccionAuditoria.InicioSesion,
            EmpleadoId = 1
        });
        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 2,
            Fecha = ahora.AddHours(-2),
            Accion = AccionAuditoria.VentaAnulada,
            EmpleadoId = 2
        });
        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 3,
            Fecha = ahora.AddHours(-1),
            Accion = AccionAuditoria.VentaAnulada,
            EmpleadoId = 2
        });

        var servicio = new ServicioConsultaAuditoria(
            registros,
            Sesiones.Con(Permiso.VerAuditoria));

        var filtro = new FiltroAuditoria(
            Accion: AccionAuditoria.VentaAnulada,
            EmpleadoId: 2,
            Pagina: 1,
            TamanoPagina: 10);

        var resultado = await servicio.ConsultarAsync(filtro);

        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(3, resultado.Items[0].Id); // Orden descendente
    }

    [Fact]
    public async Task ConsultaAuditoria_SinPermiso_LanzaAccesoDenegado()
    {
        var registros = new RepositorioEnMemoria<RegistroAuditoria>();
        var servicio = new ServicioConsultaAuditoria(
            registros,
            Sesiones.Con(Permiso.Vender));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.ConsultarAsync(new FiltroAuditoria()));
    }

    [Fact]
    public async Task ConsultaInventario_Consolidado_CalculaEstanteYBodega()
    {
        var productos = new ProductosEnMemoria();
        var lotes = new LotesEnMemoria();
        var estante = new StockEnMemoria<StockEstante>();
        var bodega = new StockEnMemoria<StockBodega>();
        var movs = new MovimientosEnMemoria();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var prod = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Harina",
            CodigoBarras = "999",
            Impuesto = impuesto
        };
        await productos.AgregarAsync(prod);

        var lote = new Lote
        {
            Id = 10,
            ProductoId = 1,
            NumeroLote = "LOT-H",
            FechaVencimiento = new DateOnly(2027, 1, 1)
        };
        await lotes.AgregarAsync(lote);

        var se = new StockEstante { ProductoId = 1, LoteId = 10, Lote = lote };
        se.Aumentar(7);
        await estante.AgregarAsync(se);

        var sb = new StockBodega { ProductoId = 1, LoteId = 10, Lote = lote };
        sb.Aumentar(13);
        await bodega.AgregarAsync(sb);

        var servicio = new ServicioConsultaInventario(
            estante,
            bodega,
            productos,
            lotes,
            movs,
            Sesiones.Con(Permiso.VerInventario));

        var consolidado = await servicio.ObtenerStockConsolidadoAsync(1);

        var item = Assert.Single(consolidado);
        Assert.Equal("Harina", item.NombreProducto);
        Assert.Equal(7, item.CantidadEstante);
        Assert.Equal(13, item.CantidadBodega);
        Assert.Equal(20, item.TotalStock);
    }

    [Fact]
    public async Task ConsultaInventario_Kardex_CalculaSaldosCronologicos()
    {
        var productos = new ProductosEnMemoria();
        var lotes = new LotesEnMemoria();
        var estante = new StockEnMemoria<StockEstante>();
        var bodega = new StockEnMemoria<StockBodega>();
        var movs = new MovimientosEnMemoria();

        var lote = new Lote { Id = 1, NumeroLote = "L1", ProductoId = 1 };
        await lotes.AgregarAsync(lote);

        var fechaBase = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 1,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 50,
            Fecha = fechaBase
        });
        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 2,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 10,
            Fecha = fechaBase.AddHours(2)
        });
        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 3,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Devolucion,
            Cantidad = 2,
            Fecha = fechaBase.AddHours(4)
        });

        var servicio = new ServicioConsultaInventario(
            estante,
            bodega,
            productos,
            lotes,
            movs,
            Sesiones.Con(Permiso.VerInventario));

        var kardex = await servicio.ObtenerKardexAsync(
            1,
            fechaBase.AddDays(-1),
            fechaBase.AddDays(1));

        Assert.Equal(3, kardex.Count);
        Assert.Equal(50, kardex[0].SaldoPosterior);
        Assert.Equal(40, kardex[1].SaldoPosterior);
        Assert.Equal(42, kardex[2].SaldoPosterior);
    }

    [Fact]
    public async Task BusquedaProductos_FiltraPorNombreYActivo()
    {
        var productos = new ProductosEnMemoria();
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz Diana",
            Activo = true
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 2,
            Nombre = "Arroz Roa",
            Activo = true
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 3,
            Nombre = "Arroz Florhuila",
            Activo = false
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 4,
            Nombre = "Aceite Diana",
            Activo = true
        });

        var servicio = new ServicioBusquedaProductos(productos);

        var resultados = await servicio.BuscarPorNombreAsync("arroz", 1);
        Assert.Single(resultados);

        var todosArroces = await servicio.BuscarPorNombreAsync("arroz", 10);
        Assert.Equal(2, todosArroces.Count);
        Assert.DoesNotContain(todosArroces, p => !p.Activo);

        var vacio = await servicio.BuscarPorNombreAsync("   ");
        Assert.Empty(vacio);
    }
}
