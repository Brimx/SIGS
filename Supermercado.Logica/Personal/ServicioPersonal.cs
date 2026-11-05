using System.Security.Cryptography;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Logica.Personal;

public class ServicioPersonal(
    IRepositorio<Empleado> empleados,
    IRepositorio<Cargo> cargos,
    IRepositorio<Rol> roles,
    IUsuarioRepositorio usuarios,
    IRepositorio<Caja> cajas,
    IVerificadorCorreo verificador,
    IHasheadorClave hasheador,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioPersonal, IServicioEmpleados, IServicioEstructura, IServicioUsuarios
{
    private const string CaracteresClave =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    // --- Empleados ---

    public async Task CrearAsync(Empleado empleado)
    {
        sesion.Exigir(Permiso.GestionarPersonal);
        Validar(empleado);
        await ExigirAlcanceDelRolAsync(empleado.RolId);

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

        var existente = await BuscarEmpleadoAsync(empleado.Id);

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

        var empleado = await BuscarEmpleadoAsync(empleadoId);
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

        var empleado = await BuscarEmpleadoAsync(empleadoId);

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

        var empleado = await BuscarEmpleadoAsync(empleadoId);
        await ExigirAlcanceDelRolAsync(empleado.RolId);
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

    // --- Estructura (Cajas, Cargos, Roles) ---

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

        var existente = roles is IRolRepositorio rolRepo
            ? await rolRepo.ObtenerPorNombreAsync(nombre.Trim())
            : (await roles.ObtenerTodosAsync()).FirstOrDefault(r => r.Nombre == nombre.Trim());
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

    public async Task EditarPermisosRolAsync(int rolId, Permiso nuevosPermisos)
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

    // --- Usuarios ---

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
    {
        sesion.Exigir(Permiso.GestionarPersonal);
        return await usuarios.ObtenerTodosAsync();
    }

    public async Task DesbloquearAsync(int usuarioId)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId)
            ?? throw new ValidacionException("El usuario no existe.");

        usuario.Desbloquear();
        usuarios.Actualizar(usuario);

        await auditoria.RegistrarAsync(
            AccionAuditoria.UsuarioDesbloqueado,
            nameof(Usuario),
            usuario.Id,
            $"Usuario {usuario.NombreUsuario} desbloqueado");
        await trabajo.GuardarAsync();
    }

    public async Task<string> RestablecerClaveTemporalAsync(int usuarioId)
    {
        sesion.Exigir(Permiso.GestionarRoles);

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId)
            ?? throw new ValidacionException("El usuario no existe.");

        var claveTemporal = GenerarClaveTemporal();
        usuario.HashClave = hasheador.Hashear(claveTemporal);
        usuario.DebeCambiarClave = true;
        usuario.Desbloquear();

        usuarios.Actualizar(usuario);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ClaveRestablecida,
            nameof(Usuario),
            usuario.Id,
            $"Clave restablecida para {usuario.NombreUsuario}");
        await trabajo.GuardarAsync();

        return claveTemporal;
    }

    // --- Auxiliares ---

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

    private async Task<Empleado> BuscarEmpleadoAsync(int empleadoId)
        => await empleados.ObtenerPorIdAsync(empleadoId)
            ?? throw new ValidacionException("El empleado no existe.");

    private async Task ExigirAlcanceDelRolAsync(int rolId)
    {
        var rol = await roles.ObtenerPorIdAsync(rolId)
            ?? throw new ValidacionException("El rol no existe.");
        if (sesion.Tiene(Permiso.GestionarRoles))
            return;

        var permisosActor = sesion.Usuario?.Empleado?.Rol?.Permisos ?? Permiso.Ninguno;
        if ((rol.Permisos & ~permisosActor) != Permiso.Ninguno)
            throw new AccesoDenegadoException(Permiso.GestionarRoles);
    }

    private static void Validar(Empleado empleado)
    {
        empleado.Documento = Validador.ExigirDocumento(empleado.Documento);
        empleado.Nombres = Validador.ExigirNombre(empleado.Nombres, "el nombre");
        empleado.Apellidos = Validador.ExigirNombreOpcional(empleado.Apellidos, "el apellido");
        empleado.Correo = Validador.ExigirCorreoOpcional(empleado.Correo);
        if (empleado.SalarioBase < 0)
            throw new ValidacionException("El salario no puede ser negativo.");
    }

    private static string GenerarClaveTemporal(int longitud = 8)
    {
        var chars = new char[longitud];
        for (var i = 0; i < longitud; i++)
        {
            var indice = RandomNumberGenerator.GetInt32(CaracteresClave.Length);
            chars[i] = CaracteresClave[indice];
        }
        return new string(chars);
    }
}

// Adaptadores para pruebas existentes
public class ServicioEmpleados(
    IRepositorio<Empleado> empleados,
    IRepositorio<Rol> roles,
    IUsuarioRepositorio usuarios,
    IVerificadorCorreo verificador,
    IHasheadorClave hasheador,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : ServicioPersonal(
        empleados,
        null!,
        roles,
        usuarios,
        null!,
        verificador,
        hasheador,
        auditoria,
        sesion,
        trabajo,
        reloj);

public class ServicioEstructura(
    IRepositorio<Caja> cajas,
    IRepositorio<Cargo> cargos,
    IRolRepositorio roles,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : ServicioPersonal(
        null!,
        cargos,
        roles,
        null!,
        cajas,
        null!,
        null!,
        auditoria,
        sesion,
        trabajo,
        TimeProvider.System);
