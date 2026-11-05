using Microsoft.EntityFrameworkCore;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Clientes;

namespace Supermercado.Pruebas.Integracion;

public class ClientesSqliteTests
{
    [Fact]
    public async Task Cliente_SePersisteYSeBuscaPorDocumento()
    {
        using var contexto = new ContextoPruebasSqlite();
        var repo = new ClienteRepositorio(contexto.Db);
        await repo.AgregarAsync(new Cliente
        {
            Documento = "1020304050",
            Nombres = "Lucía"
        });
        await contexto.CrearUnidadDeTrabajo().GuardarAsync();
        contexto.Db.ChangeTracker.Clear();

        var leido = await repo.ObtenerPorDocumentoAsync("1020304050");

        Assert.Equal("Lucía", leido?.Nombres);
    }

    [Fact]
    public async Task Cliente_DocumentoDuplicado_ViolaElIndiceUnico()
    {
        using var contexto = new ContextoPruebasSqlite();
        contexto.Db.Clientes.Add(new Cliente { Documento = "111111", Nombres = "A" });
        contexto.Db.Clientes.Add(new Cliente { Documento = "111111", Nombres = "B" });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.Db.SaveChangesAsync());
    }
}
