using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Dashboards.Widgets;

public class VentasPorCategoriaWidget(
    IVentaRepositorio ventas,
    IProductoRepositorio productos,
    IRepositorio<Categoria> categorias,
    TimeProvider reloj) : IWidget
{
    public string Id => "reportes.ventas_categoria";
    public string Titulo => "Ventas por categoría";
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
            .Select(kv => new PuntoDato(kv.Key, kv.Value))
            .ToList();

        return new DatoWidget(puntos);
    }
}
