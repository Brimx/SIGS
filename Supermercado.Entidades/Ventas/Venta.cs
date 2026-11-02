using Supermercado.Entidades.Base;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Ventas;

public class Venta : EntidadBase
{
    public DateTime Fecha { get; set; }
    public int TurnoCajaId { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.Abierta;
    public string MotivoAnulacion { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int PuntosGanados { get; set; }
    public int PuntosRedimidos { get; set; }
    public string? ClaveIdempotencia { get; set; }

    // Campos legales de adquirente (Ley 1581 / Habeas Data y DIAN)
    public const string DocumentoConsumidorFinal = "222222222222";
    public const string NombreConsumidorFinal = "CONSUMIDOR FINAL";
    public string DocumentoAdquirente { get; set; } = DocumentoConsumidorFinal;
    public string NombreAdquirente { get; set; } = NombreConsumidorFinal;

    // Campos DIAN (Resolución 000165 de 2023 - Documento Equivalente POS)
    public string? Cude { get; set; }
    public string? CadenaQr { get; set; }
    public string? PrefijoDian { get; set; }
    public long ConsecutivoDian { get; set; }
    public DateTime? FechaValidacionDian { get; set; }
    public string? XmlUbl { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = [];
    public List<Pago> Pagos { get; set; } = [];

    public long Total => Detalles.Sum(d => d.Subtotal);
    public long TotalIva => Detalles.Sum(d => d.Iva);
    public long TotalPagado => Pagos.Sum(p => p.Monto);
    public long Cambio => Math.Max(0, TotalPagado - Total);

    public long EfectivoRecibido
        => Pagos.OfType<PagoEfectivo>().Sum(p => p.Monto);

    public long EfectivoNeto => EfectivoRecibido - Cambio;

    public void AgregarProducto(Producto producto, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(producto);
        ExigirAbierta();
        Detalles.Add(DetalleVenta.Crear(producto, cantidad));
    }

    public void AgregarPago(Pago pago)
    {
        ExigirAbierta();
        pago.Validar();
        Pagos.Add(pago);
    }

    public void Cerrar()
    {
        ExigirAbierta();

        if (Detalles.Count == 0)
            throw new VentaInvalidaException("La venta está vacía.");
        if (TotalPagado < Total)
            throw new VentaInvalidaException("El pago no alcanza.");
        if (Cambio > EfectivoRecibido)
            throw new VentaInvalidaException("Solo se da cambio en efectivo.");

        Estado = EstadoVenta.Completada;
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoVenta.Anulada)
            throw new VentaInvalidaException("La venta ya está anulada.");
        if (Estado != EstadoVenta.Completada)
            throw new VentaInvalidaException("Solo se anulan ventas cerradas.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new VentaInvalidaException("Falta el motivo de anulación.");

        Estado = EstadoVenta.Anulada;
        MotivoAnulacion = motivo;
    }

    private void ExigirAbierta()
    {
        if (Estado != EstadoVenta.Abierta)
            throw new VentaInvalidaException("La venta ya no está abierta.");
    }
}
