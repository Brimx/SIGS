using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Productos;

public class Proveedor : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}
