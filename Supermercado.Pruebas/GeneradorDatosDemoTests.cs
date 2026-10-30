using Supermercado.Datos.Semilla;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.DatosDemo;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class GeneradorDatosDemoTests
{
    private class EscenarioDemo
    {
        public RepositorioEnMemoria<Categoria> Categorias { get; } = new();
        public RepositorioEnMemoria<Proveedor> Proveedores { get; } = new();
        public RepositorioEnMemoria<Impuesto> Impuestos { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public LotesEnMemoria Lotes { get; } = new();
        public StockEnMemoria<StockEstante> Estantes { get; } = new();
        public StockEnMemoria<StockBodega> Bodegas { get; } = new();
        public MovimientosEnMemoria Movimientos { get; } = new();
        public RepositorioEnMemoria<Cargo> Cargos { get; } = new();
        public RolesEnMemoria Roles { get; } = new();
        public RepositorioEnMemoria<Empleado> Empleados { get; } = new();
        public UsuariosEnMemoria Usuarios { get; } = new();
        public RepositorioEnMemoria<Caja> Cajas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public GastosEnMemoria Gastos { get; } = new();
        public HasheadorFalso Hasheador { get; } = new();
        public UnidadDeTrabajoFalsa Trabajo { get; } = new();
        public RelojFijo Reloj { get; } = new(EscenarioInventario.Ahora);

        public GeneradorDatosDemo Generador { get; }

        public EscenarioDemo()
        {
            foreach (var rol in ValoresIniciales.Roles())
                Roles.Datos.Add(rol);
            foreach (var cargo in ValoresIniciales.Cargos())
                Cargos.Datos.Add(cargo);
            foreach (var imp in ValoresIniciales.Impuestos())
                Impuestos.Datos.Add(imp);

            Generador = new GeneradorDatosDemo(
                Categorias,
                Proveedores,
                Impuestos,
                Productos,
                Lotes,
                Estantes,
                Bodegas,
                Movimientos,
                Cargos,
                Roles,
                Empleados,
                Usuarios,
                Cajas,
                Turnos,
                Ventas,
                Gastos,
                Hasheador,
                Trabajo,
                Reloj);
        }
    }

    [Fact]
    public async Task GenerarAsync_PueblaEntidadesYGuarda()
    {
        var e = new EscenarioDemo();

        await e.Generador.GenerarAsync();

        Assert.NotEmpty(e.Productos.Datos);
        Assert.NotEmpty(e.Lotes.Datos);
        Assert.NotEmpty(e.Ventas.Datos);
        Assert.NotEmpty(e.Gastos.Datos);
        Assert.NotEmpty(e.Usuarios.Datos);
        Assert.True(e.Trabajo.Guardados >= 1);
    }

    [Fact]
    public async Task GenerarAsync_MantieneCoherenciaMatematicaDeStock()
    {
        var e = new EscenarioDemo();
        await e.Generador.GenerarAsync();

        // Para cada lote generado, el stock físico (estante + bodega)
        // debe igualar exactamente: Ingresos - VentasEfectivas
        foreach (var lote in e.Lotes.Datos)
        {
            var cantBodega = e.Bodegas.Datos
                .Where(s => s.LoteId == lote.Id)
                .Sum(s => s.Cantidad);
            var cantEstante = e.Estantes.Datos
                .Where(s => s.LoteId == lote.Id)
                .Sum(s => s.Cantidad);

            var totalFisico = cantBodega + cantEstante;

            var ingresos = e.Movimientos.Datos
                .Where(m => m.LoteId == lote.Id
                    && m.Tipo == TipoMovimiento.Ingreso)
                .Sum(m => m.Cantidad);

            var ventasCompletadas = e.Movimientos.Datos
                .Where(m => m.LoteId == lote.Id
                    && m.Tipo == TipoMovimiento.Venta
                    && m.Venta?.Estado == EstadoVenta.Completada)
                .Sum(m => m.Cantidad);

            Assert.Equal(ingresos - ventasCompletadas, totalFisico);
        }
    }

    [Fact]
    public async Task GenerarAsync_NoDuplicaSiYaExistenProductos()
    {
        var e = new EscenarioDemo();
        await e.Generador.GenerarAsync();
        var conteoInicial = e.Productos.Datos.Count;

        await e.Generador.GenerarAsync();
        Assert.Equal(conteoInicial, e.Productos.Datos.Count);
    }
}
