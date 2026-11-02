using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos.Repositorios;

public class TurnoCajaRepositorio(SigsDbContext db)
    : Repositorio<TurnoCaja>(db), ITurnoCajaRepositorio
{
    public Task<TurnoCaja?> ObtenerAbiertoPorEmpleadoAsync(int empleadoId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.EmpleadoId == empleadoId && t.Estado == EstadoTurno.Abierto);

    public Task<TurnoCaja?> ObtenerAbiertoPorCajaAsync(int cajaId)
        => Db.Turnos.Include(t => t.Retiros).FirstOrDefaultAsync(t =>
            t.CajaId == cajaId && t.Estado == EstadoTurno.Abierto);
}
