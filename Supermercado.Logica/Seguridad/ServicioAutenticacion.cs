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
}
