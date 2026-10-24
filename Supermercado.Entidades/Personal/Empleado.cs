using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Personal;

public class Empleado : EntidadBase
{
    public string Documento { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool CorreoVerificado { get; set; }
    public bool Activo { get; set; } = true;
    public DateOnly FechaIngreso { get; set; }
    public long SalarioBase { get; set; }

    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }
    public int RolId { get; set; }
    public Rol? Rol { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public void Desactivar() => Activo = false;
}
