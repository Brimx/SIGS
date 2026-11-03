namespace Supermercado.GUI.Servicios.Preferencias;

/// <summary>
/// Preferencias visuales de la máquina. No son configuración de negocio: no pasan por la BD ni por auditoría.
/// </summary>
public interface IServicioPreferencias
{
    /// <summary>Clave de tema guardada; cae a <c>Claro</c> si no existe o es inválida.</summary>
    string LeerTema();

    void GuardarTema(string clave);
}
