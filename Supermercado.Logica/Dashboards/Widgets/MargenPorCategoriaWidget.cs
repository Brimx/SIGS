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
            .Select(kv => new PuntoDato(kv.Key, kv.Value))
            .ToList();

        return new DatoWidget(puntos);
    }
}
