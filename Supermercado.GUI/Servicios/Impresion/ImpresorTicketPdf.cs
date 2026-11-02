using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Reportes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.Servicios.Impresion;

/// <summary>
/// Adaptador de impresión y exportación en PDF de 80mm mediante QuestPDF
/// para envío al correo del cliente, copia contable o auditoría de supervisión.
/// </summary>
public class ImpresorTicketPdf(
    IGeneradorTicket generadorTicket,
    IServicioParametros parametros) : IImpresorTicket
{
    public string CarpetaExportacion { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SIGS",
        "TicketsPdf");

    public Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default)
        => PlantillaTicketPos.GenerarTextoTirillaAsync(venta, parametros);

    public Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default)
        => generadorTicket.GenerarPdfAsync(venta);

    public async Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default)
    {
        Directory.CreateDirectory(CarpetaExportacion);
        var pdfBytes = await generadorTicket.GenerarPdfAsync(venta);

        var prefijo = string.IsNullOrWhiteSpace(venta.PrefijoDian) ? "POS" : venta.PrefijoDian;
        var consecutivo = venta.ConsecutivoDian > 0 ? venta.ConsecutivoDian : venta.Id;
        var rutaPdf = Path.Combine(CarpetaExportacion, $"Factura_{prefijo}{consecutivo}.pdf");

        await File.WriteAllBytesAsync(rutaPdf, pdfBytes, ct);
    }

    public Task AbrirGavetaAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
