using System.Net.Mail;
using System.Text.RegularExpressions;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Comunes;

// Reglas de formato compartidas. El backend las exige siempre; la UI puede
// usar los métodos Es* para dar aviso mientras el usuario escribe.
public static partial class Validador
{
    public const int LongitudMaximaNombre = 80;

    [GeneratedRegex(@"^[\p{L}][\p{L}\p{M} '.\-]*$")]
    private static partial Regex PatronNombre();

    [GeneratedRegex(@"^\d{5,15}$")]
    private static partial Regex PatronDocumento();

    [GeneratedRegex(@"^\d{6,12}(-\d)?$")]
    private static partial Regex PatronNit();

    [GeneratedRegex(@"^(\+57)?\d{7,10}$")]
    private static partial Regex PatronTelefono();

    public static bool EsNombreValido(string? valor)
        => !string.IsNullOrWhiteSpace(valor)
           && valor.Trim().Length <= LongitudMaximaNombre
           && PatronNombre().IsMatch(valor.Trim());

    public static bool EsDocumentoValido(string? valor)
        => valor is not null && PatronDocumento().IsMatch(valor.Trim());

    public static bool EsNitValido(string? valor)
        => valor is not null && PatronNit().IsMatch(valor.Trim());

    // Vacío es válido: el teléfono es opcional.
    public static bool EsTelefonoValido(string? valor)
        => string.IsNullOrWhiteSpace(valor)
           || PatronTelefono().IsMatch(Compactar(valor));

    // Vacío es válido: el correo es opcional.
    public static bool EsCorreoValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return true;

        var texto = valor.Trim();
        return MailAddress.TryCreate(texto, out var dir)
               && dir.Address == texto
               && dir.Host.Contains('.');
    }

    public static string ExigirNombre(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionException($"Falta {campo}.");
        if (!EsNombreValido(valor))
            throw new ValidacionException(
                $"{Capitalizar(campo)} solo admite letras, espacios, "
                + "apóstrofo, punto y guion (sin números).");

        return valor.Trim();
    }

    public static string ExigirNombreOpcional(string? valor, string campo)
        => string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : ExigirNombre(valor, campo);

    public static string ExigirDocumento(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionException("Falta el documento.");
        if (!EsDocumentoValido(valor))
            throw new ValidacionException(
                "El documento debe tener solo números (5 a 15 dígitos).");

        return valor.Trim();
    }

    public static string ExigirNit(string? valor, string campo = "el NIT")
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionException($"Falta {campo}.");
        if (!EsNitValido(valor))
            throw new ValidacionException(
                "El NIT debe tener 6 a 12 dígitos y, opcionalmente, "
                + "guion con el dígito de verificación.");

        return valor.Trim();
    }

    public static string ExigirTelefonoOpcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;
        if (!EsTelefonoValido(valor))
            throw new ValidacionException(
                "El teléfono debe tener de 7 a 10 dígitos.");

        return Compactar(valor);
    }

    public static string ExigirCorreoOpcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;
        if (!EsCorreoValido(valor))
            throw new ValidacionException("El correo no tiene un formato válido.");

        return valor.Trim();
    }

    private static string Compactar(string valor)
        => valor.Replace(" ", "").Replace("-", "");

    private static string Capitalizar(string texto)
        => char.ToUpperInvariant(texto[0]) + texto[1..];

    private static readonly int[] FactoresDian =
        [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

    public static int CalcularDigitoVerificacionNit(string nit)
    {
        if (string.IsNullOrWhiteSpace(nit))
            throw new ValidacionException("El NIT no puede estar vacío.");

        var soloDigitos = new string(nit.Where(char.IsDigit).ToArray());
        if (soloDigitos.Length is < 5 or > 15)
            throw new ValidacionException("El NIT debe tener entre 5 y 15 dígitos.");

        var suma = 0;
        var len = soloDigitos.Length;
        for (var i = 0; i < len; i++)
        {
            var digito = soloDigitos[len - 1 - i] - '0';
            suma += digito * FactoresDian[i];
        }

        var residuo = suma % 11;
        return residuo is 0 or 1 ? residuo : 11 - residuo;
    }

    public static bool ValidarNitConDv(string? nitConDv)
    {
        if (string.IsNullOrWhiteSpace(nitConDv))
            return false;

        var partes = nitConDv.Trim().Split('-');
        if (partes.Length != 2)
            return false;

        var nit = partes[0].Trim();
        var dvStr = partes[1].Trim();

        if (dvStr.Length != 1 || !char.IsDigit(dvStr[0]))
            return false;

        var dvEsperado = dvStr[0] - '0';
        try
        {
            return CalcularDigitoVerificacionNit(nit) == dvEsperado;
        }
        catch (ValidacionException)
        {
            return false;
        }
    }

}
