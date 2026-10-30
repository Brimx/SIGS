using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Supermercado.Datos;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Contratos;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas.Integracion;

public sealed class ContextoPruebasSqlite : IDisposable
{
    private readonly SqliteConnection conexion;

    public SigsDbContext Db { get; }
    public RelojFijo Reloj { get; }

    public ContextoPruebasSqlite(RelojFijo? reloj = null)
    {
        Reloj = reloj ?? new RelojFijo(
            new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc));

        conexion = new SqliteConnection("DataSource=:memory:");
        conexion.Open();

        var opciones = new DbContextOptionsBuilder<SigsDbContext>()
            .UseSqlite(conexion)
            .Options;

        Db = new SigsDbContext(opciones, Reloj);
        Db.Database.EnsureCreated();
    }

    public IUnidadDeTrabajo CrearUnidadDeTrabajo()
        => new UnidadDeTrabajo(Db);

    public void Dispose()
    {
        Db.Dispose();
        conexion.Dispose();
    }
}
