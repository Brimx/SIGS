using Supermercado.Entidades.Base;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;

namespace Supermercado.Entidades.Ventas
{
public class Venta : EntidadBase
{
    public DateTime Fecha { get; set; }
    public int TurnoCajaId { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.Abierta;
    public string MotivoAnulacion { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public string? ClaveIdempotencia { get; set; }

    // Campos de adquirente (Ley 1581 / Habeas Data)
    public const string DocumentoConsumidorFinal = "222222222222";
    public const string NombreConsumidorFinal = "CONSUMIDOR FINAL";
    public string DocumentoAdquirente { get; set; } = DocumentoConsumidorFinal;
    public string NombreAdquirente { get; set; } = NombreConsumidorFinal;

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

public enum EstadoVenta
{
    Abierta,
    Completada,
    Anulada
}

public class DetalleVenta : EntidadBase
{
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public long PrecioUnitario { get; set; }
    public long Subtotal { get; set; }
    public int PuntosBasicosIva { get; set; }
    public long Iva { get; set; }

    public static DetalleVenta Crear(Producto producto, int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);

        var impuesto = producto.Impuesto
            ?? throw new ValidacionException(
                "El producto no trae su impuesto cargado.");
        var subtotal = producto.CalcularSubtotal(cantidad);

        return new DetalleVenta
        {
            ProductoId = producto.Id,
            Producto = producto,
            Cantidad = cantidad,
            PrecioUnitario = producto.PrecioVenta,
            Subtotal = subtotal,
            PuntosBasicosIva = impuesto.PuntosBasicos,
            Iva = impuesto.Calcular(subtotal)
        };
    }
}

public class VentaEnEspera : EntidadBase
{
    public int CajaId { get; set; }
    public Caja? Caja { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public string IdentificadorCliente { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public string LineasJson { get; set; } = string.Empty;
    public long TotalEstimado { get; set; }
    public string? Nota { get; set; }
    public bool Recuperada { get; set; }
}

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

public sealed class PagoEfectivo : Pago
{
    public override string Metodo => "Efectivo";
}

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

public sealed class PagoTransferencia : Pago
{
    public string Referencia { get; set; } = string.Empty;

    public override string Metodo => "Transferencia";

    public override void Validar()
    {
        base.Validar();

        if (string.IsNullOrWhiteSpace(Referencia))
            throw new VentaInvalidaException("Falta la referencia.");
    }
}
}
