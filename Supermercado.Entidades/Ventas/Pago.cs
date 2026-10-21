using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Ventas;

public abstract class Pago : EntidadBase
{
    public int VentaId { get; set; }
    public long Monto { get; set; }

    public abstract string Metodo { get; }

    public virtual void Validar()
    {
        if (Monto <= 0)
            throw new VentaInvalidaException("El monto debe ser positivo.");
    }
}
