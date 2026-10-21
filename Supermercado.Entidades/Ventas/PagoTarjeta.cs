using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Ventas;

public class PagoTarjeta : Pago
{
    public string UltimosDigitos { get; set; } = string.Empty;

    public override string Metodo => "Tarjeta";

    public override void Validar()
    {
        base.Validar();

        if (UltimosDigitos.Length != 4)
            throw new VentaInvalidaException("Faltan los 4 últimos dígitos.");
    }
}
