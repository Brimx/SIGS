using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos.Repositorios;

// SaveChanges ya es una transacción: todo lo pendiente se guarda junto.
public class UnidadDeTrabajo(SigsDbContext db) : IUnidadDeTrabajo
{
    public Task<int> GuardarAsync() => db.SaveChangesAsync();
}
