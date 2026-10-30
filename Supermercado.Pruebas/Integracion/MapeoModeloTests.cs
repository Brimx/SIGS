using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;

namespace Supermercado.Pruebas.Integracion;

public class MapeoModeloTests
{
    [Fact]
    public void Modelo_SeCreaSinErrores()
    {
        using var contexto = new ContextoPruebasSqlite();
        Assert.NotNull(contexto.Db.Model);
    }

    [Fact]
    public void StockLote_NoSeMapea_Y_EstanteYBodegaTienenTablasSeparadas()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        var tipoStockLote = modelo.FindEntityType(typeof(StockLote));
        Assert.Null(tipoStockLote);

        var tipoEstante = modelo.FindEntityType(typeof(StockEstante));
        var tipoBodega = modelo.FindEntityType(typeof(StockBodega));

        Assert.NotNull(tipoEstante);
        Assert.NotNull(tipoBodega);
        Assert.NotEqual(tipoEstante.GetTableName(), tipoBodega.GetTableName());
    }

    [Fact]
    public void Jerarquias_UsaDiscriminadorEnMismaTabla()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        var tipoProductoUnitario = modelo.FindEntityType(
            typeof(ProductoUnitario));
        var tipoProductoPesado = modelo.FindEntityType(
            typeof(ProductoPesado));
        Assert.NotNull(tipoProductoUnitario);
        Assert.NotNull(tipoProductoPesado);
        Assert.Equal(
            tipoProductoUnitario.GetTableName(),
            tipoProductoPesado.GetTableName());

        var tipoPagoEfectivo = modelo.FindEntityType(typeof(PagoEfectivo));
        var tipoPagoTarjeta = modelo.FindEntityType(typeof(PagoTarjeta));
        Assert.NotNull(tipoPagoEfectivo);
        Assert.NotNull(tipoPagoTarjeta);
        Assert.Equal(
            tipoPagoEfectivo.GetTableName(),
            tipoPagoTarjeta.GetTableName());

        var tipoIva = modelo.FindEntityType(typeof(Iva));
        var tipoRetencion = modelo.FindEntityType(typeof(Retencion));
        Assert.NotNull(tipoIva);
        Assert.NotNull(tipoRetencion);
        Assert.Equal(tipoIva.GetTableName(), tipoRetencion.GetTableName());
    }

    [Fact]
    public void IndicesUnicos_EstanConfiguradosEnMetadatos()
    {
        using var contexto = new ContextoPruebasSqlite();
        var modelo = contexto.Db.Model;

        Assert.Contains(
            modelo.FindEntityType(typeof(Producto))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "CodigoBarras"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Empleado))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Documento"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Usuario))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(
                p => p.Name == "NombreUsuario"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Rol))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Nombre"));

        Assert.Contains(
            modelo.FindEntityType(typeof(Parametro))!.GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == "Clave"));
    }

    [Fact]
    public async Task IndiceUnico_LanzaExcepcionAlDuplicar()
    {
        using var contexto = new ContextoPruebasSqlite();

        contexto.Db.Roles.Add(new Rol
        {
            Nombre = "Supervisor",
            Permisos = Permiso.VerInventario
        });
        await contexto.Db.SaveChangesAsync();

        contexto.Db.Roles.Add(new Rol
        {
            Nombre = "Supervisor",
            Permisos = Permiso.Vender
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => contexto.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Persistencia_TiposEspeciales_DateOnlyEnumsYPermiso()
    {
        using var contexto = new ContextoPruebasSqlite();

        var impuesto = new Iva { Nombre = "IVA 19", PuntosBasicos = 1900 };
        contexto.Db.Impuestos.Add(impuesto);

        var categoria = new Categoria { Nombre = "Abarrotes" };
        var proveedor = new Proveedor
        {
            Nombre = "Molino Central",
            Nit = "900123456",
            Telefono = "3001234567"
        };
        contexto.Db.Categorias.Add(categoria);
        contexto.Db.Proveedores.Add(proveedor);

        var rol = new Rol
        {
            Nombre = "Auditor",
            Permisos = Permiso.VerAuditoria | Permiso.VerReportes
        };
        contexto.Db.Roles.Add(rol);

        var producto = new ProductoUnitario
        {
            Nombre = "Arroz",
            CodigoBarras = "7701234567890",
            PrecioVenta = 3500,
            Impuesto = impuesto,
            Categoria = categoria,
            Proveedor = proveedor
        };
        contexto.Db.Productos.Add(producto);

        var vencimiento = new DateOnly(2027, 5, 20);
        var lote = new Lote
        {
            Producto = producto,
            NumeroLote = "LOT-001",
            FechaVencimiento = vencimiento,
            CostoUnitario = 2000
        };
        contexto.Db.Lotes.Add(lote);

        var movimiento = new MovimientoInventario
        {
            ProductoId = producto.Id,
            Lote = lote,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 50,
            Fecha = contexto.Reloj.AhoraUtc()
        };
        contexto.Db.Movimientos.Add(movimiento);

        await contexto.Db.SaveChangesAsync();

        contexto.Db.ChangeTracker.Clear();

        var loteLeido = await contexto.Db.Lotes
            .FirstAsync(l => l.NumeroLote == "LOT-001");
        var rolLeido = await contexto.Db.Roles
            .FirstAsync(r => r.Nombre == "Auditor");
        var movLeido = await contexto.Db.Movimientos
            .FirstAsync(m => m.Cantidad == 50);

        Assert.Equal(vencimiento, loteLeido.FechaVencimiento);
        Assert.Equal(
            Permiso.VerAuditoria | Permiso.VerReportes,
            rolLeido.Permisos);
        Assert.Equal(TipoMovimiento.Ingreso, movLeido.Tipo);
    }

    [Fact]
    public async Task FechaCreacion_SeAsignaAutomaticamenteAlGuardar()
    {
        using var contexto = new ContextoPruebasSqlite();

        var parametro = new Parametro
        {
            Clave = "test.clave",
            Valor = "123",
            Descripcion = "Prueba"
        };

        contexto.Db.Parametros.Add(parametro);
        await contexto.Db.SaveChangesAsync();

        Assert.Equal(contexto.Reloj.AhoraUtc(), parametro.FechaCreacion);
    }
}
