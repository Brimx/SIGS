using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Productos;
using Supermercado.Pruebas.Falsos;
using Xunit;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas;

public class CatalogosTests
{
    private readonly CategoriasEnMemoria categorias = new();
    private readonly ProveedoresEnMemoria proveedores = new();
    private readonly AuditoriaFalsa auditoria = new();

    private ServicioProductos CrearServicio(
        Permiso permisos = Permiso.GestionarProductos)
        => new(
            null!,
            categorias: categorias,
            proveedores: proveedores,
            auditoria: auditoria,
            sesion: Sesiones.Con(permisos));

    [Fact]
    public async Task CrearCategoria_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio();

        var creada = await servicio.CrearCategoriaAsync("Bebidas");

        Assert.Equal("Bebidas", creada.Nombre);
        Assert.Single(categorias.Datos);
        Assert.Contains(AccionAuditoria.CategoriaCreada, auditoria.Acciones);
        Assert.Equal(1, categorias.Guardados);
    }

    [Fact]
    public async Task CrearCategoria_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearCategoriaAsync("Lácteos"));
    }

    [Fact]
    public async Task EditarCategoria_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var cat = new Categoria { Id = 1, Nombre = "Vieja" };
        await categorias.AgregarAsync(cat);

        await servicio.EditarCategoriaAsync(1, "Nueva");

        Assert.Equal("Nueva", cat.Nombre);
        Assert.Contains(
            AccionAuditoria.CategoriaModificada,
            auditoria.Acciones);
        Assert.Equal(1, categorias.Guardados);
    }

    [Fact]
    public async Task CrearProveedor_ConPermiso_GuardaYAudita()
    {
        var servicio = CrearServicio();

        var prov = await servicio.CrearProveedorAsync(
            "Distribuidora ABC",
            "900123456-1",
            "3001112233",
            "contacto@abc.com");

        Assert.Equal("Distribuidora ABC", prov.Nombre);
        Assert.Equal("900123456-1", prov.Nit);
        Assert.Single(proveedores.Datos);
        Assert.Contains(AccionAuditoria.ProveedorCreado, auditoria.Acciones);
        Assert.Equal(1, proveedores.Guardados);
    }

    [Fact]
    public async Task CrearProveedor_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Ninguno);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.CrearProveedorAsync("X", "123", "", ""));
    }

    [Fact]
    public async Task EditarProveedor_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var prov = new Proveedor
        {
            Id = 1,
            Nombre = "Antiguo",
            Nit = "111222333",
            Telefono = "3001234567",
            Correo = "a@a.com"
        };
        await proveedores.AgregarAsync(prov);

        await servicio.EditarProveedorAsync(
            1, "Actualizado", "222333444", "3109876543", "b@b.com");

        Assert.Equal("Actualizado", prov.Nombre);
        Assert.Equal("222333444", prov.Nit);
        Assert.Contains(
            AccionAuditoria.ProveedorModificado,
            auditoria.Acciones);
        Assert.Equal(1, proveedores.Guardados);
    }
}

public class MantenimientoProductosTests
{
    private readonly ProductosEnMemoria productos = new();
    private readonly AuditoriaFalsa auditoria = new();

    private ServicioProductos CrearServicio(
        Permiso permisos = Permiso.GestionarProductos)
        => new(
            productos,
            auditoria: auditoria,
            sesion: Sesiones.Con(permisos));

    [Fact]
    public async Task EditarAsync_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz Antiguo",
            CodigoBarras = "7701",
            PrecioVenta = 3000,
            StockMinimoEstante = 5,
            CategoriaId = 1,
            ProveedorId = 1,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.EditarAsync(
            1, "Arroz Premium", 10, 2, 3, 2);

