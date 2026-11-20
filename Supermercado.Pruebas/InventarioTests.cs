using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;
using Xunit;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Seguridad;
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Ventas;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Finanzas;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Pruebas;

public class InventarioTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 17);

    private static StockBodega Bodega(int loteId, int dias, int cantidad)
    {
        var stock = new StockBodega
        {
            LoteId = loteId,
            Lote = new Lote
            {
                Id = loteId,
                FechaVencimiento = Hoy.AddDays(dias)
            }
        };
        stock.Aumentar(cantidad);
        return stock;
    }

    [Fact]
    public void Fefo_SacaPrimeroElQueVenceAntesYSaltaLosVencidos()
    {
        var filas = new[]
        {
            Bodega(1, 20, 5),
            Bodega(2, 5, 5),
            Bodega(3, -1, 5),
        };

        var orden = new SalidaFefo().Ordenar(filas, Hoy);

        Assert.Equal(new[] { 2, 1 }, orden.Select(s => s.LoteId));
    }

    [Fact]
    public void Plan_RepartePrimeroDelLoteQueVenceAntes()
    {
        var filas = new List<StockBodega>
        {
            Bodega(1, 5, 3),
            Bodega(2, 20, 10),
        };

        var plan = PlanDeSalida.Repartir(filas, 5);

        Assert.Equal(new[] { 3, 2 }, plan.Select(p => p.Tomar));
    }

    [Fact]
    public void Plan_FallaSiNoAlcanzaElStock()
    {
        var filas = new List<StockBodega> { Bodega(1, 5, 3) };

        Assert.Throws<StockInsuficienteException>(
            () => PlanDeSalida.Repartir(filas, 4));
    }

    [Fact]
    public void Stock_NoQuedaEnNegativo()
    {
        var stock = new StockEstante { LoteId = 1 };
        stock.Aumentar(2);

        Assert.Throws<StockInsuficienteException>(
            () => stock.Disminuir(3));
    }

    [Fact]
    public void Reabastecimiento_SoloSeAtiendeUnaVez()
    {
        var solicitud = new Reabastecimiento { CantidadSolicitada = 10 };
        var ahora = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

        solicitud.Atender(ahora);

        Assert.Equal(EstadoReabastecimiento.Atendido, solicitud.Estado);
        Assert.Equal(ahora, solicitud.FechaAtencion);
        Assert.Throws<ValidacionException>(
            () => solicitud.Atender(ahora));
    }

    [Fact]
    public void Reabastecimiento_Cancelado_NoSePuedeAtender()
    {
        var solicitud = new Reabastecimiento { CantidadSolicitada = 10 };

        solicitud.Cancelar();

        Assert.Equal(EstadoReabastecimiento.Cancelado, solicitud.Estado);
        Assert.Throws<ValidacionException>(
            () => solicitud.Atender(DateTime.UtcNow));
    }
}

public class ServicioAlertasInventarioTests
{
    private readonly StockEstanteEnMemoria estante = new();
    private readonly ProductosEnMemoria productos = new();
    private readonly LotesEnMemoria lotes = new();
    private readonly ParametrosFalsos parametros = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioInventario CrearServicio(
        Permiso permisos = Permiso.VerInventario)
        => new(
            estante,
            null!,
            null!,
            null!,
            null!,
            reloj,
            productos: productos,
            lotes: lotes,
            parametros: parametros,
            sesion: Sesiones.Con(permisos));

    [Fact]
    public async Task ObtenerBajoMinimo_IdentificaProductosBajoElMinimo()
    {
        var servicio = CrearServicio();

        var p1 = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz",
            StockMinimoEstante = 10,
            Activo = true
        };
        var p2 = new ProductoUnitario
        {
            Id = 2,
            Nombre = "Aceite",
            StockMinimoEstante = 5,
            Activo = true
        };
        var pInactivo = new ProductoUnitario
        {
            Id = 3,
            Nombre = "Sal",
            StockMinimoEstante = 10,
            Activo = false
        };
        await productos.AgregarAsync(p1);
        await productos.AgregarAsync(p2);
        await productos.AgregarAsync(pInactivo);

        var s1 = new StockEstante { ProductoId = 1, LoteId = 100 };
        s1.Aumentar(4); // 4 < 10 -> bajo mínimo
        await estante.AgregarAsync(s1);

        var s2 = new StockEstante { ProductoId = 2, LoteId = 200 };
        s2.Aumentar(8); // 8 >= 5 -> ok
        await estante.AgregarAsync(s2);

        var alertas = await servicio.ObtenerBajoMinimoAsync();

