namespace Supermercado.Logica.Seguridad;

public static class ExtensionesParametros
{
    // Interruptor: «0», «false» o «no» apagan; cualquier otro valor enciende.
    // Si el parámetro no existe (base anterior a la clave) vale `porDefecto`.
    public static async Task<bool> ObtenerBoolAsync(
        this IServicioParametros parametros,
        string clave,
        bool porDefecto = true)
    {
        try
        {
            var texto = (await parametros.ObtenerTextoAsync(clave))
                .Trim().ToLowerInvariant();
            return texto is not ("0" or "false" or "no");
        }
        catch (Supermercado.Entidades.Excepciones.ParametroFaltanteException)
        {
            return porDefecto;
        }
    }

    public static async Task<string?> ObtenerTextoOpcionalAsync(
        this IServicioParametros parametros,
        string clave,
        string? porDefecto = null)
    {
        try
        {
            var texto = await parametros.ObtenerTextoAsync(clave);
            return string.IsNullOrWhiteSpace(texto) ? porDefecto : texto.Trim();
        }
        catch (Supermercado.Entidades.Excepciones.ParametroFaltanteException)
        {
            return porDefecto;
        }
    }
}
