using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Reportes;

public interface IGeneradorTicket
{
    Task<byte[]> GenerarPdfAsync(Venta venta);
}
