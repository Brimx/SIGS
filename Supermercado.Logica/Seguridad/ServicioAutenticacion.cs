using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Seguridad;

public class ServicioAutenticacion(
    IUsuarioRepositorio usuarios,
    IRepositorio<IntentoAcceso> intentos,
    IServicioAuditoria auditoria,
    IServicioParametros parametros,
    IHasheadorClave hasheador,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioAutenticacion
{
    public const int LongitudMinimaClave = 8;
    public const int LongitudMinimaPin = 4;

    public Task IniciarSesionAsync(string nombreUsuario, string clave)
        => AutenticarAsync(nombreUsuario, clave, usaPin: false);

    public Task IniciarSesionConPinAsync(string nombreUsuario, string pin)
        => AutenticarAsync(nombreUsuario, pin, usaPin: true);

    public async Task CambiarClaveAsync(string claveActual, string claveNueva)
    {
        var usuario = UsuarioEnSesion();

        if (!hasheador.Verificar(claveActual, usuario.HashClave))
            throw new CredencialesInvalidasException();
        if (claveNueva.Length < LongitudMinimaClave)
            throw new ValidacionException("La clave nueva es muy corta.");

        usuario.CambiarClave(hasheador.Hashear(claveNueva));
        usuarios.Actualizar(usuario);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CambioClave, nameof(Usuario), usuario.Id);
        await trabajo.GuardarAsync();
    }

    public async Task AsignarPinAsync(string pin)
    {
        var usuario = UsuarioEnSesion();

        if (pin.Length < LongitudMinimaPin || !pin.All(char.IsDigit))
            throw new ValidacionException("El PIN debe ser numérico.");

        usuario.HashPin = hasheador.Hashear(pin);
        usuarios.Actualizar(usuario);
        await trabajo.GuardarAsync();
    }

    public void CerrarSesion() => sesion.Cerrar();

    public async Task<Empleado> ValidarSupervisorAsync(
        string nombreUsuario,
        string pinOClave,
        Permiso permisoExigido)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ValidacionException("Debe ingresar el usuario del supervisor.");
        if (string.IsNullOrWhiteSpace(pinOClave))
            throw new ValidacionException("Debe ingresar la credencial o PIN del supervisor.");

        var usuario = await usuarios.ObtenerPorNombreAsync(nombreUsuario);
        if (usuario is null || usuario.Bloqueado)
            throw new CredencialesInvalidasException();

        if (usuario.Empleado?.Activo != true)
            throw new CredencialesInvalidasException();

        var esValido = (usuario.HashPin is not null && hasheador.Verificar(pinOClave, usuario.HashPin))
            || hasheador.Verificar(pinOClave, usuario.HashClave);

        if (!esValido)
        {
            await auditoria.RegistrarAsync(
                AccionAuditoria.InicioSesionFallido,
                nameof(Usuario),
                usuario.Id,
                $"Intento fallido de autorización supervisor: {nombreUsuario}");
            await trabajo.GuardarAsync();
            throw new CredencialesInvalidasException();
        }

        var empleado = usuario.Empleado;
        if (empleado.Rol is null || !empleado.Rol.Tiene(permisoExigido))
        {
            throw new AccesoDenegadoException(permisoExigido);
        }

        await auditoria.RegistrarAsync(
            AccionAuditoria.AutorizacionSupervisor,
            nameof(Empleado),
            empleado.Id,
            $"Supervisor {nombreUsuario} autorizó operación que exige {permisoExigido}");
        await trabajo.GuardarAsync();

        return empleado;
    }

    private async Task AutenticarAsync(
        string nombre,
        string secreto,
        bool usaPin)
    {
        var usuario = await usuarios.ObtenerPorNombreAsync(nombre);

        if (usuario is null)
        {
            await RegistrarIntentoAsync(nombre, false, usaPin);
            await trabajo.GuardarAsync();
            throw new CredencialesInvalidasException();
        }

        if (usuario.Bloqueado)
        {
            await RegistrarIntentoAsync(nombre, false, usaPin);
            await auditoria.RegistrarAsync(
                AccionAuditoria.InicioSesionFallido, nameof(Usuario), usuario.Id);
            await trabajo.GuardarAsync();
            throw new CuentaBloqueadaException();
        }

        if (usuario.Empleado?.Activo != true)
            throw await RegistrarFalloAsync(usuario, usaPin);

        var hash = usaPin ? usuario.HashPin : usuario.HashClave;

        if (hash is null || !hasheador.Verificar(secreto, hash))
            throw await RegistrarFalloAsync(usuario, usaPin);

        usuario.RegistrarExito();
        usuarios.Actualizar(usuario);
        await RegistrarIntentoAsync(nombre, true, usaPin);
        await auditoria.RegistrarAsync(
            AccionAuditoria.InicioSesion, nameof(Usuario), usuario.Id);
        await trabajo.GuardarAsync();
        sesion.Iniciar(usuario);
    }

    private async Task<Exception> RegistrarFalloAsync(
        Usuario usuario,
        bool usaPin)
    {
        var maximo = await parametros.ObtenerEnteroAsync(
            ClavesParametro.MaxIntentosFallidos);

        usuario.RegistrarFallo(maximo);
        usuarios.Actualizar(usuario);
        await RegistrarIntentoAsync(usuario.NombreUsuario, false, usaPin);
        await auditoria.RegistrarAsync(
            AccionAuditoria.InicioSesionFallido, nameof(Usuario), usuario.Id);
        await trabajo.GuardarAsync();

        return usuario.Bloqueado
            ? new CuentaBloqueadaException()
            : new CredencialesInvalidasException();
    }

    private Task RegistrarIntentoAsync(string nombre, bool ok, bool usaPin)
        => intentos.AgregarAsync(new IntentoAcceso
        {
            NombreUsuario = nombre,
            Fecha = reloj.AhoraUtc(),
            Exitoso = ok,
            UsoPin = usaPin
        });

    private Usuario UsuarioEnSesion()
        => sesion.Usuario
            ?? throw new SesionNoIniciadaException();

    public Task InicializarAsync(string claveAdminInicial) => Task.CompletedTask;
}

