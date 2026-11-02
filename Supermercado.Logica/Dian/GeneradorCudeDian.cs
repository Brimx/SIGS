using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Dian;

// Generador de Código Único de Documento Equivalente (CUDE) según Resolución DIAN 000165 de 2023.
// Algoritmo: SHA-384 en minúsculas (96 caracteres hexadecimales).
public static class GeneradorCudeDian
{
    public const string UrlCatalogoVpfe =
        "https://catalogo-vpfe.dian.gov.co/document/searchqr?documentkey=";

    public static string CalcularCude(
        string numDoc,
        DateTime fechaUtc,
        long subtotal,
        long totalIva,
        long total,
        string nitEmisor,
        string numAdquirente,
        string claveTecnica,
        string tipoAmbiente)
    {
        var fecDoc = fechaUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var horDoc = fechaUtc.ToString("HH:mm:ss-05:00", CultureInfo.InvariantCulture);

        var valFac = FormatoMoneda(subtotal);
        var valImp1 = FormatoMoneda(totalIva);
        var codImp1 = "01"; // 01 = IVA
        var codImp2 = "04"; // 04 = Impoconsumo
        var valImp2 = "0.00";
        var codImp3 = "03"; // 03 = ICA
        var valImp3 = "0.00";
        var valTot = FormatoMoneda(total);

        var nitLimpio = new string(nitEmisor.Where(char.IsDigit).ToArray());
        var adqLimpio = string.IsNullOrWhiteSpace(numAdquirente)
            ? Venta.DocumentoConsumidorFinal
            : new string(numAdquirente.Where(char.IsDigit).ToArray());

        var cadenaCude = string.Concat(
            numDoc,
            fecDoc,
            horDoc,
            valFac,
            codImp1,
            valImp1,
            codImp2,
            valImp2,
            codImp3,
            valImp3,
            valTot,
            nitLimpio,
            adqLimpio,
            claveTecnica.Trim(),
            tipoAmbiente.Trim());

        using var sha = SHA384.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(cadenaCude));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GenerarCadenaQr(string cude)
        => $"{UrlCatalogoVpfe}{cude}";

    private static string FormatoMoneda(long centavosOPesos)
        => (centavosOPesos).ToString("0.00", CultureInfo.InvariantCulture);
}
