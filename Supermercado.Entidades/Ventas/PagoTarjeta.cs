using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Ventas;

public sealed class PagoTarjeta : Pago
{
    public const int DigitosRequeridos = 4;

    public string UltimosDigitos { get; set; } = string.Empty;

    public override string Metodo => "Tarjeta";

    public override void Validar()
    {
        base.Validar();

        if (UltimosDigitos.Length != DigitosRequeridos)
            throw new VentaInvalidaException("Faltan los últimos dígitos.");
    }
}
