using Microsoft.EntityFrameworkCore;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Ventas;

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
            Nombres = "Lucía",
            PuntosAcumulados = 42
        });
        await contexto.CrearUnidadDeTrabajo().GuardarAsync();
        contexto.Db.ChangeTracker.Clear();

        var leido = await repo.ObtenerPorDocumentoAsync("1020304050");

        Assert.Equal(42, leido?.PuntosAcumulados);
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

    [Fact]
    public void PagoPuntos_ComparteTablaConLosDemasPagos()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        Assert.Equal(
            modelo.FindEntityType(typeof(PagoEfectivo))!.GetTableName(),
            modelo.FindEntityType(typeof(PagoPuntos))!.GetTableName());
    }
}
