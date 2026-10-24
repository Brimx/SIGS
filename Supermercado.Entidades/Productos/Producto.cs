using Supermercado.Entidades.Base;
using Supermercado.Entidades.Finanzas;

namespace Supermercado.Entidades.Productos;

public abstract class Producto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;
    public long PrecioVenta { get; set; }
    public int StockMinimoEstante { get; set; }
    public bool Activo { get; set; } = true;

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public int ImpuestoId { get; set; }
    public Impuesto? Impuesto { get; set; }

    public abstract long CalcularSubtotal(int cantidad);

    public virtual string UnidadMedida => "und";
}
