using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioAlertasInventarioTests
{
    private readonly StockEnMemoria<StockEstante> estante = new();
    private readonly ProductosEnMemoria productos = new();
    private readonly LotesEnMemoria lotes = new();
    private readonly ParametrosFalsos parametros = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioAlertasInventario CrearServicio(
        Permiso permisos = Permiso.VerInventario)
        => new(
            productos,
            estante,
            lotes,
            parametros,
            Sesiones.Con(permisos),
            reloj);

    [Fact]
    public async Task ObtenerBajoMinimo_IdentificaProductosBajoElMinimo()
    {
        var servicio = CrearServicio();

        var p1 = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz",
            StockMinimoEstante = 10,
            Activo = true
        };
        var p2 = new ProductoUnitario
        {
            Id = 2,
            Nombre = "Aceite",
            StockMinimoEstante = 5,
            Activo = true
        };
        var pInactivo = new ProductoUnitario
        {
            Id = 3,
            Nombre = "Sal",
            StockMinimoEstante = 10,
            Activo = false
        };
        await productos.AgregarAsync(p1);
        await productos.AgregarAsync(p2);
        await productos.AgregarAsync(pInactivo);

        var s1 = new StockEstante { ProductoId = 1, LoteId = 100 };
        s1.Aumentar(4); // 4 < 10 -> bajo mínimo
        await estante.AgregarAsync(s1);

        var s2 = new StockEstante { ProductoId = 2, LoteId = 200 };
        s2.Aumentar(8); // 8 >= 5 -> ok
        await estante.AgregarAsync(s2);

        var alertas = await servicio.ObtenerBajoMinimoAsync();

        var alerta = Assert.Single(alertas);
        Assert.Equal(1, alerta.Producto.Id);
        Assert.Equal(4, alerta.CantidadEnEstante);
    }

    [Fact]
    public async Task ObtenerBajoMinimo_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.ObtenerBajoMinimoAsync());
    }

    [Fact]
    public async Task ObtenerLotesPorVencer_UsaParametroDias()
    {
        parametros.Valores[ClavesParametro.DiasAlertaVencimiento] = "15";
        var servicio = CrearServicio();

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

        var loteVenceEn10Dias = new Lote
        {
            Id = 1,
            NumeroLote = "L10",
            FechaVencimiento = hoy.AddDays(10)
        };
        var loteVenceEn20Dias = new Lote
        {
            Id = 2,
            NumeroLote = "L20",
            FechaVencimiento = hoy.AddDays(20)
        };
        await lotes.AgregarAsync(loteVenceEn10Dias);
        await lotes.AgregarAsync(loteVenceEn20Dias);

        var porVencer = await servicio.ObtenerLotesPorVencerAsync();

        var lote = Assert.Single(porVencer);
        Assert.Equal("L10", lote.NumeroLote);
    }
}