        var alerta = Assert.Single(alertas);
        Assert.Equal(1, alerta.Producto.Id);
        Assert.Equal(4, alerta.CantidadEnEstante);
    }

    [Fact]
    public async Task ObtenerBajoMinimo_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.ObtenerBajoMinimoAsync());
    }

    [Fact]
    public async Task ObtenerLotesPorVencer_UsaParametroDias()
    {
        parametros.Valores[ClavesParametro.DiasAlertaVencimiento] = "15";
        var servicio = CrearServicio();

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

        var loteVenceEn10Dias = new Lote
        {
            Id = 1,
            NumeroLote = "L10",
            FechaVencimiento = hoy.AddDays(10)
        };
        var loteVenceEn20Dias = new Lote
        {
            Id = 2,
            NumeroLote = "L20",
            FechaVencimiento = hoy.AddDays(20)
        };
        await lotes.AgregarAsync(loteVenceEn10Dias);
        await lotes.AgregarAsync(loteVenceEn20Dias);

        var porVencer = await servicio.ObtenerLotesPorVencerAsync();

        var lote = Assert.Single(porVencer);
        Assert.Equal("L10", lote.NumeroLote);
    }
}

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
    public async Task Traslado_NoMueveLoteVencido()
    {
        var e = new EscenarioInventario();
        var bodega = e.AgregarBodega(1, 10, -1, 5);

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Servicio.TrasladarAEstanteAsync(1, 3));

        Assert.Equal(5, bodega.Cantidad);
        Assert.Empty(e.Estante.Datos);
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

    [Fact]
    public async Task Merma_LoteDeOtroProducto_NoDescuentaStock()
    {
        var e = new EscenarioInventario();
        var fila = e.AgregarEstante(1, 10, 20, 5);

        await Assert.ThrowsAsync<ValidacionException>(
            () => e.Servicio.RegistrarMermaAsync(
                2, 10, UbicacionStock.Estante, 1, "producto equivocado"));

        Assert.Equal(5, fila.Cantidad);
        Assert.Empty(e.Mermas.Datos);
    }
}

public class ServicioReabastecimientoTests
{
    public class EstadoReabastecimientoFalso
    {
        public List<AlertaStockMinimo> AlertasBajo { get; } = [];
        public List<Lote> LotesPorVencer { get; } = [];
        public int TrasladosLlamados { get; set; }
    }

    private class Escenario
    {
        public ReabastecimientosEnMemoria Solicitudes { get; } = new();
        private readonly EstadoReabastecimientoFalso estado = new();
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public AlertasInventarioFalsas Inventario => CrearServicio();
        public AlertasInventarioFalsas Alertas => CrearServicio();

        public AlertasInventarioFalsas CrearServicio(
            Permiso permisos = Permiso.Reabastecer)
            => new(estado, Solicitudes, Sesiones.Con(permisos), Reloj);
    }

    public class AlertasInventarioFalsas : ServicioInventario
    {
        public AlertasInventarioFalsas()
            : this(new EstadoReabastecimientoFalso(), null!, null!, TimeProvider.System)
        {
        }

        public AlertasInventarioFalsas(
            EstadoReabastecimientoFalso estado,
            IReabastecimientoRepositorio solicitudes,
            SesionActual sesion,
            TimeProvider reloj)
            : base(
                null!,
                null!,
                null!,
                null!,
                null!,
                reloj,
                solicitudes: solicitudes,
                sesion: sesion)
        {
            Estado = estado;
        }

        private EstadoReabastecimientoFalso Estado { get; }

        public List<AlertaStockMinimo> AlertasBajo => Estado.AlertasBajo;
        public List<Lote> LotesPorVencer => Estado.LotesPorVencer;
        public int TrasladosLlamados => Estado.TrasladosLlamados;

        public override Task<IReadOnlyList<AlertaStockMinimo>>
            ObtenerBajoMinimoAsync()
            => Task.FromResult<IReadOnlyList<AlertaStockMinimo>>(AlertasBajo);

        public override Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
            => Task.FromResult<IReadOnlyList<Lote>>(LotesPorVencer);

        public override Task TrasladarAEstanteAsync(int productoId, int cantidad)
        {
            Estado.TrasladosLlamados++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task GenerarSolicitudes_CalculaFaltanteYEvitaDuplicados()
    {
        var e = new Escenario();
        var prod1 = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz",
            StockMinimoEstante = 20
        };
        var prod2 = new ProductoUnitario
        {
            Id = 2,
            Nombre = "Azúcar",
            StockMinimoEstante = 15
        };

        e.Alertas.AlertasBajo.Add(new AlertaStockMinimo(prod1, 5));
        e.Alertas.AlertasBajo.Add(new AlertaStockMinimo(prod2, 10));

        // Ya existe solicitud pendiente para prod1
        await e.Solicitudes.AgregarAsync(new Reabastecimiento
        {
            Id = 10,
            ProductoId = 1,
            CantidadSolicitada = 15,
            Estado = EstadoReabastecimiento.Pendiente
        });

        var servicio = e.CrearServicio();
        var nuevas = await servicio.GenerarSolicitudesAsync();

        // Solo se debe generar para prod2 (15 - 10 = 5)
        var generada = Assert.Single(nuevas);
        Assert.Equal(2, generada.ProductoId);
        Assert.Equal(5, generada.CantidadSolicitada);
        Assert.Equal(1, e.Solicitudes.Guardados);
    }

    [Fact]
    public async Task GenerarSolicitudes_SinPermiso_LanzaAccesoDenegado()
    {
        var e = new Escenario();
        var servicio = e.CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.GenerarSolicitudesAsync());
    }

    [Fact]
    public async Task AtenderAsync_TrasladaAEstanteYMarcaAtendido()
    {
        var e = new Escenario();
        var reab = new Reabastecimiento
        {
            Id = 1,
            ProductoId = 5,
            CantidadSolicitada = 12,
            Estado = EstadoReabastecimiento.Pendiente
        };
        await e.Solicitudes.AgregarAsync(reab);

        var servicio = e.CrearServicio();
        await servicio.AtenderAsync(1);

        Assert.Equal(EstadoReabastecimiento.Atendido, reab.Estado);
        Assert.NotNull(reab.FechaAtencion);
        Assert.Equal(1, e.Inventario.TrasladosLlamados);
        Assert.Equal(1, e.Solicitudes.Guardados);
    }

    [Fact]
    public async Task AtenderAsync_Inexistente_LanzaValidacionException()
    {
        var e = new Escenario();
        var servicio = e.CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.AtenderAsync(999));
    }
}
