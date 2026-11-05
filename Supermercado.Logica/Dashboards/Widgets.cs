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

namespace Supermercado.Logica.Dashboards.Widgets;

public class ArqueoTurnoWidget(
    ITurnoCajaRepositorio turnos,
    IVentaRepositorio ventas,
    ISesionActual sesion) : IWidget
{
    public string Id => "caja.arqueo_turno";
    public string Titulo => "Arqueo del turno";
    public string Grupo => GruposWidget.Caja;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(1);
    public Permiso PermisoRequerido => Permiso.AbrirCerrarTurno;

    public async Task<DatoWidget> ObtenerDatosAsync()
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
    TimeProvider reloj) : IWidget
{
    public string Id => "finanzas.iva_generado";
    public string Titulo => "IVA generado";
    public string Grupo => GruposWidget.Finanzas;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(2);
    public Permiso PermisoRequerido => Permiso.VerFinanzas;

    public async Task<DatoWidget> ObtenerDatosAsync()
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
    IServicioAlertasInventario alertas) : IWidget
{
    public string Id => "inventario.lotes_por_vencer";
    public string Titulo => "Lotes por vencer";
    public string Grupo => GruposWidget.Inventario;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var lotes = await alertas.ObtenerLotesPorVencerAsync();
        return new DatoWidget(
        [
            new PuntoDato("Lotes en riesgo", lotes.Count)
        ]);
    }
}

public class ReabastecimientosPendientesWidget(
    IReabastecimientoRepositorio solicitudes) : IWidget
{
    public string Id => "inventario.reabastecimientos";
    public string Titulo => "Reabastecimientos pendientes";
    public string Grupo => GruposWidget.Inventario;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var pendientes = await solicitudes.ObtenerPendientesAsync();
        return new DatoWidget([new PuntoDato("Pendientes", pendientes.Count)]);
    }
}

public class StockBajoMinimoWidget(
    IServicioAlertasInventario alertas) : IWidget
{
    public string Id => "inventario.stock_bajo_minimo";
    public string Titulo => "Stock bajo el mínimo";
    public string Grupo => GruposWidget.Inventario;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(3);
    public Permiso PermisoRequerido => Permiso.VerInventario;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var deficit = await alertas.ObtenerBajoMinimoAsync();
        return new DatoWidget(
        [
            new PuntoDato("Productos bajo mínimo", deficit.Count)
        ]);
    }
}

public class TrmDelDiaWidget(IServicioTrm trm) : IWidget
{
    public string Id => "externo.trm";
    public string Titulo => "TRM del día";
    public string Grupo => GruposWidget.Externo;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromHours(1);
    public Permiso PermisoRequerido => Permiso.Ninguno;

    public async Task<DatoWidget> ObtenerDatosAsync()
    {
        var valor = await trm.ObtenerAsync();

        return valor is null
            ? new DatoWidget([])
            : new DatoWidget([new PuntoDato("TRM (centavos)", valor.Centavos, FormatoDato.MonedaEnCentavos)]);
    }
}

public class VentasDelDiaWidget(
    IVentaRepositorio ventas,
    TimeProvider reloj) : IWidget
{
    public string Id => "ventas.del_dia";
    public string Titulo => "Ventas del día";
    public string Grupo => GruposWidget.Ventas;
    public TamanoWidget Tamano => TamanoWidget.Pequeno;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(1);
    public Permiso PermisoRequerido => Permiso.VerReportes;

    public async Task<DatoWidget> ObtenerDatosAsync()
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

public class VentasPorCategoriaWidget(
    IVentaRepositorio ventas,
    IProductoRepositorio productos,
    IRepositorio<Categoria> categorias,
    TimeProvider reloj) : IWidget
{
    public string Id => "reportes.ventas_categoria";
    public string Titulo => "Ventas por categoría";
    public string Grupo => GruposWidget.Ventas;
    public TamanoWidget Tamano => TamanoWidget.Mediano;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(2);
    public Permiso PermisoRequerido => Permiso.VerReportes;

    public async Task<DatoWidget> ObtenerDatosAsync()
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
    IRepositorio<Categoria> categorias,
    TimeProvider reloj) : IWidget
{
    public string Id => "finanzas.margen_categoria";
    public string Titulo => "Margen por categoría";
    public string Grupo => GruposWidget.Finanzas;
    public TamanoWidget Tamano => TamanoWidget.Mediano;
    public TimeSpan FrecuenciaActualizacion => TimeSpan.FromMinutes(5);
    public Permiso PermisoRequerido => Permiso.VerFinanzas;

    public async Task<DatoWidget> ObtenerDatosAsync()
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
