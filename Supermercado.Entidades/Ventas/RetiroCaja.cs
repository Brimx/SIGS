using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Ventas;

public class RetiroCaja : EntidadBase
{
    public int TurnoCajaId { get; set; }
    public TurnoCaja? TurnoCaja { get; set; }
    public long Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public int EmpleadoAutorizaId { get; set; }
    public Empleado? EmpleadoAutoriza { get; set; }
    public DateTime Fecha { get; set; }

    public void Validar()
    {
        if (Monto <= 0)
            throw new ValidacionException("El monto a retirar debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new ValidacionException("Debe ingresar un motivo para el retiro de caja.");
    }
}
