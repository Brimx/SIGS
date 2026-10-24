namespace Supermercado.Logica.Comunes;

public static class ExtensionesReloj
{
    public static DateTime AhoraUtc(this TimeProvider reloj)
        => reloj.GetUtcNow().UtcDateTime;

    public static DateOnly HoyLocal(this TimeProvider reloj)
        => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    public static (DateTime Desde, DateTime Hasta) RangoDeHoyUtc(
        this TimeProvider reloj)
    {
        var inicio = reloj.HoyLocal().ToDateTime(TimeOnly.MinValue);
        var desde = TimeZoneInfo.ConvertTimeToUtc(inicio, reloj.LocalTimeZone);
        return (desde, desde.AddDays(1));
    }
}
