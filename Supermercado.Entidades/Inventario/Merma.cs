using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Inventario;

public class Merma : EntidadBase
{
    public int ProductoId { get; set; }
    public int LoteId { get; set; }
    public int Cantidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
