using System.Globalization;
using System.Text;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.Servicios.Impresion;

/// <summary>
/// Generador de tirilla de facturación electrónica POS de 80mm (48 columnas)
/// calibrada contra el estándar comercial de Tiendas Ara y la Resolución DIAN 000165 de 2023.
/// </summary>
public static class PlantillaTicketPos
{
    public const int AnchoColumnas = 48;
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public static string ObtenerCodigoTarifa(int puntosBasicosIva) => puntosBasicosIva switch
    {
        0 => "C",
        500 => "M",
        1900 => "J",
        _ => "G"
    };

    public static async Task<string> GenerarTextoTirillaAsync(
        Venta venta,
        IServicioParametros parametros)
    {
        var nombreTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioNombre, "SUPERMERCADO SIGS S.A.S.");
        var nitTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioNit, "900.480.569-1");
        var direccionTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioDireccion, "CL 18 BIS 1 #36-04");
        var telefonoTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioTelefono, "3009124523");

        var resNumero = await ObtenerSeguroAsync(parametros, ClavesParametro.DianResolucionNumero, "18764102352817");
        var resFecha = await ObtenerSeguroAsync(parametros, ClavesParametro.DianResolucionFecha, "2025-12-01");
        var resVigencia = await ObtenerSeguroAsync(parametros, ClavesParametro.DianResolucionVigenciaHasta, "2027-06-01");
        var prefijo = !string.IsNullOrWhiteSpace(venta.PrefijoDian)
            ? venta.PrefijoDian
            : await ObtenerSeguroAsync(parametros, ClavesParametro.DianPrefijo, "709B");
        var rangoDesde = await ObtenerSeguroAsync(parametros, ClavesParametro.DianRangoDesde, "169477");
        var rangoHasta = await ObtenerSeguroAsync(parametros, ClavesParametro.DianRangoHasta, "999999");

        var consecutivo = venta.ConsecutivoDian > 0 ? venta.ConsecutivoDian : venta.Id;
        var numFactura = $"{prefijo}{consecutivo}";
        var fechaGen = venta.Fecha.ToLocalTime();
        var fechaVal = (venta.FechaValidacionDian ?? venta.Fecha.AddSeconds(3)).ToLocalTime();

        var sb = new StringBuilder();

        // 1. Cabecera comercial y calidades tributarias (estilo Tiendas Ara)
        sb.AppendLine(Centrar($"{nombreTienda.ToUpperInvariant()}  NIT {nitTienda}"));
        sb.AppendLine(Centrar("GRAN CONTRIBUYENTE RES. 000200/24 AGENTE RETENEDOR"));
        sb.AppendLine(Centrar($"DE IVA AUTOR.DE RENTA RES.000492/2014 TEL {telefonoTienda}"));
        sb.AppendLine(Separador('-'));

        sb.AppendLine(AlinearExtremos("FACTURA ELECTRONICA DE VENTA N:", numFactura));
        sb.AppendLine($"GENERACION: {fechaGen:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"VALIDACION DIAN: {fechaVal:yyyy-MM-dd HH:mm:ss}");

        var clienteNombre = !string.IsNullOrWhiteSpace(venta.NombreAdquirente)
            ? venta.NombreAdquirente
            : Venta.NombreConsumidorFinal;
        var clienteDoc = !string.IsNullOrWhiteSpace(venta.DocumentoAdquirente)
            ? venta.DocumentoAdquirente
            : Venta.DocumentoConsumidorFinal;

        sb.AppendLine($"CLIENTE: {Truncar(clienteNombre, 22),-22} ID: {clienteDoc}");
        sb.AppendLine($"DIRECCION: {Truncar(direccionTienda, 20),-20} FORMA PAGO: CONTADO");
        sb.AppendLine($"TOTAL ART: {venta.Detalles.Count}");
        sb.AppendLine(Separador('-'));

        // 2. Cabecera de columnas de detalle
        sb.AppendLine("#  CODIGO        DESCRIP          ID CANT   UM  VALOR/U  VALOR/T");
        sb.AppendLine(Separador('-'));

        var idx = 1;
        foreach (var d in venta.Detalles)
        {
            var codigo = Truncar(d.Producto?.CodigoBarras ?? d.ProductoId.ToString(), 13).PadRight(13);
            var descrip = Truncar(d.Producto?.Nombre ?? $"PROD {d.ProductoId}", 16).PadRight(16);
            var idTarifa = ObtenerCodigoTarifa(d.PuntosBasicosIva);

            string cantUm;
            if (d.Producto is ProductoPesado)
            {
                var kg = d.Cantidad / 1000.0m;
                cantUm = $"{kg.ToString("0.000", CulturaCo),6} KG";
            }
            else
            {
                cantUm = $"{d.Cantidad,6} UN";
            }

            var valUnit = FormatearMonto(d.PrecioUnitario).PadLeft(8);
            var valTot = FormatearMonto(d.Subtotal).PadLeft(8);

            sb.AppendLine($"{idx,2} {codigo} {descrip} {idTarifa} {cantUm} {valUnit} {valTot}");
            idx++;
        }

        sb.AppendLine(Separador('-'));

        // 3. Totales y medios de pago
        sb.AppendLine(AlinearExtremos("TOTALES: *RECARGOS: 0  DESCUENTOS: -0", $"FACTURA: {FormatearMonto(venta.Total)}"));
        foreach (var pago in venta.Pagos)
        {
            sb.AppendLine(AlinearExtremos($"MEDIO/PAGO: {pago.Metodo}:", FormatearMonto(pago.Monto)));
        }

        if (venta.Cambio > 0)
        {
            sb.AppendLine(AlinearExtremos("CAMBIO / DEVUELTA:", FormatearMonto(venta.Cambio)));
        }

        sb.AppendLine(AlinearExtremos("Total:", FormatearMonto(venta.Total)));
        sb.AppendLine(Separador('-'));

        // 4. Discriminación de impuestos por tarifa (J: 19%, M: 5%, C: 0%)
        sb.AppendLine("DESCRIPCION            BASE/CANTIDAD     IVA/IMP CONSUMO");

        AgregarFilaImpuesto(sb, venta, 1900, "J:IVA 19%");
        AgregarFilaImpuesto(sb, venta, 500, "M:IVA 5%");
        AgregarFilaImpuesto(sb, venta, 0, "C:IVA 0%");

        sb.AppendLine(AlinearExtremos("TOTAL IMPUESTOS:", FormatearMonto(venta.TotalIva)));
        sb.AppendLine(Separador('-'));

        // 5. TrackID, Resolución DIAN, CUFE/CUDE y QR
        var trackId = $"477448{fechaGen:yyyyMMddHHmm}{consecutivo:D10}";
        sb.AppendLine($"Su TrackID es: {trackId}");
        sb.AppendLine($"SIGS-POS Resolucion#{resNumero} de {resFecha}");
        sb.AppendLine($"vigencia hasta {resVigencia} Habilita {prefijo} Rango {rangoDesde} a {rangoHasta}");

        var cude = !string.IsNullOrWhiteSpace(venta.Cude)
            ? venta.Cude
            : GenerarCudeRespaldo(numFactura, fechaGen, venta.Total);

        sb.AppendLine($"CUFE/CUDE: {cude}");
        var qrUrl = !string.IsNullOrWhiteSpace(venta.CadenaQr)
            ? venta.CadenaQr
            : $"https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey={cude}";
        sb.AppendLine($"QR DIAN: {qrUrl}");

        if (venta.Cliente is not null || venta.PuntosGanados > 0 || venta.PuntosRedimidos > 0)
        {
            sb.AppendLine(Separador('-'));
            sb.AppendLine($"PUNTOS GANADOS: +{venta.PuntosGanados}  REDIMIDOS: -{venta.PuntosRedimidos}");
        }

        sb.AppendLine(Separador('='));
        sb.AppendLine(Centrar("¡GRACIAS POR SU COMPRA!"));

        return sb.ToString();
    }

    private static void AgregarFilaImpuesto(StringBuilder sb, Venta venta, int puntosBasicos, string etiqueta)
    {
        var detallesTarifa = venta.Detalles.Where(d => d.PuntosBasicosIva == puntosBasicos).ToList();
        var subtotalTarifa = detallesTarifa.Sum(d => d.Subtotal);
        var ivaTarifa = detallesTarifa.Sum(d => d.Iva);
        var baseGravable = Math.Max(0, subtotalTarifa - ivaTarifa);

        sb.AppendLine(
            $"{etiqueta,-20} {FormatearMonto(baseGravable),15} {FormatearMonto(ivaTarifa),19}");
    }

    private static string FormatearMonto(long pesos)
        => pesos.ToString("N0", CulturaCo);

    private static string Centrar(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length >= AnchoColumnas)
            return texto;
        var pad = (AnchoColumnas - texto.Length) / 2;
        return texto.PadLeft(pad + texto.Length);
    }

    private static string AlinearExtremos(string izquierda, string derecha)
    {
        var espacio = AnchoColumnas - izquierda.Length - derecha.Length;
        return espacio > 0
            ? izquierda + new string(' ', espacio) + derecha
            : $"{izquierda} {derecha}";
    }

    private static string Truncar(string texto, int max)
        => texto.Length <= max ? texto : texto[..max];

    private static string Separador(char caracter)
        => new(caracter, AnchoColumnas);

    private static async Task<string> ObtenerSeguroAsync(
        IServicioParametros parametros,
        string clave,
        string porDefecto)
    {
        try
        {
            var valor = await parametros.ObtenerTextoAsync(clave);
            return string.IsNullOrWhiteSpace(valor) ? porDefecto : valor;
        }
        catch
        {
            return porDefecto;
        }
    }

    private static string GenerarCudeRespaldo(string numFactura, DateTime fecha, long total)
    {
        var semilla = $"{numFactura}|{fecha:yyyyMMddHHmmss}|{total}|SIGS-DIAN";
        var bytes = System.Security.Cryptography.SHA384.HashData(Encoding.UTF8.GetBytes(semilla));
        return Convert.ToHexStringLower(bytes);
    }
}
