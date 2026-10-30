using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Dashboards.Widgets;

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

            foreach (var m in movs)
            {
                if (!todosProductos.TryGetValue(m.ProductoId, out var prod))
                    continue;

                var nombreCat = todasCategorias.GetValueOrDefault(
                    prod.CategoriaId, "Sin categoría");

                var costoUnitario = todosLotes.TryGetValue(m.LoteId, out var l)
                    ? l.CostoUnitario
                    : 0;
                var costoTotal = m.Cantidad * costoUnitario;

                // Detalle correspondiente para obtener ingreso neto sin IVA
                var detalle = v.Detalles
                    .FirstOrDefault(d => d.ProductoId == m.ProductoId);
                var ingreso = detalle is not null
                    ? (detalle.Subtotal - detalle.Iva)
                    : 0;

                var margen = ingreso - costoTotal;

                margenPorCategoria[nombreCat] =
                    margenPorCategoria.GetValueOrDefault(nombreCat, 0) + margen;
            }
        }

        var puntos = margenPorCategoria
            .Select(kv => new PuntoDato(kv.Key, kv.Value))
            .ToList();

        return new DatoWidget(puntos);
    }
}
