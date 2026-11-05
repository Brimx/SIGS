using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos;

public class InicializadorBaseDatos(SigsDbContext db) : IInicializadorBaseDatos
{
    public bool EsMotorPostgres => db.Database.IsNpgsql();

    public async Task CargarScriptPostgresAsync(string? sqlPersonalizado = null)
    {
        var sql = sqlPersonalizado;
        if (string.IsNullOrWhiteSpace(sql))
        {
            // 1. Intentar cargar desde el recurso incrustado en el ensamblado
            var ensamblado = typeof(InicializadorBaseDatos).Assembly;
            var nombreRecurso = ensamblado.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("postgresql-schema.sql", StringComparison.OrdinalIgnoreCase));
            if (nombreRecurso is not null)
            {
                await using var stream = ensamblado.GetManifestResourceStream(nombreRecurso);
                if (stream is not null)
                {
                    using var lector = new StreamReader(stream);
                    sql = await lector.ReadToEndAsync();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(sql))
        {
            // 2. Intentar buscar en disco en rutas conocidas relativas
            string[] rutas =
            [
                Path.Combine(AppContext.BaseDirectory, "docs", "postgresql-schema.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "docs", "postgresql-schema.sql"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "postgresql-schema.sql"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "docs", "postgresql-schema.sql")
            ];

            foreach (var ruta in rutas)
            {
                var rutaAbsoluta = Path.GetFullPath(ruta);
                if (File.Exists(rutaAbsoluta))
                {
                    sql = await File.ReadAllTextAsync(rutaAbsoluta);
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(sql))
            throw new FileNotFoundException("No se encontró el archivo de esquema y semilla 'docs/postgresql-schema.sql'.");

        var conexion = db.Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
            await conexion.OpenAsync();

        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 60;
        await comando.ExecuteNonQueryAsync();
    }

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
        if (db.Database.IsNpgsql())
        {
            comando.CommandText =
                "SELECT column_name FROM information_schema.columns WHERE table_name = @tabla";
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@tabla";
            parametro.Value = tabla.ToLowerInvariant();
            comando.Parameters.Add(parametro);
        }
        else
        {
            comando.CommandText = $"SELECT name FROM pragma_table_info('{tabla}')";
        }

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
