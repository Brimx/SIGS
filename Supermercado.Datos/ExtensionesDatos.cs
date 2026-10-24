using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Contratos;

namespace Supermercado.Datos;

public static class ExtensionesDatos
{
    public static IServiceCollection AgregarDatos(
        this IServiceCollection servicios,
        string rutaBaseDatos)
    {
        var carpeta = Path.GetDirectoryName(rutaBaseDatos);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        servicios.AddSingleton(TimeProvider.System);
        servicios.AddDbContext<SigsDbContext>(
            opciones => opciones.UseSqlite($"Data Source={rutaBaseDatos}"));

        servicios.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
        servicios.AddScoped(
            typeof(IStockRepositorio<>), typeof(StockRepositorio<>));
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IInicializadorBaseDatos, InicializadorBaseDatos>();

        servicios.AddScoped<IProductoRepositorio, ProductoRepositorio>();
        servicios.AddScoped<ILoteRepositorio, LoteRepositorio>();
        servicios.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();
        servicios.AddScoped<IReabastecimientoRepositorio,
            ReabastecimientoRepositorio>();
        servicios.AddScoped<IVentaRepositorio, VentaRepositorio>();
        servicios.AddScoped<ITurnoCajaRepositorio, TurnoCajaRepositorio>();
        servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        servicios.AddScoped<IRolRepositorio, RolRepositorio>();
        servicios.AddScoped<IParametroRepositorio, ParametroRepositorio>();
        servicios.AddScoped<IGastoRepositorio, GastoRepositorio>();
        servicios.AddScoped<IConfiguracionWidgetRepositorio,
            ConfiguracionWidgetRepositorio>();

        return servicios;
    }
}
