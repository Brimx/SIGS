using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Logica.Comunes;

namespace Supermercado.Logica.Seguridad;

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