        Assert.Equal("Arroz Premium", producto.Nombre);
        Assert.Equal(10, producto.StockMinimoEstante);
        Assert.Equal(2, producto.CategoriaId);
        Assert.Equal(3, producto.ProveedorId);
        Assert.Equal(2, producto.ImpuestoId);
        Assert.Contains(
            AccionAuditoria.ProductoModificado,
            auditoria.Acciones);
        Assert.Equal(1, productos.Guardados);
    }

    [Fact]
    public async Task EditarAsync_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearServicio(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.EditarAsync(1, "Nuevo", 5, 1, 1, 1));
    }

    [Fact]
    public async Task EditarAsync_ProductoInexistente_LanzaValidacionException()
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.EditarAsync(999, "Nuevo", 5, 1, 1, 1));
    }

    [Fact]
    public async Task CambiarPrecioAsync_ConPermiso_ActualizaYAudita()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Café",
            CodigoBarras = "7702",
            PrecioVenta = 5000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.CambiarPrecioAsync(1, 5500);

        Assert.Equal(5500, producto.PrecioVenta);
        Assert.Contains(
            AccionAuditoria.PrecioCambiado,
            auditoria.Acciones);
        Assert.Equal(1, productos.Guardados);
    }

    [Fact]
    public async Task CambiarPrecioAsync_PrecioInvalido_LanzaError()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Café",
            CodigoBarras = "7702",
            PrecioVenta = 5000,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarPrecioAsync(1, 0));
        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CambiarPrecioAsync(1, -100));
    }

    [Fact]
    public async Task ActivarYDesactivar_CambianEstadoYAuditan()
    {
        var servicio = CrearServicio();
        var producto = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Galletas",
            CodigoBarras = "7703",
            Activo = true,
            ImpuestoId = 1
        };
        await productos.AgregarAsync(producto);

        await servicio.DesactivarAsync(1);
        Assert.False(producto.Activo);
        Assert.Equal(1, productos.Guardados);

        await servicio.ActivarAsync(1);
        Assert.True(producto.Activo);
        Assert.Equal(2, productos.Guardados);
        Assert.Equal(2, auditoria.Acciones.Count(
            a => a == AccionAuditoria.ProductoModificado));
    }
}

public class ProductoTests
{
    [Fact]
    public void Unitario_MultiplicaPrecioPorCantidad()
    {
        var producto = new ProductoUnitario { PrecioVenta = 2500 };
        Assert.Equal(7500L, producto.CalcularSubtotal(3));
    }

    [Fact]
    public void Pesado_CobraPorGramos()
    {
        var producto = new ProductoPesado { PrecioVenta = 8000 };
        Assert.Equal(4000L, producto.CalcularSubtotal(500));
    }

    [Fact]
    public void Lote_VencidoSiLaFechaYaPaso()
    {
        var lote = new Lote
        {
            FechaVencimiento = new DateOnly(2026, 10, 1)
        };
        var hoy = new DateOnly(2026, 10, 17);
        Assert.True(lote.EstaVencido(hoy));
    }
}

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

    private class InventarioMock : ServicioInventario
    {
        public InventarioMock() : base(null!, null!, null!, null!, null!, null!)
        {
        }

        public int IngresosRegistrados { get; private set; }

        public override Task DescontarVentaAsync(
            int productoId, int cantidad, Entidades.Ventas.Venta venta)
            => Task.CompletedTask;

        public override Task ReintegrarVentaAsync(int ventaId)
            => Task.CompletedTask;

        public override Task TrasladarAEstanteAsync(int productoId, int cantidad)
            => Task.CompletedTask;

        public override Task RegistrarIngresoAsync(Lote lote, int cantidad)
        {
            IngresosRegistrados += cantidad;
            return Task.CompletedTask;
        }

        public override Task RegistrarMermaAsync(
            int productoId,
            int loteId,
            UbicacionStock ubicacion,
            int cantidad,
            string motivo)
            => Task.CompletedTask;
    }

    private readonly ProductosEnMemoria productos = new();
    private readonly LotesEnMemoria lotes = new();
    private readonly InventarioMock inventario = new();
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
        Assert.Equal(1, productos.Guardados);
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
        Assert.Equal(1, lotes.Guardados);
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

public class AltaInventarioTests
{
    private readonly ProductosEnMemoria productos = new();
    private readonly LotesEnMemoria lotes = new();
    private readonly StockBodegaEnMemoria bodega = new();
    private readonly MovimientosEnMemoria movimientos = new();
    private readonly CategoriasEnMemoria categorias = new();
    private readonly ProveedoresEnMemoria proveedores = new();
    private readonly ImpuestosEnMemoria impuestos = new();
    private readonly AuditoriaFalsa auditoria = new();
    private readonly RelojFijo reloj = new(EscenarioInventario.Ahora);

    private ServicioInventario CrearInventario()
        => new(null!, bodega, movimientos, null!, null!, reloj,
            productos: productos, lotes: lotes);

    private ServicioProductos CrearCatalogo(Permiso permisos)
        => new(
            productos,
            lotes,
            CrearInventario(),
            sesion: Sesiones.Con(permisos),
            reloj: reloj,
            categorias: categorias,
            proveedores: proveedores,
            auditoria: auditoria,
            impuestos: impuestos);

