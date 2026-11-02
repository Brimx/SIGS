using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Ventas;

// Redención de puntos del programa de fidelización. Monto = Puntos × valor
// del punto vigente al momento de la venta.
public sealed class PagoPuntos : Pago
{
    public int Puntos { get; set; }

    public override string Metodo => "Puntos";

    public override void Validar()
    {
        base.Validar();

        if (Puntos <= 0)
            throw new VentaInvalidaException("Faltan los puntos a redimir.");
    }
}
