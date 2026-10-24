using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Inventario;

public class Reabastecimiento : EntidadBase
{
    public int ProductoId { get; set; }
    public int CantidadSolicitada { get; set; }
    public EstadoReabastecimiento Estado { get; set; }
        = EstadoReabastecimiento.Pendiente;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAtencion { get; set; }

    public void Atender(DateTime ahora)
    {
        if (Estado != EstadoReabastecimiento.Pendiente)
            throw new ValidacionException(
                "La solicitud de reabastecimiento ya se cerró.");

        Estado = EstadoReabastecimiento.Atendido;
        FechaAtencion = ahora;
    }

    public void Cancelar()
    {
        if (Estado != EstadoReabastecimiento.Pendiente)
            throw new ValidacionException(
                "La solicitud de reabastecimiento ya se cerró.");

        Estado = EstadoReabastecimiento.Cancelado;
        FechaAtencion = null;
    }
}
