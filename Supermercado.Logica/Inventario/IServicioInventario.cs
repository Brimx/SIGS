using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Inventario;

// Ninguna operación guarda: quien orquesta llama a GuardarAsync una vez.
public interface IServicioInventario
{
    Task DescontarVentaAsync(int productoId, int cantidad, Venta venta);
    Task ReintegrarVentaAsync(int ventaId);
    Task TrasladarAEstanteAsync(int productoId, int cantidad);
    Task RegistrarIngresoAsync(Lote lote, int cantidad);

    Task RegistrarMermaAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo);
}
