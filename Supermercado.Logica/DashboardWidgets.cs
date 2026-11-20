using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Dashboards;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;

namespace Supermercado.Logica.Dashboards.Widgets;

public abstract class WidgetBase : IWidget
{
    protected WidgetBase(
        string id,
        string titulo,
        string grupo,
        TamanoWidget tamano,
        TimeSpan frecuenciaActualizacion,
        Permiso permisoRequerido)
    {
        Id = id;
        Titulo = titulo;
        Grupo = grupo;
        Tamano = tamano;
        FrecuenciaActualizacion = frecuenciaActualizacion;
        PermisoRequerido = permisoRequerido;
    }

    public string Id { get; }
    public string Titulo { get; }
    public string Grupo { get; }
    public TamanoWidget Tamano { get; }
    public TimeSpan FrecuenciaActualizacion { get; }
    public Permiso PermisoRequerido { get; }

    public abstract Task<DatoWidget> ObtenerDatosAsync();
}

public class ArqueoTurnoWidget(
    ITurnoCajaRepositorio turnos,
    IVentaRepositorio ventas,
    SesionActual sesion) : WidgetBase(
        "caja.arqueo_turno",
        "Arqueo del turno",
        GruposWidget.Caja,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(1),
        Permiso.AbrirCerrarTurno)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        if (!sesion.EstaAutenticada)
            return new DatoWidget([new PuntoDato("Sin sesión", 0)]);

        var turno = await turnos
            .ObtenerAbiertoPorEmpleadoAsync(sesion.EmpleadoId);

        if (turno is null)
            return new DatoWidget([new PuntoDato("Sin turno abierto", 0)]);

        var delTurno = await ventas.ObtenerPorTurnoAsync(turno.Id);
        var esperado = turno.CalcularEfectivoEsperado(delTurno);

        return new DatoWidget(
        [
            new PuntoDato("Fondo inicial", turno.EfectivoInicial, FormatoDato.Moneda),
            new PuntoDato("Efectivo esperado", esperado, FormatoDato.Moneda)
        ]);
    }
}

public class IvaGeneradoWidget(
    IVentaRepositorio ventas,
    TimeProvider reloj) : WidgetBase(
        "finanzas.iva_generado",
        "IVA generado",
        GruposWidget.Finanzas,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(2),
        Permiso.VerFinanzas)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        var totalIva = completadas.Sum(v => v.TotalIva);

        return new DatoWidget(
        [
            new PuntoDato("IVA generado hoy", totalIva, FormatoDato.Moneda)
        ]);
    }
}

public class LotesPorVencerWidget(
    ServicioInventario alertas) : WidgetBase(
        "inventario.lotes_por_vencer",
        "Lotes por vencer",
        GruposWidget.Inventario,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(5),
        Permiso.VerInventario)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var lotes = await alertas.ObtenerLotesPorVencerAsync();
        return new DatoWidget(
        [
            new PuntoDato("Lotes en riesgo", lotes.Count)
        ]);
    }
}

public class ReabastecimientosPendientesWidget(
    IReabastecimientoRepositorio solicitudes) : WidgetBase(
        "inventario.reabastecimientos",
        "Reabastecimientos pendientes",
        GruposWidget.Inventario,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(5),
        Permiso.VerInventario)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var pendientes = await solicitudes.ObtenerPendientesAsync();
        return new DatoWidget([new PuntoDato("Pendientes", pendientes.Count)]);
    }
}

public class StockBajoMinimoWidget(
    ServicioInventario alertas) : WidgetBase(
        "inventario.stock_bajo_minimo",
        "Stock bajo el mínimo",
        GruposWidget.Inventario,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(3),
        Permiso.VerInventario)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var deficit = await alertas.ObtenerBajoMinimoAsync();
        return new DatoWidget(
        [
            new PuntoDato("Productos bajo mínimo", deficit.Count)
        ]);
    }
}

public class TrmDelDiaWidget(IServicioTrm trm) : WidgetBase(
    "externo.trm",
    "TRM del día",
    GruposWidget.Externo,
    TamanoWidget.Pequeno,
    TimeSpan.FromHours(1),
    Permiso.Ninguno)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var valor = await trm.ObtenerAsync();

        return valor is null
            ? new DatoWidget([])
            : new DatoWidget([new PuntoDato("TRM (centavos)", valor.Centavos, FormatoDato.MonedaEnCentavos)]);
    }
}

public class VentasDelDiaWidget(
    IVentaRepositorio ventas,
    TimeProvider reloj) : WidgetBase(
        "ventas.del_dia",
        "Ventas del día",
        GruposWidget.Ventas,
        TamanoWidget.Pequeno,
        TimeSpan.FromMinutes(1),
        Permiso.VerReportes)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        return new DatoWidget(
        [
            new PuntoDato("Total vendido", completadas.Sum(v => v.Total), FormatoDato.Moneda),
            new PuntoDato("Tickets", completadas.Count)
        ]);
    }
}

