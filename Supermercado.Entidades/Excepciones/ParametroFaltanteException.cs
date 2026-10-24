namespace Supermercado.Entidades.Excepciones;

public class ParametroFaltanteException(string clave)
    : Exception($"Falta configurar el parámetro '{clave}'.");
