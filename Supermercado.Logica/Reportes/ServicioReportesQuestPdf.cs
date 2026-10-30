using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;

namespace Supermercado.Logica.Reportes;

public class ServicioReportesQuestPdf(
    ISesionActual sesion) : IServicioReportes
{
    static ServicioReportesQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerarDesprendibleNominaAsync(Nomina nomina)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        var doc = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("DESPRENDIBLE DE NÓMINA")
                        .Bold().FontSize(14);
                    col.Item().AlignCenter().Text(
                        $"Periodo: {nomina.Anio} - Mes {nomina.Mes}");
                    col.Item().LineHorizontal(1);
                });

                pagina.Content().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Empleado ID: {nomina.EmpleadoId}");
                    col.Item().Text(
                        $"Salario Base: ${nomina.SalarioBase:N0}");
                    col.Item().Text(
                        $"Auxilio Transporte: ${nomina.AuxilioTransporte:N0}");
                    col.Item().Text(
                        $"Deducción Salud: ${nomina.DeduccionSalud:N0}");
                    col.Item().Text(
                        $"Deducción Pensión: ${nomina.DeduccionPension:N0}");
                    col.Item().LineHorizontal(1);
                    col.Item().Text(
                        $"NETO A PAGAR: ${nomina.Neto:N0}")
                        .Bold().FontSize(12);
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> GenerarReporteVentasAsync(
        DateTime desde,
        DateTime hasta,
        IReadOnlyList<Venta> ventas)
    {
        sesion.Exigir(Permiso.VerReportes);

        var doc = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9));

                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("REPORTE DE VENTAS")
                        .Bold().FontSize(14);
                    col.Item().AlignCenter().Text(
                        $"Desde: {desde:yyyy-MM-dd} Hasta: {hasta:yyyy-MM-dd}");
                    col.Item().LineHorizontal(1);
                });

                pagina.Content().Table(tabla =>
                {
                    tabla.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    tabla.Header(header =>
                    {
                        header.Cell().Text("ID").Bold();
                        header.Cell().Text("Fecha").Bold();
                        header.Cell().Text("Estado").Bold();
                        header.Cell().AlignRight().Text("Total").Bold();
                    });

                    foreach (var v in ventas)
                    {
                        tabla.Cell().Text(v.Id.ToString());
                        tabla.Cell().Text(v.Fecha.ToString("yyyy-MM-dd HH:mm"));
                        tabla.Cell().Text(v.Estado.ToString());
                        tabla.Cell().AlignRight().Text($"${v.Total:N0}");
                    }
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> GenerarReporteInventarioAsync(
        IReadOnlyList<StockConsolidado> inventario)
    {
        sesion.Exigir(Permiso.VerInventario);

        var doc = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9));

                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("REPORTE DE INVENTARIO")
                        .Bold().FontSize(14);
                    col.Item().LineHorizontal(1);
                });

                pagina.Content().Table(tabla =>
                {
                    tabla.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                    });

                    tabla.Header(header =>
                    {
                        header.Cell().Text("Producto").Bold();
                        header.Cell().Text("Código").Bold();
                        header.Cell().Text("Lote").Bold();
                        header.Cell().Text("Vence").Bold();
                        header.Cell().AlignRight().Text("Estante").Bold();
                        header.Cell().AlignRight().Text("Bodega").Bold();
                        header.Cell().AlignRight().Text("Total").Bold();
                    });

                    foreach (var item in inventario)
                    {
                        tabla.Cell().Text(item.NombreProducto);
                        tabla.Cell().Text(item.CodigoBarras);
                        tabla.Cell().Text(item.NumeroLote);
                        tabla.Cell().Text(
                            item.FechaVencimiento.ToString("yyyy-MM-dd"));
                        tabla.Cell().AlignRight().Text(
                            item.CantidadEstante.ToString());
                        tabla.Cell().AlignRight().Text(
                            item.CantidadBodega.ToString());
                        tabla.Cell().AlignRight().Text(
                            item.TotalStock.ToString());
                    }
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    public Task<byte[]> GenerarReporteArqueoAsync(
        TurnoCaja turno,
        ResultadoArqueo arqueo)
    {
        sesion.Exigir(Permiso.AbrirCerrarTurno);

        var doc = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("REPORTE DE ARQUEO DE CAJA")
                        .Bold().FontSize(14);
                    col.Item().AlignCenter().Text($"Turno #: {turno.Id}");
                    col.Item().LineHorizontal(1);
                });

                pagina.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Caja ID: {turno.CajaId}");
                    col.Item().Text($"Cajero ID: {turno.EmpleadoId}");
                    var fmt = "yyyy-MM-dd HH:mm";
                    var fAper = turno.FechaApertura.ToString(fmt);
                    var fCier = turno.FechaCierre?.ToString(fmt);
                    col.Item().Text($"Apertura: {fAper} UTC");
                    col.Item().Text($"Cierre: {fCier} UTC");
                    col.Item().LineHorizontal(1);
                    col.Item().Text(
                        $"Fondo Inicial: ${turno.EfectivoInicial:N0}");
                    col.Item().Text(
                        $"Efectivo Esperado: ${arqueo.Esperado:N0}");
                    col.Item().Text(
                        $"Efectivo Contado: ${arqueo.Contado:N0}");
                    col.Item().Text(
                        $"Diferencia: ${arqueo.Diferencia:N0}");
                    var dentro = arqueo.DentroDeTolerancia ? "SÍ" : "NO";
                    col.Item().Text($"Dentro de Tolerancia: {dentro}").Bold();
                });

                pagina.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }
}
