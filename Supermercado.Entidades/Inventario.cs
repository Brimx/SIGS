using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Inventario
{
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

public enum EstadoReabastecimiento
{
    Pendiente,
    Atendido,
    Cancelado
}

public record KardexMovimiento(
    int MovimientoId,
    DateTime Fecha,
    TipoMovimiento Tipo,
    int LoteId,
    string NumeroLote,
    int Cantidad,
    int SaldoPosterior,
    string Motivo);

public class Merma : EntidadBase
{
    public int ProductoId { get; set; }
    public int LoteId { get; set; }
    public int Cantidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class MovimientoInventario : EntidadBase
{
    public int ProductoId { get; set; }
    public int LoteId { get; set; }
    public Lote? Lote { get; set; }
    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class StockBodega : StockLote
{
}

public record StockConsolidado(
    int ProductoId,
    string NombreProducto,
    string CodigoBarras,
    int LoteId,
    string NumeroLote,
    DateOnly FechaVencimiento,
    int CantidadEstante,
    int CantidadBodega,
    int TotalStock);

public sealed class StockEstante : StockLote
{
}

public abstract class StockLote : EntidadBase
{
    public int ProductoId { get; set; }
    public int LoteId { get; set; }
    public Lote Lote { get; set; } = null!;
    public int Cantidad { get; private set; }

    public void Aumentar(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        Cantidad += cantidad;
    }

    public void Disminuir(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        if (cantidad > Cantidad)
            throw new StockInsuficienteException($"lote {LoteId}");

        Cantidad -= cantidad;
    }
}

public enum TipoMovimiento
{
    Ingreso,
    Traslado,
    Venta,
    Devolucion,
    Merma,
    Ajuste
}

public enum UbicacionStock
{
    Estante,
    Bodega
}
}
