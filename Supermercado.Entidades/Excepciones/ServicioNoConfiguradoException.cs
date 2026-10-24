namespace Supermercado.Entidades.Excepciones;

public class ServicioNoConfiguradoException(string servicio)
    : Exception($"El servicio '{servicio}' no está configurado.");
