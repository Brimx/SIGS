using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Ventas;

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
            .Sum(v => v.EfectivoNeto);

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
