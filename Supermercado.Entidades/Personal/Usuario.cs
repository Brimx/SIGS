using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Personal;

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
