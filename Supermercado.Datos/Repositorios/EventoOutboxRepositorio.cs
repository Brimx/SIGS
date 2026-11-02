using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Sincronizacion;

namespace Supermercado.Datos.Repositorios;

public class EventoOutboxRepositorio(SigsDbContext db)
    : Repositorio<EventoOutbox>(db), IEventoOutboxRepositorio
{
    public async Task<IReadOnlyList<EventoOutbox>> ObtenerPendientesAsync(
        int limite = 50)
        => await Db.EventosOutbox
            .Where(e => !e.Procesado)
            .OrderBy(e => e.Id)
            .Take(limite)
            .ToListAsync();
}
