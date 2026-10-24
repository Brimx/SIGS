using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Finanzas;

public sealed class Retencion : Impuesto
{
    public long BaseMinima { get; set; }

    public override long Calcular(long baseGravable)
        => baseGravable < BaseMinima
            ? 0
            : Dinero.Porcentaje(baseGravable, PuntosBasicos);
}
