using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

public class ClienteTrmNoConfigurado : ITrmClient
{
    public Task<Trm> ObtenerAsync(CancellationToken ct)
        => throw new ServicioNoConfiguradoException("TRM");
}
