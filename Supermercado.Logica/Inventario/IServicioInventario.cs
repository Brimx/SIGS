namespace Supermercado.Logica.Inventario;

public interface IServicioInventario
{
    Task VenderDeEstanteAsync(int productoId, int cantidad);
    Task ReponerEstanteAsync(int productoId, int cantidad);
}
