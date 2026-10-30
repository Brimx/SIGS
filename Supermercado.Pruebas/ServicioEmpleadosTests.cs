using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Personal;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioEmpleadosTests
{
    private class VerificadorCorreoFalso(bool resultado = true)
        : IVerificadorCorreo
    {
        public Task<bool> VerificarAsync(
            string correo,
            CancellationToken ct = default)
            => Task.FromResult(resultado);
    }

    private readonly RepositorioEnMemoria<Empleado> empleados = new();
    private readonly RepositorioEnMemoria<Rol> roles = new();
    private readonly UsuariosEnMemoria usuarios = new();
    private readonly HasheadorFalso hasheador = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioEmpleados CrearServicio(
        Permiso permisos = Permiso.GestionarPersonal | Permiso.GestionarRoles,
        bool correoValido = true)
        => new(
            empleados,
            roles,
            usuarios,
            new VerificadorCorreoFalso(correoValido),
            hasheador,
            auditoria,
            Sesiones.Con(permisos),
            trabajo,
            reloj);

    [Fact]
    public async Task CrearAsync_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio();
        var emp = new Empleado
        {
            Documento = "101",
            Nombres = "Laura",
            Apellidos = "Gómez",
            Correo = "laura@sigs.local",
            SalarioBase = 1500000,
            RolId = 1,
            CargoId = 1
        };

        await servicio.CrearAsync(emp);

        Assert.Single(empleados.Datos);
        Assert.True(emp.CorreoVerificado);
        Assert.Contains(AccionAuditoria.EmpleadoCreado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearAsync_DocumentoDuplicado_LanzaValidacionException()
    {
        var servicio = CrearServicio();
        await empleados.AgregarAsync(new Empleado
        {
            Id = 1,
            Documento = "101",
            Nombres = "Existente"
        });

        var nuevo = new Empleado
        {
            Documento = "101",
            Nombres = "Otro"
        };

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CrearAsync(nuevo));
    }

    [Fact]
    public async Task EditarAsync_CambiaRolSinPermisoGestionarRoles_LanzaError()
    {
        var emp = new Empleado
        {
            Id = 1,
            Documento = "101",
            Nombres = "Ana",
            RolId = 1
        };
        await empleados.AgregarAsync(emp);

        // Sesión solo con GestionarPersonal, SIN GestionarRoles
        var servicio = CrearServicio(Permiso.GestionarPersonal);

        var editado = new Empleado
        {
            Id = 1,
            Documento = "101",
            Nombres = "Ana",
            RolId = 2 // Intenta cambiar rol
        };

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.EditarAsync(editado));
    }

    [Fact]
    public async Task CambiarRolAsync_ConPermiso_ActualizaYAudita()
    {
        var emp = new Empleado
        {
            Id = 1,
            Documento = "101",
            Nombres = "Pedro",
            RolId = 1
        };
        await empleados.AgregarAsync(emp);
        await roles.AgregarAsync(new Rol { Id = 2, Nombre = "Gerente" });

        var servicio = CrearServicio();
        await servicio.CambiarRolAsync(1, 2);

        Assert.Equal(2, emp.RolId);
        Assert.Contains(AccionAuditoria.CambioRol, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task DesactivarAsync_ConPermiso_DesactivaYAudita()
    {
        var emp = new Empleado
        {
            Id = 1,
            Documento = "101",
            Nombres = "Marta",
            Activo = true
        };
        await empleados.AgregarAsync(emp);

        var servicio = CrearServicio();
        await servicio.DesactivarAsync(1);

        Assert.False(emp.Activo);
        Assert.Contains(
            AccionAuditoria.EmpleadoDesactivado,
            auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearUsuarioAsync_ConPermiso_GuardaYAudita()
    {
        var emp = new Empleado
        {
            Id = 5,
            Documento = "555",
            Nombres = "Usuario"
        };
        await empleados.AgregarAsync(emp);

        var servicio = CrearServicio();
        await servicio.CrearUsuarioAsync(5, "usr5", "temporal123");

        var u = Assert.Single(usuarios.Datos);
        Assert.Equal("usr5", u.NombreUsuario);
        Assert.Equal("h:temporal123", u.HashClave);
        Assert.True(u.DebeCambiarClave);
        Assert.Contains(AccionAuditoria.UsuarioCreado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearUsuarioAsync_ClaveCorta_LanzaValidacionException()
    {
        var emp = new Empleado
        {
            Id = 5,
            Documento = "555",
            Nombres = "Usuario"
        };
        await empleados.AgregarAsync(emp);

        var servicio = CrearServicio();
        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CrearUsuarioAsync(5, "usr5", "123")); // < 6 chars
    }
}
