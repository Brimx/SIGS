namespace Supermercado.Logica.Seguridad;

public interface IServicioInicializacion
{
    // La clave inicial la entrega quien arranca la app; no vive en el código.
    Task InicializarAsync(string claveAdminInicial);
}
