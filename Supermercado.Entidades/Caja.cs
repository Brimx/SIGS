using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Ventas
{
public class TurnoCaja : EntidadBase
{
    public int CajaId { get; set; }
    public Caja? Caja { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public long EfectivoInicial { get; set; }
    public long? EfectivoEsperado { get; set; }
    public long? EfectivoContado { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Abierto;
    public ICollection<RetiroCaja> Retiros { get; set; } = new List<RetiroCaja>();

    public long TotalRetiros => Retiros.Sum(r => r.Monto);
    public long? Diferencia => EfectivoContado - EfectivoEsperado;

    public static TurnoCaja Abrir(
        int cajaId,
        int empleadoId,
        long efectivoInicial,
        DateTime ahora)
    {
        if (efectivoInicial < 0)
            throw new TurnoInvalidoException("Efectivo inicial negativo.");

        return new TurnoCaja
        {
            CajaId = cajaId,
            EmpleadoId = empleadoId,
            EfectivoInicial = efectivoInicial,
            FechaApertura = ahora
        };
    }

    public long CalcularEfectivoEsperado(IEnumerable<Venta> ventas)
        => EfectivoInicial + ventas
            .Where(v => v.Estado == EstadoVenta.Completada)
            .Sum(v => v.EfectivoNeto)
            - ventas
            .Where(v => v.Estado == EstadoVenta.Anulada)
            .Sum(v => v.EfectivoNeto)
            - TotalRetiros;

    public void Cerrar(DateTime ahora, long esperado, long contado)
    {
        if (Estado != EstadoTurno.Abierto)
            throw new TurnoInvalidoException("El turno ya está cerrado.");
        if (contado < 0)
            throw new TurnoInvalidoException("Efectivo contado negativo.");

        EfectivoEsperado = esperado;
        EfectivoContado = contado;
        FechaCierre = ahora;
        Estado = EstadoTurno.Cerrado;
    }
}

public class Caja : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
}

public enum EstadoTurno
{
    Abierto,
    Cerrado
}

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
}
