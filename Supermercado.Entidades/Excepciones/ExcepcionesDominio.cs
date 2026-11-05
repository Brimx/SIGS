using Supermercado.Entidades.Seguridad;

namespace Supermercado.Entidades.Excepciones;

public class AccesoDenegadoException(Permiso permiso)
    : Exception($"No tienes permiso para esta acción ({permiso}).");

public class CredencialesInvalidasException()
    : Exception("Usuario o clave incorrectos.");

public class CuentaBloqueadaException()
    : Exception("La cuenta está bloqueada. Contacta a un administrador.");

public class DebeCambiarClaveException()
    : Exception("Debes cambiar la clave antes de continuar.");

public class ParametroFaltanteException(string clave)
    : Exception($"Falta configurar el parámetro '{clave}'.");

public class ServicioNoConfiguradoException(string servicio)
    : Exception($"El servicio '{servicio}' no está configurado.");

public class SesionNoIniciadaException()
    : Exception("No hay sesión iniciada.");

public class StockInsuficienteException(string detalle)
    : Exception($"Stock insuficiente: {detalle}");

public class TurnoInvalidoException(string mensaje) : Exception(mensaje);

public class ValidacionException(string mensaje) : Exception(mensaje);

public class VentaInvalidaException(string mensaje) : Exception(mensaje);
