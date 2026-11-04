using Supermercado.Entidades.Base;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Ventas;

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
