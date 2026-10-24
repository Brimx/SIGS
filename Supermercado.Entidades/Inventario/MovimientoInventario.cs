using Supermercado.Entidades.Base;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Entidades.Inventario;

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
