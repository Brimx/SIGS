using Supermercado.Entidades.Productos;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Productos;

public interface IServicioProductos
{
    Task RegistrarAsync(Producto producto);

    Task<Producto?> BuscarPorCodigoBarrasAsync(string codigo);

    // Sugerencia externa: si falla o no encuentra, se registra a mano.
    Task<InfoProductoExterno?> ConsultarExternoAsync(
        string codigo,
        CancellationToken ct = default);

    Task RegistrarIngresoLoteAsync(
        int productoId,
        string numeroLote,
        DateOnly vencimiento,
        int cantidad,
        long costoUnitario);
}
