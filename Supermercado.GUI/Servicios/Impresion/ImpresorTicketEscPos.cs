using System.Text;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.Servicios.Impresion;

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

        // 3. Secuencia nativa ESC/POS para impresión de código QR 2D (GS ( k)
        var qrContenido = !string.IsNullOrWhiteSpace(venta.CadenaQr)
            ? venta.CadenaQr
            : $"https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey={venta.Cude ?? "DEMO"}";
        EscribirSecuenciaQrEscPos(ms, qrContenido);

        // 4. Avance de papel y corte automático
        ms.Write(CmdAvanceLineas);
        ms.Write(CmdCortePapelTotal);

        // 5. Si hubo pago en efectivo, abrir gaveta monedero
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

        var prefijo = string.IsNullOrWhiteSpace(venta.PrefijoDian) ? "POS" : venta.PrefijoDian;
        var consecutivo = venta.ConsecutivoDian > 0 ? venta.ConsecutivoDian : venta.Id;
        var baseNombre = $"Ticket_{prefijo}{consecutivo}_{DateTime.Now:yyyyMMdd_HHmmss}";

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

    private static void EscribirSecuenciaQrEscPos(Stream destino, string datosQr)
    {
        var qrBytes = Encoding.ASCII.GetBytes(datosQr);
        var len = qrBytes.Length + 3;
        var pL = (byte)(len % 256);
        var pH = (byte)(len / 256);

        // Seleccionar modelo QR 2: GS ( k 4 0 49 65 50 0
        destino.Write([0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00]);
        // Tamaño de módulo 5: GS ( k 3 0 49 67 5
        destino.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x05]);
        // Nivel de corrección de error M (49): GS ( k 3 0 49 69 49
        destino.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31]);
        // Almacenar datos en buffer QR: GS ( k pL pH 49 80 48 [datos]
        destino.Write([0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30]);
        destino.Write(qrBytes);
        // Imprimir QR almacenado: GS ( k 3 0 49 81 48
        destino.Write([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30]);
    }
}
