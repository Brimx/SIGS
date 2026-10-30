using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Supermercado.Datos;

public class FabricaSigsDbContext : IDesignTimeDbContextFactory<SigsDbContext>
{
    public SigsDbContext CreateDbContext(string[] args)
    {
        var ruta = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0]
            : RutasDatos.BaseDatosPorDefecto();

        var carpeta = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        var constructor = new DbContextOptionsBuilder<SigsDbContext>();
        constructor.UseSqlite($"Data Source={ruta}");

        return new SigsDbContext(constructor.Options, TimeProvider.System);
    }
}
