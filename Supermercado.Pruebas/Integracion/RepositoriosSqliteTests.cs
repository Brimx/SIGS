using Supermercado.Datos.Repositorios;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Pruebas.Integracion;

public class RepositoriosSqliteTests
{
    [Fact]
    public async Task UsuarioRepositorio_ObtenerPorNombre_CargaEmpleadoYRol()
    {
        using var contexto = new ContextoPruebasSqlite();

        var rol = new Rol
        {
            Nombre = "Gerente",
            Permisos = Permiso.VerReportes
        };
        var cargo = new Cargo { Nombre = "Gerente de Tienda" };
        var empleado = new Empleado
        {
            Nombres = "Carlos",
            Apellidos = "Pérez",
            Documento = "12345",
            Correo = "carlos@sigs.com",
            Rol = rol,
            Cargo = cargo,
            Activo = true
        };
        var usuario = new Usuario
        {
            NombreUsuario = "carlos",
            HashClave = "hash",
            Empleado = empleado
        };

        contexto.Db.Usuarios.Add(usuario);
        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new UsuarioRepositorio(contexto.Db);
        var obtenido = await repo.ObtenerPorNombreAsync("carlos");

        Assert.NotNull(obtenido);
        Assert.NotNull(obtenido.Empleado);
        Assert.NotNull(obtenido.Empleado.Rol);
        Assert.Equal("Gerente", obtenido.Empleado.Rol.Nombre);
        Assert.Equal(Permiso.VerReportes, obtenido.Empleado.Rol.Permisos);
    }

    [Fact]
    public async Task StockRepositorio_ObtenerTotalesPorProducto_AgrupaYSuma()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var categoria = new Categoria { Nombre = "Alimentos" };
        var proveedor = new Proveedor
        {
            Nombre = "Proveedor General",
            Nit = "800999000",
            Telefono = "3000000000"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var p1 = new ProductoUnitario
        {
            Nombre = "Leche",
            CodigoBarras = "111",
            PrecioVenta = 4000,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        var p2 = new ProductoUnitario
        {
            Nombre = "Pan",
            CodigoBarras = "222",
            PrecioVenta = 2000,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.AddRange(p1, p2);

        var l1 = new Lote
        {
            Producto = p1,
            NumeroLote = "L1",
            FechaVencimiento = new DateOnly(2027, 1, 1),
            CostoUnitario = 3000
        };
        var l2 = new Lote
        {
            Producto = p1,
            NumeroLote = "L2",
            FechaVencimiento = new DateOnly(2027, 2, 1),
            CostoUnitario = 3100
        };
        var l3 = new Lote
        {
            Producto = p2,
            NumeroLote = "L3",
            FechaVencimiento = new DateOnly(2027, 3, 1),
            CostoUnitario = 1500
        };
        contexto.Db.Lotes.AddRange(l1, l2, l3);
        await contexto.Db.SaveChangesAsync();

        var s1 = new StockEstante { ProductoId = p1.Id, LoteId = l1.Id };
        s1.Aumentar(10);
        var s2 = new StockEstante { ProductoId = p1.Id, LoteId = l2.Id };
        s2.Aumentar(15);
        var s3 = new StockEstante { ProductoId = p2.Id, LoteId = l3.Id };
        s3.Aumentar(8);
        contexto.Db.StockEstante.AddRange(s1, s2, s3);

        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new StockRepositorio<StockEstante>(contexto.Db);
        var totales = await repo.ObtenerTotalesPorProductoAsync();

        Assert.Equal(25, totales[p1.Id]);
        Assert.Equal(8, totales[p2.Id]);
    }

    [Fact]
    public async Task LoteRepositorio_ObtenerPorVencer_FiltraPorFechaYStock()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var categoria = new Categoria { Nombre = "Lácteos" };
        var proveedor = new Proveedor
        {
            Nombre = "Lácteos San Juan",
            Nit = "800555444",
            Telefono = "3111111111"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var producto = new ProductoUnitario
        {
            Nombre = "Yogurt",
            CodigoBarras = "333",
            PrecioVenta = 2500,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.Add(producto);

        var loteVenceProntoConStock = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-STOCK",
            FechaVencimiento = new DateOnly(2026, 11, 5),
            CostoUnitario = 1500
        };
        var loteVenceProntoSinStock = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-VACIO",
            FechaVencimiento = new DateOnly(2026, 11, 4),
            CostoUnitario = 1500
        };
        var loteLejano = new Lote
        {
            Producto = producto,
            NumeroLote = "LV-LEJOS",
            FechaVencimiento = new DateOnly(2027, 11, 1),
            CostoUnitario = 1500
        };
        contexto.Db.Lotes.AddRange(
            loteVenceProntoConStock,
            loteVenceProntoSinStock,
            loteLejano);

        var stock = new StockEstante
        {
            ProductoId = producto.Id,
            Lote = loteVenceProntoConStock
        };
        stock.Aumentar(5);
        contexto.Db.StockEstante.Add(stock);

        await contexto.Db.SaveChangesAsync();
        contexto.Db.ChangeTracker.Clear();

        var repo = new LoteRepositorio(contexto.Db);
        var porVencer = await repo.ObtenerPorVencerAsync(
            new DateOnly(2026, 11, 10));

        var obtenido = Assert.Single(porVencer);
        Assert.Equal("LV-STOCK", obtenido.NumeroLote);
    }
}
