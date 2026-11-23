using Supermercado.Entidades.Base;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Entidades.Personal
{
public class Usuario : EntidadBase
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string HashClave { get; set; } = string.Empty;
    public string? HashPin { get; set; }
    public bool DebeCambiarClave { get; set; }
    public bool Bloqueado { get; set; }
    public int IntentosFallidos { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public void RegistrarFallo(int maximoIntentos)
    {
        if (maximoIntentos <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maximoIntentos),
                "El máximo de intentos debe ser positivo.");

        IntentosFallidos++;

        if (IntentosFallidos >= maximoIntentos)
            Bloqueado = true;
    }

    public void RegistrarExito() => IntentosFallidos = 0;

    public void Desbloquear()
    {
        Bloqueado = false;
        IntentosFallidos = 0;
    }

    public void CambiarClave(string nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            throw new ArgumentException(
                "El hash de la clave no puede estar vacío.",
                nameof(nuevoHash));

        HashClave = nuevoHash;
        DebeCambiarClave = false;
    }
}

public class Cargo : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
}

public class Empleado : PersonaBase
{
    public bool CorreoVerificado { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public long SalarioBase { get; set; }

    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }
    public int RolId { get; set; }
    public Rol? Rol { get; set; }

    public void Desactivar() => Activo = false;
}

public static class NombresRol
{
    public const string Admin = "Admin";
    public const string Gerente = "Gerente";
    public const string Cajero = "Cajero";
    public const string Bodeguero = "Bodeguero";
}

public class Rol : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public Permiso Permisos { get; set; }

    public bool Tiene(Permiso permiso)
        => (Permisos & permiso) == permiso;
}
}
