using Microsoft.Extensions.Logging;
using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Comunes;

public class TraductorExcepciones(
    ILogger<TraductorExcepciones>? logger = null) : ITraductorExcepciones
{
    public string Traducir(Exception excepcion)
    {
        ArgumentNullException.ThrowIfNull(excepcion);

        return excepcion switch
        {
            ValidacionException
                or VentaInvalidaException
                or StockInsuficienteException
                or TurnoInvalidoException
                or AccesoDenegadoException
                or CredencialesInvalidasException
                or CuentaBloqueadaException
                or SesionNoIniciadaException
                or DebeCambiarClaveException
                or ParametroFaltanteException
                or ServicioNoConfiguradoException => excepcion.Message,
            _ => RegistrarYDevolverGenerico(excepcion)
        };
    }

    private string RegistrarYDevolverGenerico(Exception excepcion)
    {
        logger?.LogError(
            excepcion,
            "Error inesperado no controlado en la aplicación.");
        return "Ocurrió un error inesperado. Intente de nuevo o contacte "
            + "al administrador.";
    }
}