public sealed class ResumenOperativoWidget(
    IVentaRepositorio ventas,
    ServicioCaja caja,
    ServicioInventario alertas,
    TimeProvider reloj) : WidgetBase(
        "operacion.resumen",
        "Resumen operativo",
        GruposWidget.Ventas,
        TamanoWidget.Mediano,
        TimeSpan.FromMinutes(1),
        Permiso.VerReportes | Permiso.VerFinanzas | Permiso.VerInventario)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var delDia = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();
        var turno = await caja.ObtenerTurnoActualAsync();
        var stockBajo = await alertas.ObtenerBajoMinimoAsync();

        return new DatoWidget(
        [
            new PuntoDato("Ventas de hoy", delDia.Sum(v => v.Total), FormatoDato.Moneda),
            new PuntoDato("Tickets", delDia.Count),
            new PuntoDato("Turno abierto", turno is null ? 0 : 1),
            new PuntoDato("Productos bajo mínimo", stockBajo.Count)
        ]);
    }
}

public class VentasPorCategoriaWidget(
    IVentaRepositorio ventas,
    IProductoRepositorio productos,
    ICategoriaRepositorio categorias,
    TimeProvider reloj) : WidgetBase(
        "reportes.ventas_categoria",
        "Ventas por categoría",
        GruposWidget.Ventas,
        TamanoWidget.Mediano,
        TimeSpan.FromMinutes(2),
        Permiso.VerReportes)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        var todosProductos = (await productos.ObtenerTodosAsync())
            .ToDictionary(p => p.Id);
        var todasCategorias = (await categorias.ObtenerTodosAsync())
            .ToDictionary(c => c.Id, c => c.Nombre);

        var ventasPorCat = new Dictionary<string, long>();

        foreach (var v in completadas)
        {
            foreach (var d in v.Detalles)
            {
                if (!todosProductos.TryGetValue(d.ProductoId, out var prod))
                    continue;

                var nombreCat = todasCategorias.GetValueOrDefault(
                    prod.CategoriaId, "Sin categoría");

                ventasPorCat[nombreCat] =
                    ventasPorCat.GetValueOrDefault(nombreCat, 0) + d.Subtotal;
            }
        }

        var puntos = ventasPorCat
            .Select(kv => new PuntoDato(kv.Key, kv.Value, FormatoDato.Moneda))
            .ToList();

        return new DatoWidget(puntos);
    }
}

public class MargenPorCategoriaWidget(
    IVentaRepositorio ventas,
    IMovimientoRepositorio movimientos,
    ILoteRepositorio lotes,
    IProductoRepositorio productos,
    ICategoriaRepositorio categorias,
    TimeProvider reloj) : WidgetBase(
        "finanzas.margen_categoria",
        "Margen por categoría",
        GruposWidget.Finanzas,
        TamanoWidget.Mediano,
        TimeSpan.FromMinutes(5),
        Permiso.VerFinanzas)
{
    public override async Task<DatoWidget> ObtenerDatosAsync()
    {
        var (desde, hasta) = reloj.RangoDeHoyUtc();
        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();

        var todosLotes = (await lotes.ObtenerTodosAsync())
            .ToDictionary(l => l.Id);
        var todosProductos = (await productos.ObtenerTodosAsync())
            .ToDictionary(p => p.Id);
        var todasCategorias = (await categorias.ObtenerTodosAsync())
            .ToDictionary(c => c.Id, c => c.Nombre);

        var margenPorCategoria = new Dictionary<string, long>();

        foreach (var v in completadas)
        {
            var movs = await movimientos.ObtenerSalidasDeVentaAsync(v.Id);

            foreach (var grupoProducto in movs.GroupBy(m => m.ProductoId))
            {
                if (!todosProductos.TryGetValue(grupoProducto.Key, out var prod))
                    continue;

                var nombreCat = todasCategorias.GetValueOrDefault(
                    prod.CategoriaId, "Sin categoría");

                decimal costoTotal = 0;
                foreach (var movimiento in grupoProducto)
                {
                    var costoUnitario = todosLotes.TryGetValue(movimiento.LoteId, out var lote)
                        ? lote.CostoUnitario
                        : 0;
                    var costo = (decimal)movimiento.Cantidad * costoUnitario;
                    costoTotal += prod is ProductoPesado
                        ? decimal.Round(costo / ProductoPesado.GramosPorKilo, 0, MidpointRounding.AwayFromZero)
                        : costo;
                }

                var ingreso = v.Detalles
                    .Where(d => d.ProductoId == grupoProducto.Key)
                    .Sum(d => d.Subtotal - d.Iva);

                var margen = checked(ingreso - checked((long)costoTotal));

                margenPorCategoria[nombreCat] =
                    margenPorCategoria.GetValueOrDefault(nombreCat, 0) + margen;
            }
        }

        var puntos = margenPorCategoria
            .Select(kv => new PuntoDato(kv.Key, kv.Value, FormatoDato.Moneda))
            .ToList();

        return new DatoWidget(puntos);
    }
}
