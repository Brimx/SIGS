using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

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
