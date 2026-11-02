using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Reportes;

public class GeneradorTicketQuestPdf(
    IServicioParametros parametros) : IGeneradorTicket
{
    static GeneradorTicketQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerarPdfAsync(Venta venta)
    {
        var nombreNegocio = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioNombre);
        var nit = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioNit);
        var direccion = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioDireccion);
        var telefono = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioTelefono);

        var doc = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.ContinuousSize(80, Unit.Millimetre);
                pagina.Margin(5, Unit.Millimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9));

                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(nombreNegocio)
                        .Bold().FontSize(11);
                    col.Item().AlignCenter().Text(nit);
                    col.Item().AlignCenter().Text(direccion);
                    col.Item().AlignCenter().Text(telefono);
                    col.Item().LineHorizontal(1);
                    col.Item().Text($"Ticket #: {venta.Id}");
                    col.Item().Text(
                        $"Fecha: {venta.Fecha:yyyy-MM-dd HH:mm:ss} UTC");
                    col.Item().Text($"Turno #: {venta.TurnoCajaId}");
                    if (venta.Cliente is not null || venta.ClienteId is not null)
                    {
                        var nombre = venta.Cliente?.NombreCompleto;
                        var doc = venta.Cliente?.Documento;
                        var textoCliente = !string.IsNullOrWhiteSpace(nombre)
                            ? $"Cliente: {nombre} ({doc})"
                            : $"Cliente ID: {venta.ClienteId}";
                        col.Item().Text(textoCliente);

                        if (venta.PuntosGanados > 0)
                            col.Item().Text($"Puntos ganados: +{venta.PuntosGanados}");
                        if (venta.PuntosRedimidos > 0)
                            col.Item().Text($"Puntos redimidos: -{venta.PuntosRedimidos}");
                        if (venta.Cliente is not null)
                            col.Item().Text($"Saldo puntos: {venta.Cliente.PuntosAcumulados}");
                    }
                    col.Item().LineHorizontal(1);
                });

                pagina.Content().Column(col =>
                {
                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn(3);
                            columnas.RelativeColumn(1);
                            columnas.RelativeColumn(2);
                        });

                        tabla.Header(encabezado =>
                        {
                            encabezado.Cell().Text("Cant/Prod").Bold();
                            encabezado.Cell().AlignRight().Text("IVA").Bold();
                            encabezado.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var det in venta.Detalles)
                        {
                            var desc = $"{det.Cantidad}x Prod {det.ProductoId}";
                            tabla.Cell().Text(desc);
                            tabla.Cell().AlignRight().Text($"${det.Iva:N0}");
                            tabla.Cell().AlignRight().Text(
                                $"${det.Subtotal:N0}");
                        }
                    });

                    col.Item().LineHorizontal(1);
                    col.Item().AlignRight().Text(
                        $"TOTAL: ${venta.Total:N0}").Bold().FontSize(11);
                    col.Item().AlignRight().Text(
                        $"IVA Total: ${venta.TotalIva:N0}");

                    col.Item().LineHorizontal(1);
                    col.Item().Text("PAGOS:").Bold();
                    foreach (var p in venta.Pagos)
                        col.Item().Text($"{p.Metodo}: ${p.Monto:N0}");

                    if (venta.Cambio > 0)
                        col.Item().Text($"Cambio: ${venta.Cambio:N0}");

                    if (!string.IsNullOrWhiteSpace(venta.Cude))
                    {
                        col.Item().LineHorizontal(1);
                        col.Item().Text($"DOC. EQUIVALENTE POS: {venta.PrefijoDian}{venta.ConsecutivoDian}").Bold();
                        col.Item().Text($"Adquirente: {venta.NombreAdquirente} ({venta.DocumentoAdquirente})");
                        var cudeCorto = venta.Cude.Length >= 32
                            ? $"{venta.Cude[..16]}...{venta.Cude[^16..]}"
                            : venta.Cude;
                        col.Item().Text($"CUDE: {cudeCorto}").FontSize(7);
                        if (!string.IsNullOrWhiteSpace(venta.CadenaQr))
                            col.Item().Text($"QR: {venta.CadenaQr}").FontSize(7);
                    }
                });

                pagina.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1);
                    col.Item().AlignCenter().Text(
                        "¡Gracias por su compra!").Italic();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
