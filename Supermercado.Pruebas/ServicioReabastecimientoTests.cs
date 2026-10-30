using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioReabastecimientoTests
{
    private class Escenario
    {
        public ReabastecimientosEnMemoria Solicitudes { get; } = new();
        public AlertasInventarioFalsas Alertas { get; } = new();
        public InventarioFalso Inventario { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public ServicioReabastecimiento CrearServicio(
            Permiso permisos = Permiso.Reabastecer)
            => new(
                Solicitudes,
                Alertas,
                Inventario,
                Sesiones.Con(permisos),
                Trabajo,
                Reloj);
    }

    public class AlertasInventarioFalsas : IServicioAlertasInventario
    {
        public List<AlertaStockMinimo> AlertasBajo { get; } = [];
        public List<Lote> LotesPorVencer { get; } = [];

        public Task<IReadOnlyList<AlertaStockMinimo>>
            ObtenerBajoMinimoAsync()
            => Task.FromResult<IReadOnlyList<AlertaStockMinimo>>(AlertasBajo);

        public Task<IReadOnlyList<Lote>> ObtenerLotesPorVencerAsync()
            => Task.FromResult<IReadOnlyList<Lote>>(LotesPorVencer);
    }

    public class InventarioFalso : IServicioInventario
    {
        public int TrasladosLlamados { get; private set; }

        public Task DescontarVentaAsync(
            int productoId, int cantidad, Entidades.Ventas.Venta venta)
            => Task.CompletedTask;

        public Task ReintegrarVentaAsync(int ventaId)
            => Task.CompletedTask;

        public Task TrasladarAEstanteAsync(int productoId, int cantidad)
        {
            TrasladosLlamados++;
            return Task.CompletedTask;
        }

        public Task RegistrarIngresoAsync(Lote lote, int cantidad)
            => Task.CompletedTask;

        public Task RegistrarMermaAsync(
            int productoId,
            int loteId,
            UbicacionStock ubicacion,
            int cantidad,
            string motivo)
            => Task.CompletedTask;
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
        Assert.Equal(1, e.Trabajo.Guardados);
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
        Assert.Equal(1, e.Trabajo.Guardados);
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
