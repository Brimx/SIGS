namespace Supermercado.Logica.Dian;

public record EstadoResolucionDian(
    string Numero,
    string Prefijo,
    DateOnly VigenciaHasta,
    long RangoDesde,
    long RangoHasta,
    long SiguienteConsecutivo,
    bool EstaVigente,
    bool RangoDisponible,
    bool AlertaVencimientoProximo,
    bool AlertaAgotamientoRango,
    int DiasRestantes,
    long FoliosRestantes)
{
    public bool EsValida => EstaVigente && RangoDisponible;
    public bool EstaVencidaPorFecha => !EstaVigente;
    public bool EstaAgotadaPorFolios => !RangoDisponible;

    public string? Alerta =>
        !EstaVigente ? "Resolución DIAN vencida." :
        !RangoDisponible ? "Rango de consecutivos DIAN agotado." :
        AlertaAgotamientoRango ? $"Quedan {FoliosRestantes} folios de la resolución DIAN." :
        AlertaVencimientoProximo ? $"La resolución DIAN vence en {DiasRestantes} días." :
        null;
}
