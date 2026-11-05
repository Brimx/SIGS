using System.IO;
using System.Text;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Reportes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.Servicios.Impresion;

public interface IImpresorTicket
{
    Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default);
    Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default);
    Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default);
    Task AbrirGavetaAsync(CancellationToken ct = default);
}

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

        var rutaPdf = Path.Combine(CarpetaExportacion, $"Ticket_POS{venta.Id}.pdf");

        await File.WriteAllBytesAsync(rutaPdf, pdfBytes, ct);
    }

    public Task AbrirGavetaAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}

/// <summary>
/// Adaptador de impresión térmica ESC/POS directa para rollo de 80mm (Epson TM, Star Micronics, Bixolon).
/// Soporta comandos nativos de corte de papel, apertura de gaveta monedero y código QR bidimensional.
/// </summary>
public class ImpresorTicketEscPos(IServicioParametros parametros) : IImpresorTicket
{
    // Secuencias estándar ESC/POS
    private static readonly byte[] CmdInicializar = [0x1B, 0x40]; // ESC @
    private static readonly byte[] CmdAvanceLineas = [0x1B, 0x64, 0x04]; // ESC d 4
    private static readonly byte[] CmdCortePapelTotal = [0x1D, 0x56, 0x00]; // GS V 0
    private static readonly byte[] CmdPulsoGaveta = [0x1B, 0x70, 0x00, 0x19, 0xFA]; // ESC p 0 25 250

    public string CarpetaSpool { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SIGS",
        "Tickets");

    public Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default)
        => PlantillaTicketPos.GenerarTextoTirillaAsync(venta, parametros);

    public async Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default)
    {
        var textoTirilla = await FormatearTirillaTextoAsync(venta, ct);
        using var ms = new MemoryStream();

        // 1. Inicializar impresora
        ms.Write(CmdInicializar);

        // 2. Cuerpo del tiquete en codificación Latin1 / CP858 compatible con impresoras térmicas
        var bytesTexto = Encoding.Latin1.GetBytes(textoTirilla);
        ms.Write(bytesTexto);

        // 3. Avance de papel y corte automático
        ms.Write(CmdAvanceLineas);
        ms.Write(CmdCortePapelTotal);

        // 4. Si hubo pago en efectivo, abrir gaveta monedero
        if (venta.EfectivoRecibido > 0)
        {
            ms.Write(CmdPulsoGaveta);
        }

        return ms.ToArray();
    }

    public async Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default)
    {
        Directory.CreateDirectory(CarpetaSpool);
        var texto = await FormatearTirillaTextoAsync(venta, ct);
        var bytesEscPos = await GenerarComandosEscPosAsync(venta, ct);

        var baseNombre = $"Ticket_POS{venta.Id}_{DateTime.Now:yyyyMMdd_HHmmss}";

        await File.WriteAllTextAsync(
            Path.Combine(CarpetaSpool, $"{baseNombre}.txt"),
            texto,
            Encoding.UTF8,
            ct);

        await File.WriteAllBytesAsync(
            Path.Combine(CarpetaSpool, $"{baseNombre}.escpos"),
            bytesEscPos,
            ct);
    }

    public async Task AbrirGavetaAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(CarpetaSpool);
        var rutaPulso = Path.Combine(CarpetaSpool, "ultimo_pulso_gaveta.escpos");
        await File.WriteAllBytesAsync(rutaPulso, CmdPulsoGaveta, ct);
    }
}