    private async Task<(int CategoriaId, int ImpuestoId)> SembrarCatalogo()
    {
        var categoria = new Categoria { Id = 1, Nombre = "Granos" };
        var proveedor = new Proveedor { Id = 1, Nombre = "Distribuidora" };
        var impuesto = new Iva { Id = 1, Nombre = "IVA 19 %", PuntosBasicos = 1900 };

        await categorias.AgregarAsync(categoria);
        await proveedores.AgregarAsync(proveedor);
        await impuestos.AgregarAsync(impuesto);

        return (categoria.Id, impuesto.Id);
    }

    [Fact]
    public async Task CrearProductoAsync_ConPermiso_GuardaProductoYAudita()
    {
        var (categoriaId, impuestoId) = await SembrarCatalogo();
        var servicio = CrearCatalogo(Permiso.GestionarProductos);

        var producto = await servicio.CrearProductoAsync(
            "770999", "Arroz Diana 1 kg", categoriaId, 4500, impuestoId, 10);

        Assert.Single(productos.Datos);
        Assert.Equal("Arroz Diana 1 kg", producto.Nombre);
        Assert.Equal(1, producto.ProveedorId);
        Assert.Contains(AccionAuditoria.ProductoCreado, auditoria.Acciones);
        Assert.Equal(1, productos.Guardados);
    }

    [Fact]
    public async Task CrearProductoAsync_CodigoDuplicado_LanzaValidacionException()
    {
        var (categoriaId, impuestoId) = await SembrarCatalogo();
        var servicio = CrearCatalogo(Permiso.GestionarProductos);
        await servicio.CrearProductoAsync("770999", "Arroz", categoriaId, 4500, impuestoId, 10);

        await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CrearProductoAsync(
                "770999", "Arroz duplicado", categoriaId, 5000, impuestoId, 5));

        Assert.Single(productos.Datos);
    }

    [Fact]
    public async Task EntradaBodega_CreaLoteConVencimientoStockMovimientoYAuditoria()
    {
        var servicio = CrearCatalogo(Permiso.GestionarProductos);
        var producto = new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770999",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        };
        await productos.AgregarAsync(producto);

        var vencimiento = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime).AddDays(30);
        await servicio.RegistrarIngresoLoteAsync(5, "L-2026-01", vencimiento, 25, 3000);

        var lote = Assert.Single(lotes.Datos);
        Assert.Equal("L-2026-01", lote.NumeroLote);
        Assert.Equal(vencimiento, lote.FechaVencimiento);
        Assert.Equal(25, lote.CantidadIngresada);
        Assert.Equal(25, Assert.Single(bodega.Datos).Cantidad);
        Assert.Equal(TipoMovimiento.Ingreso, Assert.Single(movimientos.Datos).Tipo);
        Assert.Contains(AccionAuditoria.IngresoBodegaRegistrado, auditoria.Acciones);
    }

    [Fact]
    public async Task EntradaBodega_MismoLote_SumaCantidadSinDuplicarLote()
    {
        var servicio = CrearCatalogo(Permiso.GestionarProductos);
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770999",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        });

        var vencimiento = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime).AddDays(30);
        await servicio.RegistrarIngresoLoteAsync(5, "L-2026-01", vencimiento, 10, 3000);
        await servicio.RegistrarIngresoLoteAsync(5, "L-2026-01", vencimiento, 20, 3000);

        Assert.Single(lotes.Datos);
        Assert.Equal(30, lotes.Datos[0].CantidadIngresada);
        Assert.Equal(30, Assert.Single(bodega.Datos).Cantidad);
        Assert.Equal(2, movimientos.Datos.Count);
    }

    [Fact]
    public async Task EntradaBodega_SinPermiso_LanzaAccesoDenegado()
    {
        var servicio = CrearCatalogo(Permiso.Vender);
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 5,
            Nombre = "Arroz",
            CodigoBarras = "770999",
            PrecioVenta = 4500,
            ImpuestoId = 1,
            ProveedorId = 1
        });

        var vencimiento = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime).AddDays(30);
        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.RegistrarIngresoLoteAsync(5, "L-2026-01", vencimiento, 10, 3000));

        Assert.Empty(lotes.Datos);
        Assert.Empty(bodega.Datos);
        Assert.Empty(movimientos.Datos);
    }

}
