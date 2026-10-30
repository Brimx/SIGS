using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Servicios;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ServicioProductosTests
{
    private class ConsultaExternaFalsa(
        InfoProductoExterno? respuesta = null,
        bool lanzarExcepcion = false) : IConsultaCodigoBarras
    {
        public Task<InfoProductoExterno?> ConsultarAsync(
            string codigo,
            CancellationToken ct)
            => lanzarExcepcion
                ? throw new HttpRequestException("Error de red")
                : Task.FromResult(respuesta);
    }

    private class InventarioMock : IServicioInventario
    {
        public int IngresosRegistrados { get; private set; }

        public Task DescontarVentaAsync(
            int productoId, int cantidad, Entidades.Ventas.Venta venta)
            => Task.CompletedTask;

        public Task ReintegrarVentaAsync(int ventaId)
            => Task.CompletedTask;

        public Task TrasladarAEstanteAsync(int productoId, int cantidad)
            => Task.CompletedTask;

        public Task RegistrarIngresoAsync(Lote lote, int cantidad)
        {
            IngresosRegistrados += cantidad;
            return Task.CompletedTask;
        }

        public Task RegistrarMermaAsync(
            int productoId,
            int loteId,
            UbicacionStock ubicacion,
            int cantidad,
            string motivo)
            => Task.CompletedTask;
    }

    private readonly ProductosEnMemoria productos = new();
    private readonly RepositorioEnMemoria<Lote> lotes = new();
    private readonly InventarioMock inventario = new();
    private readonly UnidadDeTrabajoFalsa trabajo = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioProductos CrearServicio(
        Permiso permisos = Permiso.GestionarProductos,
        InfoProductoExterno? respuestaExterna = null,
        bool fallaExterna = false)
        => new(
            productos,
            lotes,
            inventario,
            new ConsultaExternaFalsa(respuestaExterna, fallaExterna),
            Sesiones.Con(permisos),
            trabajo,
            reloj);

    [Fact]
    public async Task RegistrarAsync_ConPermiso_GuardaProducto()
    {
        var servicio = CrearServicio();
        var prod = new ProductoUnitario
        {
            Nombre = "Galletas",
            CodigoBarras = "770111",
            PrecioVenta = 1500,
            ImpuestoId = 1
        };

        await servicio.RegistrarAsync(prod);

        Assert.Single(productos.Datos);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task RegistrarAsync_CodigoDuplicado_LanzaValidacionException()
    {
        var servicio = CrearServicio();
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 1,
            Nombre = "Existente",
            CodigoBarras = "770111",
            PrecioVenta = 1000,
            ImpuestoId = 1
        });

        var nuevo = new ProductoUnitario
        {
            Nombre = "Nuevo",
            CodigoBarras = "770111",
            PrecioVenta = 2000,
            ImpuestoId = 1
        };

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarAsync(nuevo));
    }

    [Fact]
    public async Task ConsultarExternoAsync_FallaRedRetornaNull()
    {
        var servicio = CrearServicio(fallaExterna: true);

        var resultado = await servicio.ConsultarExternoAsync("770000");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task RegistrarIngresoLoteAsync_RegistraLoteEIngresoInventario()
    {
        var servicio = CrearServicio();
        var prod = new ProductoUnitario
        {
            Id = 10,
            Nombre = "Arroz",
            CodigoBarras = "770222",
            PrecioVenta = 4000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(prod);

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        await servicio.RegistrarIngresoLoteAsync(
            10,
            "LOTE-A",
            hoy.AddDays(30),
            cantidad: 100,
            costoUnitario: 2500);

        Assert.Single(lotes.Datos);
        Assert.Equal(100, inventario.IngresosRegistrados);
        Assert.Equal(1, trabajo.Guardados);
    }

    [Fact]
    public async Task RegistrarIngresoLoteAsync_LoteVencido_LanzaError()
    {
        var servicio = CrearServicio();
        var prod = new ProductoUnitario
        {
            Id = 10,
            Nombre = "Arroz",
            CodigoBarras = "770222",
            PrecioVenta = 4000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(prod);

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarIngresoLoteAsync(
                10,
                "LOTE-VENCIDO",
                hoy.AddDays(-1),
                10,
                1000));
    }
}
