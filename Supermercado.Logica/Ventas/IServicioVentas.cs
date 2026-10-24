using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Ventas;

public interface IServicioVentas
{
    Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud);
    Task AnularVentaAsync(int ventaId, string motivo);
}
