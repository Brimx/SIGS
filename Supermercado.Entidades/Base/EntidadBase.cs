namespace Supermercado.Entidades.Base;

public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// Todo el dinero son pesos enteros; las tarifas, puntos básicos.
public static class Dinero
{
    public const int PuntosBasicosCien = 10000;

    public static long DividirRedondeando(long numerador, long divisor)
        => (numerador + divisor / 2) / divisor;

    public static long Porcentaje(long valor, int puntosBasicos)
        => DividirRedondeando(valor * puntosBasicos, PuntosBasicosCien);
}
