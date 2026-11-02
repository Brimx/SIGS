using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Dian;

// Generador de estructura XML estándar UBL 2.1 para Documento Equivalente POS (Anexo Técnico DIAN 1.9).
public static class GeneradorUblDian
{
    private static readonly XNamespace NsInvoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace NsCac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace NsCbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    public static string GenerarXml(
        Venta venta,
        string resolucionNumero,
        string fechaResolucion,
        string vigenciaHasta,
        string nitEmisor,
        string nombreEmisor,
        string direccionEmisor)
    {
        var numDoc = $"{venta.PrefijoDian}{venta.ConsecutivoDian}";
        var fechaStr = venta.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var horaStr = venta.Fecha.ToString("HH:mm:ss-05:00", CultureInfo.InvariantCulture);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(NsInvoice + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", NsCac),
                new XAttribute(XNamespace.Xmlns + "cbc", NsCbc),
                new XElement(NsCbc + "UBLVersionID", "2.1"),
                new XElement(NsCbc + "CustomizationID", "10"),
                new XElement(NsCbc + "ProfileID", "DIAN 2.1: Documento Equivalente Electronico POS"),
                new XElement(NsCbc + "ID", numDoc),
                new XElement(NsCbc + "UUID",
                    new XAttribute("schemeName", "CUDE-SHA384"),
                    venta.Cude ?? string.Empty),
                new XElement(NsCbc + "IssueDate", fechaStr),
                new XElement(NsCbc + "IssueTime", horaStr),
                new XElement(NsCbc + "InvoiceTypeCode", "20"), // 20 = Documento Equivalente POS

                // Datos de la resolución DIAN
                new XElement(NsCac + "InvoicePeriod",
                    new XElement(NsCbc + "StartDate", fechaResolucion),
                    new XElement(NsCbc + "EndDate", vigenciaHasta),
                    new XElement(NsCbc + "Description", $"Resolución DIAN No. {resolucionNumero}")),

                // Emisor (Supermercado)
                new XElement(NsCac + "AccountingSupplierParty",
                    new XElement(NsCac + "Party",
                        new XElement(NsCac + "PartyTaxScheme",
                            new XElement(NsCbc + "RegistrationName", nombreEmisor),
                            new XElement(NsCbc + "CompanyID",
                                new XAttribute("schemeAgencyID", "195"),
                                new XAttribute("schemeID", "31"),
                                nitEmisor)),
                        new XElement(NsCac + "PhysicalLocation",
                            new XElement(NsCac + "Address",
                                new XElement(NsCbc + "Line", direccionEmisor))))),

                // Adquirente (Cliente o Consumidor Final)
                new XElement(NsCac + "AccountingCustomerParty",
                    new XElement(NsCac + "Party",
                        new XElement(NsCac + "PartyIdentification",
                            new XElement(NsCbc + "ID",
                                new XAttribute("schemeName", "13"), // Cédula o Consumidor final
                                venta.DocumentoAdquirente)),
                        new XElement(NsCac + "PartyTaxScheme",
                            new XElement(NsCbc + "RegistrationName", venta.NombreAdquirente)))),

                // Impuestos totales
                new XElement(NsCac + "TaxTotal",
                    new XElement(NsCbc + "TaxAmount",
                        new XAttribute("currencyID", "COP"),
                        Formato(venta.TotalIva)),
                    new XElement(NsCac + "TaxSubtotal",
                        new XElement(NsCbc + "TaxableAmount",
                            new XAttribute("currencyID", "COP"),
                            Formato(venta.Total - venta.TotalIva)),
                        new XElement(NsCbc + "TaxAmount",
                            new XAttribute("currencyID", "COP"),
                            Formato(venta.TotalIva)),
                        new XElement(NsCac + "TaxCategory",
                            new XElement(NsCac + "TaxScheme",
                                new XElement(NsCbc + "ID", "01"),
                                new XElement(NsCbc + "Name", "IVA"))))),

                // Totales monetarios
                new XElement(NsCac + "LegalMonetaryTotal",
                    new XElement(NsCbc + "LineExtensionAmount",
                        new XAttribute("currencyID", "COP"),
                        Formato(venta.Total - venta.TotalIva)),
                    new XElement(NsCbc + "TaxExclusiveAmount",
                        new XAttribute("currencyID", "COP"),
                        Formato(venta.Total - venta.TotalIva)),
                    new XElement(NsCbc + "TaxInclusiveAmount",
                        new XAttribute("currencyID", "COP"),
                        Formato(venta.Total)),
                    new XElement(NsCbc + "PayableAmount",
                        new XAttribute("currencyID", "COP"),
                        Formato(venta.Total))),

                // Líneas de detalle de la venta
                venta.Detalles.Select((d, idx) =>
                    new XElement(NsCac + "InvoiceLine",
                        new XElement(NsCbc + "ID", idx + 1),
                        new XElement(NsCbc + "InvoicedQuantity",
                            new XAttribute("unitCode", "EA"),
                            d.Cantidad),
                        new XElement(NsCbc + "LineExtensionAmount",
                            new XAttribute("currencyID", "COP"),
                            Formato(d.Subtotal - d.Iva)),
                        new XElement(NsCac + "TaxTotal",
                            new XElement(NsCbc + "TaxAmount",
                                new XAttribute("currencyID", "COP"),
                                Formato(d.Iva))),
                        new XElement(NsCac + "Item",
                            new XElement(NsCbc + "Description", $"Producto ID {d.ProductoId}"),
                            new XElement(NsCac + "ClassifiedTaxCategory",
                                new XElement(NsCbc + "Percent", (d.PuntosBasicosIva / 100.0).ToString("0.00", CultureInfo.InvariantCulture)),
                                new XElement(NsCac + "TaxScheme",
                                    new XElement(NsCbc + "ID", "01"),
                                    new XElement(NsCbc + "Name", "IVA")))),
                        new XElement(NsCac + "Price",
                            new XElement(NsCbc + "PriceAmount",
                                new XAttribute("currencyID", "COP"),
                                Formato(d.PrecioUnitario)))))
            )
        );

        return doc.ToString();
    }

    private static string Formato(long valor)
        => valor.ToString("0.00", CultureInfo.InvariantCulture);
}
