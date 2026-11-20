using Supermercado.Datos;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using System.Globalization;

namespace Supermercado.Logica.Seguridad
{
public class ServicioAutenticacion(
    IUsuarioRepositorio usuarios,
    IIntentoAccesoRepositorio intentos,
    ServicioAuditoria auditoria,
    ServicioParametros parametros,
    IHasheadorClave hasheador,
    SesionActual sesion,
    TimeProvider reloj)
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
        await usuarios.GuardarAsync();
    }

    public async Task AsignarPinAsync(string pin)
    {
        var usuario = UsuarioEnSesion();

        if (pin.Length < LongitudMinimaPin || !pin.All(char.IsDigit))
            throw new ValidacionException("El PIN debe ser numérico.");

        usuario.HashPin = hasheador.Hashear(pin);
        usuarios.Actualizar(usuario);
        await usuarios.GuardarAsync();
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
            await usuarios.GuardarAsync();
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
        await usuarios.GuardarAsync();

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
            await intentos.GuardarAsync();
            throw new CredencialesInvalidasException();
        }

        if (usuario.Bloqueado)
        {
            await RegistrarIntentoAsync(nombre, false, usaPin);
            await auditoria.RegistrarAsync(
                AccionAuditoria.InicioSesionFallido, nameof(Usuario), usuario.Id);
            await intentos.GuardarAsync();
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
        await intentos.GuardarAsync();
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
        await intentos.GuardarAsync();

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

public class ServicioInicializacion(
    InicializadorBaseDatos baseDatos,
    IUsuarioRepositorio usuarios,
    IEmpleadoRepositorio empleados,
    ICargoRepositorio cargos,
    IRolRepositorio roles,
    IHasheadorClave hasheador,
    TimeProvider reloj)
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
        await usuarios.GuardarAsync();
    }
}


public interface IHasheadorClave
{
    string Hashear(string secreto);
    bool Verificar(string secreto, string hash);
}

public class HasheadorBCrypt : IHasheadorClave
{
    public string Hashear(string secreto)
        => BCrypt.Net.BCrypt.HashPassword(secreto);

    public bool Verificar(string secreto, string hash)
        => BCrypt.Net.BCrypt.Verify(secreto, hash);
}

public class ServicioAuditoria(
    IRegistroAuditoriaRepositorio registros,
    SesionActual sesion,
    TimeProvider reloj)
{
    public virtual Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
        => registros.AgregarAsync(new RegistroAuditoria
        {
            Fecha = reloj.AhoraUtc(),
            EmpleadoId = sesion.EstaAutenticada ? sesion.EmpleadoId : null,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle
        });

    public virtual async Task<ResultadoPaginado<RegistroAuditoria>> ConsultarAsync(
        FiltroAuditoria filtro)
    {
        sesion.Exigir(Permiso.VerAuditoria);

        var todos = await registros.ObtenerTodosAsync();
        var consulta = todos.AsEnumerable();

        if (filtro.Desde.HasValue)
            consulta = consulta.Where(r => r.Fecha >= filtro.Desde.Value);

        if (filtro.Hasta.HasValue)
            consulta = consulta.Where(r => r.Fecha <= filtro.Hasta.Value);

        if (filtro.Accion.HasValue)
            consulta = consulta.Where(r => r.Accion == filtro.Accion.Value);

        if (filtro.EmpleadoId.HasValue)
            consulta = consulta.Where(r => r.EmpleadoId == filtro.EmpleadoId.Value);

        var ordenados = consulta
            .OrderByDescending(r => r.Fecha)
            .ToList();

        var total = ordenados.Count;
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Max(1, filtro.TamanoPagina);

        var items = ordenados
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToList();

        return new ResultadoPaginado<RegistroAuditoria>(
            items, total, pagina, tamano);
    }
}

public static class ExtensionesParametros
{
    // Interruptor: «0», «false» o «no» apagan; cualquier otro valor enciende.
    // Si el parámetro no existe (base anterior a la clave) vale `porDefecto`.
    public static async Task<bool> ObtenerBoolAsync(
        this ServicioParametros parametros,
        string clave,
        bool porDefecto = true)
    {
        try
        {
            var texto = (await parametros.ObtenerTextoAsync(clave))
                .Trim().ToLowerInvariant();
            return texto is not ("0" or "false" or "no");
        }
        catch (ParametroFaltanteException)
        {
            return porDefecto;
        }
    }

