using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Personal;

public class ServicioEmpleados(
    IRepositorio<Empleado> empleados,
    IRepositorio<Rol> roles,
    IUsuarioRepositorio usuarios,
    IVerificadorCorreo verificador,
    IHasheadorClave hasheador,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioEmpleados
{
    public async Task CrearAsync(Empleado empleado)
    {
        sesion.Exigir(Permiso.GestionarPersonal);
        Validar(empleado);

        var todos = await empleados.ObtenerTodosAsync();
        if (todos.Any(e => e.Documento == empleado.Documento))
            throw new ValidacionException("Ya existe ese documento.");

        if (empleado.FechaIngreso == default)
            empleado.FechaIngreso = reloj.HoyLocal();

        empleado.CorreoVerificado = await VerificarCorreoAsync(empleado.Correo);
        await empleados.AgregarAsync(empleado);
        await auditoria.RegistrarAsync(
            AccionAuditoria.EmpleadoCreado,
            nameof(Empleado),
            detalle: empleado.Documento);
        await trabajo.GuardarAsync();
    }

    public async Task EditarAsync(Empleado empleado)
    {
        sesion.Exigir(Permiso.GestionarPersonal);
        Validar(empleado);

        var existente = await BuscarAsync(empleado.Id);

        if (existente.RolId != empleado.RolId && !sesion.Tiene(Permiso.GestionarRoles))
            throw new AccesoDenegadoException(Permiso.GestionarRoles);

        existente.Nombres = empleado.Nombres;
        existente.Apellidos = empleado.Apellidos;
        existente.Documento = empleado.Documento;
        existente.Correo = empleado.Correo;
        existente.CargoId = empleado.CargoId;
        existente.FechaIngreso = empleado.FechaIngreso;
        existente.SalarioBase = empleado.SalarioBase;
        existente.Activo = empleado.Activo;
        existente.CorreoVerificado = empleado.CorreoVerificado;

        if (sesion.Tiene(Permiso.GestionarRoles))
            existente.RolId = empleado.RolId;

        empleados.Actualizar(existente);
        await trabajo.GuardarAsync();
    }

    public async Task CambiarRolAsync(int empleadoId, int rolId)
    {
        sesion.Exigir(Permiso.GestionarRoles);

        var empleado = await BuscarAsync(empleadoId);
        var rol = await roles.ObtenerPorIdAsync(rolId)
            ?? throw new ValidacionException("El rol no existe.");
        var anterior = empleado.RolId;

        empleado.RolId = rol.Id;
        empleados.Actualizar(empleado);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CambioRol,
            nameof(Empleado),
            empleadoId,
            $"Rol {anterior} a {rol.Id}");
        await trabajo.GuardarAsync();
    }

    public async Task DesactivarAsync(int empleadoId)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        var empleado = await BuscarAsync(empleadoId);

        empleado.Desactivar();
        empleados.Actualizar(empleado);
        await auditoria.RegistrarAsync(
            AccionAuditoria.EmpleadoDesactivado, nameof(Empleado), empleadoId);
        await trabajo.GuardarAsync();
    }

    public async Task CrearUsuarioAsync(
        int empleadoId,
        string nombreUsuario,
        string claveTemporal)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ValidacionException("Falta el nombre de usuario.");
        if (claveTemporal.Length < ServicioAutenticacion.LongitudMinimaClave)
            throw new ValidacionException("La clave temporal es muy corta.");

        var existente = await usuarios.ObtenerPorNombreAsync(nombreUsuario);
        if (existente is not null)
            throw new ValidacionException("Ya existe ese nombre de usuario.");

        var todos = await usuarios.ObtenerTodosAsync();
        if (todos.Any(u => u.EmpleadoId == empleadoId))
            throw new ValidacionException("El empleado ya tiene un usuario.");

        await BuscarAsync(empleadoId);
        await usuarios.AgregarAsync(new Usuario
        {
            NombreUsuario = nombreUsuario,
            HashClave = hasheador.Hashear(claveTemporal),
            DebeCambiarClave = true,
            EmpleadoId = empleadoId
        });
        await auditoria.RegistrarAsync(
            AccionAuditoria.UsuarioCreado,
            nameof(Usuario),
            detalle: nombreUsuario);
        await trabajo.GuardarAsync();
    }

    // La verificación es complementaria: si falla, queda "no verificado".
    private async Task<bool> VerificarCorreoAsync(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return false;

        try
        {
            return await verificador.VerificarAsync(correo);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<Empleado> BuscarAsync(int empleadoId)
        => await empleados.ObtenerPorIdAsync(empleadoId)
            ?? throw new ValidacionException("El empleado no existe.");

    private static void Validar(Empleado empleado)
    {
        if (string.IsNullOrWhiteSpace(empleado.Documento)
            || string.IsNullOrWhiteSpace(empleado.Nombres))
            throw new ValidacionException("Faltan documento o nombres.");
        if (empleado.SalarioBase < 0)
            throw new ValidacionException("El salario no puede ser negativo.");
    }
}
