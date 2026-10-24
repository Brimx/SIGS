using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Finanzas;

// PuntosBasicos es entero, igual que el dinero: 1900 equivale a 19 %.
public abstract class Impuesto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int PuntosBasicos { get; set; }
    public bool Activo { get; set; } = true;

    public decimal Porcentaje
        => PuntosBasicos * 100m / Dinero.PuntosBasicosCien;

    public abstract long Calcular(long baseGravable);
}
