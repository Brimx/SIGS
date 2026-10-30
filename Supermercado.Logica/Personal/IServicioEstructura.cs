using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Personal;

public interface IServicioEstructura
{
    Task<IReadOnlyList<Caja>> ObtenerCajasAsync();

    Task<Caja> CrearCajaAsync(string nombre);

    Task EditarCajaAsync(int id, string nuevoNombre, bool activa);

    Task<IReadOnlyList<Cargo>> ObtenerCargosAsync();

    Task<Cargo> CrearCargoAsync(string nombre);

    Task EditarCargoAsync(int id, string nuevoNombre);

    Task<IReadOnlyList<Rol>> ObtenerRolesAsync();

    Task<Rol> CrearRolAsync(string nombre, Permiso permisos);

    Task EditarPermisosRolAsync(int rolId, Permiso nuevosPermisos);
}
