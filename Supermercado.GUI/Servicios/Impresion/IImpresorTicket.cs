using Supermercado.Entidades.Ventas;

namespace Supermercado.GUI.Servicios.Impresion;

public interface IImpresorTicket
{
    Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default);
    Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default);
    Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default);
    Task AbrirGavetaAsync(CancellationToken ct = default);
}
