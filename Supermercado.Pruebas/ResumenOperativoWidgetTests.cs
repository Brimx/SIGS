using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Dashboards.Widgets;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Ventas;

namespace Supermercado.Pruebas;

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

        public override Task<TurnoCaja?> ObtenerTurnoActualAsync() => Task.FromResult<TurnoCaja?>(null);
        public override Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial) => throw new NotSupportedException();
        public override Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado) => throw new NotSupportedException();
        public override Task<RetiroCaja> RegistrarRetiroAsync(long monto, string motivo, int supervisorId) => throw new NotSupportedException();
        public override Task<IReadOnlyList<RetiroCaja>> ObtenerRetirosTurnoActualAsync() => throw new NotSupportedException();
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
