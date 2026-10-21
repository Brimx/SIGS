using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Ventas;

public class Venta : EntidadBase
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public EstadoVenta Estado { get; set; } = EstadoVenta.Abierta;
    public List<DetalleVenta> Detalles { get; set; } = [];
    public List<Pago> Pagos { get; set; } = [];

    public long Total => Detalles.Sum(d => d.Subtotal);
    public long TotalPagado => Pagos.Sum(p => p.Monto);
    public long Cambio => Math.Max(0, TotalPagado - Total);

    private long MontoEfectivo
        => Pagos.OfType<PagoEfectivo>().Sum(p => p.Monto);

    public void AgregarProducto(Producto producto, int cantidad)
    {
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
        if (Cambio > MontoEfectivo)
            throw new VentaInvalidaException("Solo se da cambio en efectivo.");

        Estado = EstadoVenta.Completada;
    }

    public void Anular()
    {
        if (Estado != EstadoVenta.Completada)
            throw new VentaInvalidaException("Solo se anulan ventas cerradas.");

        Estado = EstadoVenta.Anulada;
    }

    private void ExigirAbierta()
    {
        if (Estado != EstadoVenta.Abierta)
            throw new VentaInvalidaException("La venta ya no está abierta.");
    }
}
