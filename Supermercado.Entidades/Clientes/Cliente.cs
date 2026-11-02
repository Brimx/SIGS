using Supermercado.Entidades.Base;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Entidades.Clientes;

// Cliente registrado en el programa de puntos. Las ventas sin cliente
// son a «consumidor final».
public class Cliente : EntidadBase
{
    public string Documento { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int PuntosAcumulados { get; set; }
    public bool AutorizaTratamientoDatos { get; set; } = true;
    public DateTime? FechaAutorizacion { get; set; }
    public string? TerminalAutorizacion { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    public void AcumularPuntos(int puntos)
    {
        if (puntos < 0)
            throw new ValidacionException("Los puntos no pueden ser negativos.");

        PuntosAcumulados += puntos;
    }

    public void RedimirPuntos(int puntos)
    {
        if (puntos <= 0)
            throw new ValidacionException("Los puntos a redimir deben ser positivos.");
        if (puntos > PuntosAcumulados)
            throw new ValidacionException("El cliente no tiene puntos suficientes.");

        PuntosAcumulados -= puntos;
    }

    // Anulación de venta: quita lo ganado y devuelve lo redimido.
    // El saldo nunca baja de cero aunque el cliente ya haya gastado los puntos.
    public void RevertirVenta(int ganados, int redimidos)
        => PuntosAcumulados = Math.Max(0, PuntosAcumulados - ganados) + redimidos;

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
