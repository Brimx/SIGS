using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Finanzas;

public class Gasto : EntidadBase
{
    public string Descripcion { get; set; } = string.Empty;
    public long Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int RegistradoPorEmpleadoId { get; set; }
}
