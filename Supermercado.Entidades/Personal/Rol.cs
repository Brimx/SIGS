using Supermercado.Entidades.Base;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Entidades.Personal;

public class Rol : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public Permiso Permisos { get; set; }

    public bool Tiene(Permiso permiso)
        => (Permisos & permiso) == permiso;
}
