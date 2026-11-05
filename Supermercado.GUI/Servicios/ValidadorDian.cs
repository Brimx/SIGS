namespace Supermercado.GUI.Servicios.Validacion;

/// <summary>
/// Validador tributario colombiano según estándar de la DIAN (Módulo 11 para NIT y Habeas Data Ley 1581).
/// </summary>
public static class ValidadorDian
{
    private static readonly int[] FactoresModulo11 =
        [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

    public const string TextoLegalHabeasData =
        "Autorizo de manera libre, previa, expresa e informada el tratamiento de mis datos personales " +
        "conforme a la Ley 1581 de 2012 y el Decreto 1377 de 2013 para fines de facturación electrónica " +
        "DIAN y programa de fidelización. Si no autoriza, la factura se emitirá como CONSUMIDOR FINAL (222222222222).";

    /// <summary>
    /// Calcula el dígito de verificación (DV) de un NIT colombiano usando el algoritmo oficial Módulo 11 de la DIAN.
    /// </summary>
    public static int CalcularDigitoVerificacion(string? nitSinDv)
    {
        if (string.IsNullOrWhiteSpace(nitSinDv))
            return -1;

        var soloDigitos = new string(nitSinDv.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length == 0 || soloDigitos.Length > FactoresModulo11.Length)
            return -1;

        var suma = 0;
        var longitud = soloDigitos.Length;

        for (var i = 0; i < longitud; i++)
        {
            var digito = soloDigitos[longitud - 1 - i] - '0';
            suma += digito * FactoresModulo11[i];
        }

        var residuo = suma % 11;
        return residuo > 1 ? 11 - residuo : residuo;
    }

    public static string FormatearNitConDv(string nitSinDv)
    {
        var limpio = new string(nitSinDv.Where(char.IsDigit).ToArray());
        var dv = CalcularDigitoVerificacion(limpio);
        return dv >= 0 ? $"{limpio}-{dv}" : nitSinDv;
    }

    public static bool ValidarNitCompleto(string? nitCompleto)
    {
        if (string.IsNullOrWhiteSpace(nitCompleto))
            return false;

        var partes = nitCompleto.Trim().Split('-');
        if (partes.Length != 2)
            return false;

        if (!int.TryParse(partes[1], out var dvProvisto))
            return false;

        var dvCalculado = CalcularDigitoVerificacion(partes[0]);
        return dvCalculado >= 0 && dvCalculado == dvProvisto;
    }
}