    public static async Task<string?> ObtenerTextoOpcionalAsync(
        this ServicioParametros parametros,
        string clave,
        string? porDefecto = null)
    {
        try
        {
            var texto = await parametros.ObtenerTextoAsync(clave);
            return string.IsNullOrWhiteSpace(texto) ? porDefecto : texto.Trim();
        }
        catch (ParametroFaltanteException)
        {
            return porDefecto;
        }
    }
}

public class ServicioParametros(
    IParametroRepositorio parametros,
    ServicioAuditoria auditoria,
    SesionActual sesion)
{
    public virtual async Task<string> ObtenerTextoAsync(string clave)
    {
        var parametro = await parametros.ObtenerPorClaveAsync(clave);
        return parametro?.Valor ?? throw new ParametroFaltanteException(clave);
    }

    public virtual async Task<int> ObtenerEnteroAsync(string clave)
        => (int)await ObtenerLongAsync(clave);

    public virtual async Task<long> ObtenerLongAsync(string clave)
    {
        var texto = await ObtenerTextoAsync(clave);
        var valido = long.TryParse(
            texto, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var valor);

        return valido
            ? valor
            : throw new ValidacionException($"'{clave}' no es un número.");
    }

    public virtual async Task GuardarAsync(string clave, string valor)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        await AsignarAsync(clave, valor);

        await auditoria.RegistrarAsync(
            AccionAuditoria.ParametroCambiado,
            nameof(Parametro),
            detalle: clave);

        await parametros.GuardarAsync();
    }

    public virtual async Task GuardarRespaldoAsync(
        IReadOnlyDictionary<string, string> valores)
    {
        foreach (var (clave, valor) in valores)
            await AsignarAsync(clave, valor);

        await parametros.GuardarAsync();
    }

    private async Task AsignarAsync(string clave, string valor)
    {
        var existente = await parametros.ObtenerPorClaveAsync(clave);

        if (existente is null)
            await parametros.AgregarAsync(
                new Parametro { Clave = clave, Valor = valor });
        else
        {
            existente.Valor = valor;
            parametros.Actualizar(existente);
        }
    }
}

public class SesionActual
{
    private readonly AsyncLocal<Usuario?> _usuarioTemporal = new();
    private Usuario? _usuarioActual;

    public Usuario? Usuario => _usuarioTemporal.Value ?? _usuarioActual;
    public bool EstaAutenticada => Usuario is not null;
    public bool DebeCambiarClave => Usuario?.DebeCambiarClave ?? false;
    public int EmpleadoId => Usuario?.EmpleadoId ?? throw new SesionNoIniciadaException();

    public bool Tiene(Permiso permiso)
    {
        if (Usuario?.Empleado?.Activo != true)
        {
            Cerrar();
            return false;
        }

        return Usuario.Empleado.Rol?.Tiene(permiso) ?? false;
    }

    public void Exigir(Permiso permiso)
    {
        if (Usuario?.Empleado?.Activo != true)
        {
            Cerrar();
            throw new AccesoDenegadoException(permiso);
        }

        if (DebeCambiarClave)
            throw new DebeCambiarClaveException();
        if (!Tiene(permiso))
            throw new AccesoDenegadoException(permiso);
    }

    public void Iniciar(Usuario usuario) => _usuarioActual = usuario;

    public IDisposable UsarUsuario(Usuario usuario)
    {
        var anterior = _usuarioTemporal.Value;
        _usuarioTemporal.Value = usuario;
        return new Restaurador(() => _usuarioTemporal.Value = anterior);
    }

    public void Cerrar() => _usuarioActual = null;

    private sealed class Restaurador(Action restaurar) : IDisposable
    {
        private Action? _restaurar = restaurar;
        public void Dispose() => Interlocked.Exchange(ref _restaurar, null)?.Invoke();
    }
}
}
