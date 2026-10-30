using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Supermercado.Datos.Migraciones;

[DbContext(typeof(SigsDbContext))]
[Migration("20260420000000_Inicial")]
public class Inicial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var contexto = new FabricaSigsDbContext().CreateDbContext(
            [":memory:"]);
        var sql = contexto.Database.GenerateCreateScript();
        foreach (var sentencia in DividirSentencias(sql))
            migrationBuilder.Sql(sentencia);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        string[] tablas =
        [
            "Widgets", "Parametros", "Intentos", "Auditoria", "Nominas",
            "Gastos", "Pago", "DetalleVenta", "Ventas", "Turnos", "Cajas",
            "Reabastecimientos", "Mermas", "Movimientos", "StockBodega",
            "StockEstante", "Lotes", "Productos", "Impuestos", "Proveedores",
            "Categorias", "Usuarios", "Empleados", "Roles", "Cargos"
        ];

        foreach (var tabla in tablas)
            migrationBuilder.DropTable(name: tabla);
    }

    private static IEnumerable<string> DividirSentencias(string script)
        => script
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s + ";");
}
