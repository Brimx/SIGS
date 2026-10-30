using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Personal;

public class ServicioEstructura(
    IRepositorio<Caja> cajas,
    IRepositorio<Cargo> cargos,
    IRolRepositorio roles,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioEstructura
{
    public Task<IReadOnlyList<Caja>> ObtenerCajasAsync()
        => cajas.ObtenerTodosAsync();

    public async Task<Caja> CrearCajaAsync(string nombre)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre de la caja.");

        var caja = new Caja { Nombre = nombre.Trim() };
        await cajas.AgregarAsync(caja);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CajaCreada,
            nameof(Caja),
            detalle: $"Caja creada: {caja.Nombre}");
        await trabajo.GuardarAsync();
        return caja;
    }

    public async Task EditarCajaAsync(int id, string nuevoNombre, bool activa)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre de la caja.");

        var caja = await cajas.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("La caja no existe.");

        caja.Nombre = nuevoNombre.Trim();
        caja.Activa = activa;

        cajas.Actualizar(caja);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CajaModificada,
            nameof(Caja),
            caja.Id,
            $"Caja modificada: {caja.Nombre} (Activa: {activa})");
        await trabajo.GuardarAsync();
    }

    public Task<IReadOnlyList<Cargo>> ObtenerCargosAsync()
        => cargos.ObtenerTodosAsync();

    public async Task<Cargo> CrearCargoAsync(string nombre)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre del cargo.");

        var cargo = new Cargo { Nombre = nombre.Trim() };
        await cargos.AgregarAsync(cargo);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CargoCreado,
            nameof(Cargo),
            detalle: $"Cargo creado: {cargo.Nombre}");
        await trabajo.GuardarAsync();
        return cargo;
    }

    public async Task EditarCargoAsync(int id, string nuevoNombre)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new ValidacionException("Falta el nombre del cargo.");

        var cargo = await cargos.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("El cargo no existe.");

        cargo.Nombre = nuevoNombre.Trim();
        cargos.Actualizar(cargo);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CargoModificado,
            nameof(Cargo),
            cargo.Id,
            $"Cargo renombrado a {cargo.Nombre}");
        await trabajo.GuardarAsync();
    }

    public Task<IReadOnlyList<Rol>> ObtenerRolesAsync()
        => roles.ObtenerTodosAsync();

    public async Task<Rol> CrearRolAsync(string nombre, Permiso permisos)
    {
        sesion.Exigir(Permiso.GestionarRoles);

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidacionException("Falta el nombre del rol.");

        var existente = await roles.ObtenerPorNombreAsync(nombre.Trim());
        if (existente is not null)
            throw new ValidacionException("Ya existe un rol con ese nombre.");

        var rol = new Rol
        {
            Nombre = nombre.Trim(),
            Permisos = permisos
        };

        await roles.AgregarAsync(rol);
        await auditoria.RegistrarAsync(
            AccionAuditoria.RolCreado,
            nameof(Rol),
            detalle: $"Rol creado: {rol.Nombre} con permisos {rol.Permisos}");
        await trabajo.GuardarAsync();
        return rol;
    }

    public async Task EditarPermisosRolAsync(
        int rolId,
        Permiso nuevosPermisos)
    {
        sesion.Exigir(Permiso.GestionarRoles);

        var rol = await roles.ObtenerPorIdAsync(rolId)
            ?? throw new ValidacionException("El rol no existe.");

        if (rol.Nombre == NombresRol.Admin
            && !nuevosPermisos.HasFlag(Permiso.GestionarRoles))
        {
            throw new ValidacionException(
                "El rol Admin no puede perder el permiso de GestionarRoles.");
        }

        rol.Permisos = nuevosPermisos;
        roles.Actualizar(rol);
        await auditoria.RegistrarAsync(
            AccionAuditoria.RolModificado,
            nameof(Rol),
            rol.Id,
            $"Permisos de rol {rol.Nombre} actualizados a {nuevosPermisos}");
        await trabajo.GuardarAsync();
    }
}
