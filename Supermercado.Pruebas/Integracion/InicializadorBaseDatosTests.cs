using Microsoft.EntityFrameworkCore;
using Supermercado.Datos;
using Supermercado.Entidades.Configuracion;

namespace Supermercado.Pruebas.Integracion;

public class InicializadorBaseDatosTests
{
    [Fact]
    public async Task CrearYSembrarAsync_EsIdempotente()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);

        await inicializador.CrearYSembrarAsync();
        var rolesIniciales = await contexto.Db.Roles.CountAsync();
        var parametrosIniciales = await contexto.Db.Parametros.CountAsync();
        var cajasIniciales = await contexto.Db.Cajas.CountAsync();

        // Segunda ejecución consecutiva
        await inicializador.CrearYSembrarAsync();

        Assert.Equal(rolesIniciales, await contexto.Db.Roles.CountAsync());
        Assert.Equal(
            parametrosIniciales,
            await contexto.Db.Parametros.CountAsync());
        Assert.Equal(cajasIniciales, await contexto.Db.Cajas.CountAsync());
    }

    [Fact]
    public async Task CrearYSembrarAsync_NoPisaParametrosEditados()
    {
        using var contexto = new ContextoPruebasSqlite();
        var inicializador = new InicializadorBaseDatos(contexto.Db);

        await inicializador.CrearYSembrarAsync();

        var param = await contexto.Db.Parametros
            .FirstAsync(p => p.Clave == ClavesParametro.MaxIntentosFallidos);
        param.Valor = "99";
        await contexto.Db.SaveChangesAsync();

        // Ejecutar sembrado de nuevo
        await inicializador.CrearYSembrarAsync();
        contexto.Db.ChangeTracker.Clear();

        var paramDespues = await contexto.Db.Parametros
            .FirstAsync(p => p.Clave == ClavesParametro.MaxIntentosFallidos);

        Assert.Equal("99", paramDespues.Valor);
    }
}