public class ServicioInicializacion(
    IInicializadorBaseDatos baseDatos,
    IRepositorio<Usuario> usuarios,
    IRepositorio<Empleado> empleados,
    IRepositorio<Cargo> cargos,
    IRolRepositorio roles,
    IHasheadorClave hasheador,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioInicializacion
{
    public const string UsuarioAdministrador = "admin";

    public async Task InicializarAsync(string claveAdminInicial)
    {
        await baseDatos.CrearYSembrarAsync();

        if ((await usuarios.ObtenerTodosAsync()).Count > 0)
            return;
        if (string.IsNullOrWhiteSpace(claveAdminInicial))
            throw new ValidacionException("Falta la clave inicial del admin.");

        var rol = await roles.ObtenerPorNombreAsync(NombresRol.Admin)
            ?? throw new ValidacionException(
                $"Falta el rol '{NombresRol.Admin}' en la semilla.");
        var cargo = (await cargos.ObtenerTodosAsync())
            .FirstOrDefault(c => c.Nombre == NombresRol.Admin)
            ?? throw new ValidacionException(
                $"Falta el cargo '{NombresRol.Admin}' en la semilla.");

        var empleado = new Empleado
        {
            Documento = UsuarioAdministrador,
            Nombres = "Administrador",
            FechaIngreso = reloj.HoyLocal(),
            CargoId = cargo.Id,
            RolId = rol.Id
        };

        await empleados.AgregarAsync(empleado);
        await usuarios.AgregarAsync(new Usuario
        {
            NombreUsuario = UsuarioAdministrador,
            HashClave = hasheador.Hashear(claveAdminInicial),
            DebeCambiarClave = true,
            Empleado = empleado
        });
        await trabajo.GuardarAsync();
    }
}

