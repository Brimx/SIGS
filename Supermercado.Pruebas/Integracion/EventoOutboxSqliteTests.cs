using Microsoft.EntityFrameworkCore;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Pruebas.Integracion;

public class EventoOutboxSqliteTests
{
    [Fact]
    public async Task Venta_ConClaveIdempotencia_SeGuardaYRecupera()
    {
        using var contexto = new ContextoPruebasSqlite();
        await new Supermercado.Datos.InicializadorBaseDatos(contexto.Db).CrearYSembrarAsync();
        var caja = await contexto.Db.Cajas.FirstAsync();
        var rol = await contexto.Db.Roles.FirstAsync();
        var cargo = await contexto.Db.Cargos.FirstAsync();

        var empleado = new Empleado
        {
            Nombres = "Cajero",
            Apellidos = "Uno",
            Documento = "987654321",
            RolId = rol.Id,
            CargoId = cargo.Id
        };
        contexto.Db.Empleados.Add(empleado);
        await contexto.Db.SaveChangesAsync();

        var turno = TurnoCaja.Abrir(caja.Id, empleado.Id, 50000, DateTime.UtcNow);
        contexto.Db.Turnos.Add(turno);
        await contexto.Db.SaveChangesAsync();

        var ventaRepo = new VentaRepositorio(contexto.Db);
        var venta = new Venta
        {
            Fecha = DateTime.UtcNow,
            TurnoCajaId = turno.Id,
            ClaveIdempotencia = "IDEM-TEST-456"
        };

        await ventaRepo.AgregarAsync(venta);
        await contexto.CrearUnidadDeTrabajo().GuardarAsync();

        contexto.Db.ChangeTracker.Clear();

        var recuperada = await ventaRepo.ObtenerPorClaveIdempotenciaAsync("IDEM-TEST-456");
        Assert.NotNull(recuperada);
        Assert.Equal(venta.Id, recuperada.Id);
    }
}
