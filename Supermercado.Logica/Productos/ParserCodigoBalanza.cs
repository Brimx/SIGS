namespace Supermercado.Logica.Productos;

// Parser para códigos de barras EAN-13 emitidos por balanzas de fruver,
// carnicería y charcutería con prefijo de medida variable GS1 (20 a 29).
// Formato estándar retail: PP [PLU: 5 dígitos] [GRAMOS: 5 dígitos] [CHECK: 1 dígito]
public static class ParserCodigoBalanza
{
    public static ResultadoCodigoBalanza Parsear(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return new ResultadoCodigoBalanza(false, "", "", 0, codigo ?? "");

        var limpio = codigo.Trim();
        if (limpio.Length != 13 || !limpio.All(char.IsDigit))
            return new ResultadoCodigoBalanza(false, "", "", 0, limpio);

        var prefijoNum = int.Parse(limpio[..2]);
        if (prefijoNum is < 20 or > 29)
            return new ResultadoCodigoBalanza(false, "", "", 0, limpio);

        var prefijo = limpio[..2];
        var plu = limpio[2..7];
        var gramos = int.Parse(limpio[7..12]);
        var digitoControl = limpio[12] - '0';

        return new ResultadoCodigoBalanza(true, prefijo, plu, gramos, limpio, digitoControl);
    }
}
