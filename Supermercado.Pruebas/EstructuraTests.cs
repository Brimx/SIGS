using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Personal;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class EstructuraTests
{
    private readonly RepositorioEnMemoria<Caja> cajas = new();
    private readonly RepositorioEnMemoria<Cargo> cargos = new();
    private readonly RolesEnMemoria roles = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();

    private ServicioEstructura CrearServicio(
        Permiso permisos = Permiso.GestionarPersonal | Permiso.GestionarRoles)
        => new(
            cajas,
            cargos,
            roles,
            auditoria,
            Sesiones.Con(permisos),
            trabajo);

    [Fact]
    public async Task CrearCaja_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarPersonal);

        var caja = await servicio.CrearCajaAsync("Caja Rápida");

        Assert.Equal("Caja Rápida", caja.Nombre);
        Assert.Single(cajas.Datos);
        Assert.Contains(AccionAuditoria.CajaCreada, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task EditarCaja_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarPersonal);
        var caja = new Caja { Id = 1, Nombre = "Caja 1", Activa = true };
        await cajas.AgregarAsync(caja);

        await servicio.EditarCajaAsync(1, "Caja Principal", false);

        Assert.Equal("Caja Principal", caja.Nombre);
        Assert.False(caja.Activa);
        Assert.Contains(AccionAuditoria.CajaModificada, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearCargo_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarPersonal);

        var cargo = await servicio.CrearCargoAsync("Auxiliar de Bodega");

        Assert.Equal("Auxiliar de Bodega", cargo.Nombre);
        Assert.Single(cargos.Datos);
        Assert.Contains(AccionAuditoria.CargoCreado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task EditarCargo_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarPersonal);
        var cargo = new Cargo { Id = 1, Nombre = "Cajero Auxiliar" };
        await cargos.AgregarAsync(cargo);

        await servicio.EditarCargoAsync(1, "Cajero Principal");

        Assert.Equal("Cajero Principal", cargo.Nombre);
        Assert.Contains(AccionAuditoria.CargoModificado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearRol_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarRoles);

        var rol = await servicio.CrearRolAsync(
            "Supervisor",
            Permiso.VerReportes | Permiso.VerInventario);

        Assert.Equal("Supervisor", rol.Nombre);
        Assert.Equal(
            Permiso.VerReportes | Permiso.VerInventario,
            rol.Permisos);
        Assert.Contains(AccionAuditoria.RolCreado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task CrearRol_Duplicado_LanzaValidacionException()
    {
        var servicio = CrearServicio(Permiso.GestionarRoles);
        await roles.AgregarAsync(new Rol
        {
            Nombre = "Supervisor",
            Permisos = Permiso.VerReportes
        });

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CrearRolAsync("Supervisor", Permiso.Vender));
    }

    [Fact]
    public async Task EditarPermisosRol_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio(Permiso.GestionarRoles);
        var rol = new Rol
        {
            Id = 2,
            Nombre = "Operario",
            Permisos = Permiso.VerInventario
        };
        await roles.AgregarAsync(rol);

        await servicio.EditarPermisosRolAsync(
            2,
            Permiso.VerInventario | Permiso.AjustarInventario);

        Assert.Equal(
            Permiso.VerInventario | Permiso.AjustarInventario,
            rol.Permisos);
        Assert.Contains(AccionAuditoria.RolModificado, auditoria.Acciones);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task EditarPermisosRol_AdminSinGestionarRoles_LanzaError()
    {
        var servicio = CrearServicio(Permiso.GestionarRoles);
        var admin = new Rol
        {
            Id = 1,
            Nombre = NombresRol.Admin,
            Permisos = Permiso.GestionarRoles | Permiso.Vender
        };
        await roles.AgregarAsync(admin);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.EditarPermisosRolAsync(1, Permiso.Vender));
    }

    [Fact]
    public async Task Operaciones_SinPermisoRequerido_LanzanAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Ninguno);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearCajaAsync("Caja"));
        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearCargoAsync("Cargo"));
        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearRolAsync("Rol", Permiso.Vender));
    }
}
