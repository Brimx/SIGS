using Microsoft.EntityFrameworkCore;
using Supermercado.Datos.Semilla;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos;

public class InicializadorBaseDatos(SigsDbContext db) : IInicializadorBaseDatos
{
    public async Task CrearYSembrarAsync()
    {
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await SembrarSiVacioAsync(db.Roles, ValoresIniciales.Roles());
        await SembrarSiVacioAsync(db.Cargos, ValoresIniciales.Cargos());
        await SembrarSiVacioAsync(db.Impuestos, ValoresIniciales.Impuestos());
        await SembrarSiVacioAsync(db.Cajas, ValoresIniciales.Cajas());
        await SembrarParametrosFaltantesAsync();
    }

    private async Task SembrarSiVacioAsync<T>(
        DbSet<T> conjunto,
        IEnumerable<T> valores) where T : EntidadBase
    {
        if (await conjunto.AnyAsync())
            return;

        await conjunto.AddRangeAsync(valores);
        await db.SaveChangesAsync();
    }

    // Agrega solo los parámetros nuevos; nunca pisa los ya editados.
    private async Task SembrarParametrosFaltantesAsync()
    {
        var existentes = await db.Parametros
            .Select(p => p.Clave)
            .ToListAsync();
        var faltantes = ValoresIniciales.Parametros()
            .Where(p => !existentes.Contains(p.Clave))
            .ToList();

        await db.Parametros.AddRangeAsync(faltantes);
        await db.SaveChangesAsync();
    }
}
