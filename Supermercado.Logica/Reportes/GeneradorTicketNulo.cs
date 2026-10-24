using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Reportes;

public class GeneradorTicketNulo : IGeneradorTicket
{
    public Task<byte[]> GenerarPdfAsync(Venta venta)
        => Task.FromResult(Array.Empty<byte>());
}
