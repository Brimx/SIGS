using Supermercado.Entidades.Inventario;

namespace Supermercado.Logica.Inventario;

public interface IServicioMermas
{
    Task RegistrarAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo);
}
