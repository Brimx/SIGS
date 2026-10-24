using Supermercado.Entidades.Seguridad;

namespace Supermercado.Entidades.Excepciones;

public class AccesoDenegadoException(Permiso permiso)
    : Exception($"No tienes permiso para esta acción ({permiso}).");
