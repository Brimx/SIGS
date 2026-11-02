using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Ventas;

public record ItemVentaEnEspera(
    int Id,
    DateTime FechaCreacion,
    string IdentificadorCliente,
    int TotalItems,
    long TotalEstimado,
    string? Nota);

public interface IServicioVentasEnEspera
{
    Task<VentaEnEspera> SuspenderVentaAsync(
        int cajaId,
        IReadOnlyList<LineaVenta> lineas,
        int? clienteId,
        int puntosARedimir,
        string identificadorCliente,
        long totalEstimado,
        string? nota = null);

    Task<IReadOnlyList<ItemVentaEnEspera>> ObtenerVentasEnEsperaAsync(int cajaId);

    Task<(VentaEnEspera Venta, IReadOnlyList<LineaVenta> Lineas)> RecuperarVentaAsync(int id);

    Task CancelarVentaEnEsperaAsync(int id);
}
