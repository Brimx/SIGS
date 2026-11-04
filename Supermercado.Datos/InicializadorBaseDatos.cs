using System.Data;
using Microsoft.EntityFrameworkCore;
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

        await AsegurarIndiceIdempotenciaUnicoAsync();
        await AsegurarColumnasPanelAsync();

        await SembrarSiVacioAsync(db.Roles, ValoresIniciales.Roles());
        await SembrarSiVacioAsync(db.Cargos, ValoresIniciales.Cargos());
        await SembrarSiVacioAsync(db.Impuestos, ValoresIniciales.Impuestos());
        await SembrarSiVacioAsync(db.Cajas, ValoresIniciales.Cajas());
        await SembrarParametrosFaltantesAsync();
    }

    private async Task AsegurarIndiceIdempotenciaUnicoAsync()
    {
        var claveDuplicada = await db.Ventas
            .Where(v => v.ClaveIdempotencia != null)
            .GroupBy(v => v.ClaveIdempotencia)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        if (claveDuplicada is not null)
            throw new InvalidOperationException(
                "Hay ventas con claves de idempotencia duplicadas; resuélvelas antes de iniciar SIGS.");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Ventas_ClaveIdempotencia\" ON \"Ventas\" (\"ClaveIdempotencia\");");
    }

    // La migración Inicial se genera desde el modelo (ADR-018), así que una base
    // creada antes no tiene las columnas de personalización del panel.
    private async Task AsegurarColumnasPanelAsync()
    {
        var columnas = await ObtenerColumnasAsync("Widgets");

        if (!columnas.Contains("Tamano"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Widgets\" ADD COLUMN \"Tamano\" INTEGER NOT NULL DEFAULT 1;");

        if (!columnas.Contains("TituloPersonalizado"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Widgets\" ADD COLUMN \"TituloPersonalizado\" TEXT NULL;");
    }

    private async Task<IReadOnlyList<string>> ObtenerColumnasAsync(string tabla)
    {
        var conexion = db.Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
            await conexion.OpenAsync();

        await using var comando = conexion.CreateCommand();
        comando.CommandText = $"SELECT name FROM pragma_table_info('{tabla}')";

        var columnas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            columnas.Add(lector.GetString(0));

        return columnas;
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
