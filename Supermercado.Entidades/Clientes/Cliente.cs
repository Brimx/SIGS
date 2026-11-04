using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Clientes;

// Cliente registrado en el sistema. Las ventas sin cliente
// son a «consumidor final».
public class Cliente : EntidadBase
{
    public string Documento { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool AutorizaTratamientoDatos { get; set; } = true;
    public DateTime? FechaAutorizacion { get; set; }
    public string? TerminalAutorizacion { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public void AutorizarHabeasData(string terminal, DateTime ahora)
    {
        AutorizaTratamientoDatos = true;
        TerminalAutorizacion = terminal;
        FechaAutorizacion = ahora;
    }

    public void RevocarHabeasData()
    {
        AutorizaTratamientoDatos = false;
    }

}
