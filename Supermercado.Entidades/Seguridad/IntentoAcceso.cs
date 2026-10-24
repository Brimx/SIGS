using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Seguridad;

public class IntentoAcceso : EntidadBase
{
    public string NombreUsuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool Exitoso { get; set; }
    public bool UsoPin { get; set; }
}
