using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Ventas;

public class PagoTransferencia : Pago
{
    public string Referencia { get; set; } = string.Empty;

    public override string Metodo => "Transferencia";

    public override void Validar()
    {
        base.Validar();

        if (string.IsNullOrWhiteSpace(Referencia))
            throw new VentaInvalidaException("Falta la referencia.");
    }
}
