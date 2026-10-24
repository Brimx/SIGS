using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Finanzas;

// El precio de venta ya incluye IVA: se extrae, no se suma.
public sealed class Iva : Impuesto
{
    public override long Calcular(long baseGravable)
        => Dinero.DividirRedondeando(
            baseGravable * PuntosBasicos,
            Dinero.PuntosBasicosCien + PuntosBasicos);
}
