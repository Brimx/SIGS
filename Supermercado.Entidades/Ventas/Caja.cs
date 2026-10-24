using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Ventas;

public class Caja : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
}
