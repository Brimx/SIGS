using Bogus;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.DatosDemo;

public class GeneradorDatosDemo(
    IRepositorio<Categoria> categorias,
    IRepositorio<Proveedor> proveedores,
    IRepositorio<Impuesto> impuestos,
    IProductoRepositorio productos,
    ILoteRepositorio lotes,
    IStockRepositorio<StockEstante> estantes,
    IStockRepositorio<StockBodega> bodegas,
    IMovimientoRepositorio movimientos,
    IRepositorio<Cargo> cargos,
    IRolRepositorio roles,
    IRepositorio<Empleado> empleados,
    IUsuarioRepositorio usuarios,
    IRepositorio<Caja> cajas,
    ITurnoCajaRepositorio turnos,
    IVentaRepositorio ventas,
    IGastoRepositorio gastos,
    IHasheadorClave hasheador,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IGeneradorDatosDemo
{
    public async Task GenerarAsync(int semilla = 42)
    {
        Randomizer.Seed = new Random(semilla);

        var existentes = await productos.ObtenerTodosAsync();
        if (existentes.Count > 0)
            return;

        var hoy = reloj.HoyLocal();
        var ahora = reloj.AhoraUtc();

        // 1. Categorías y proveedores
        var catGranos = new Categoria { Nombre = "Granos y Despensa" };
        var catLacteos = new Categoria { Nombre = "Lácteos y Frescos" };
        var catAseo = new Categoria { Nombre = "Aseo y Cuidado Personal" };
        var catFrutas = new Categoria { Nombre = "Frutas y Verduras" };
        await categorias.AgregarAsync(catGranos);
        await categorias.AgregarAsync(catLacteos);
        await categorias.AgregarAsync(catAseo);
        await categorias.AgregarAsync(catFrutas);

        var prov1 = new Proveedor
        {
            Nombre = "Distribuidora Nacional S.A.S.",
            Nit = "900111222-1",
            Telefono = "3101112233",
            Correo = "ventas@distrinacional.com"
        };
        var prov2 = new Proveedor
        {
            Nombre = "Agropecuaria del Campo",
            Nit = "800333444-2",
            Telefono = "3204445566",
            Correo = "contacto@agrodelcampo.co"
        };
        await proveedores.AgregarAsync(prov1);
        await proveedores.AgregarAsync(prov2);

        // 2. Impuestos
        var listaImpuestos = await impuestos.ObtenerTodosAsync();
        var iva0 = listaImpuestos.FirstOrDefault(i => i.PuntosBasicos == 0)
            ?? new Iva { Nombre = "IVA 0 %", PuntosBasicos = 0 };
        var iva19 = listaImpuestos.FirstOrDefault(i => i.PuntosBasicos == 1900)
            ?? new Iva { Nombre = "IVA 19 %", PuntosBasicos = 1900 };

        if (iva0.Id == 0)
            await impuestos.AgregarAsync(iva0);
        if (iva19.Id == 0)
            await impuestos.AgregarAsync(iva19);

        // 3. Productos (unitarios y pesados)
        var pArroz = new ProductoUnitario
        {
            Nombre = "Arroz Diana 1kg",
            CodigoBarras = "7701001",
            PrecioVenta = 4500,
            StockMinimoEstante = 15,
            Categoria = catGranos,
            Proveedor = prov1,
            Impuesto = iva0
        };
        var pAceite = new ProductoUnitario
        {
            Nombre = "Aceite Premier 900ml",
            CodigoBarras = "7701002",
            PrecioVenta = 12000,
            StockMinimoEstante = 10,
            Categoria = catGranos,
            Proveedor = prov1,
            Impuesto = iva19
        };
        var pLeche = new ProductoUnitario
        {
            Nombre = "Leche Entera 1L",
            CodigoBarras = "7701003",
            PrecioVenta = 4200,
            StockMinimoEstante = 20,
            Categoria = catLacteos,
            Proveedor = prov1,
            Impuesto = iva0
        };
        var pTomate = new ProductoPesado
        {
            Nombre = "Tomate Chonto",
            CodigoBarras = "7702001",
            PrecioVenta = 3800,
            StockMinimoEstante = 5000, // 5kg en gramos
            Categoria = catFrutas,
            Proveedor = prov2,
            Impuesto = iva0
        };

        await productos.AgregarAsync(pArroz);
        await productos.AgregarAsync(pAceite);
        await productos.AgregarAsync(pLeche);
        await productos.AgregarAsync(pTomate);

        // 4. Lotes e inventario inicial
        // Arroz: L1 vigente (6 meses), L2 próximo (7 días)
        var lArroz1 = new Lote
        {
            Producto = pArroz,
            NumeroLote = "ARR-001",
            FechaIngreso = hoy.AddDays(-20),
            FechaVencimiento = hoy.AddMonths(6),
            CantidadIngresada = 80,
            CostoUnitario = 3200
        };
        var lArroz2 = new Lote
        {
            Producto = pArroz,
            NumeroLote = "ARR-002",
            FechaIngreso = hoy.AddDays(-10),
            FechaVencimiento = hoy.AddDays(7),
            CantidadIngresada = 30,
            CostoUnitario = 3200
        };
        await lotes.AgregarAsync(lArroz1);
        await lotes.AgregarAsync(lArroz2);

        // Stock Arroz L1: 50 en bodega, 30 en estante
        var sbArroz1 = new StockBodega
        {
            Lote = lArroz1,
            ProductoId = pArroz.Id
        };
        sbArroz1.Aumentar(50);
        var seArroz1 = new StockEstante
        {
            Lote = lArroz1,
            ProductoId = pArroz.Id
        };
        seArroz1.Aumentar(30);
        await bodegas.AgregarAsync(sbArroz1);
        await estantes.AgregarAsync(seArroz1);

        // Stock Arroz L2: 10 en bodega, 20 en estante (antes de ventas)
        var sbArroz2 = new StockBodega
        {
            Lote = lArroz2,
            ProductoId = pArroz.Id
        };
        sbArroz2.Aumentar(10);
        var seArroz2 = new StockEstante
        {
            Lote = lArroz2,
            ProductoId = pArroz.Id
        };
        seArroz2.Aumentar(20);
        await bodegas.AgregarAsync(sbArroz2);
        await estantes.AgregarAsync(seArroz2);

        // Movimientos de ingreso y traslado para Arroz
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz1,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 80,
            Fecha = ahora.AddDays(-20),
            Motivo = "Ingreso inicial L1"
        });
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz1,
            Tipo = TipoMovimiento.Traslado,
            Cantidad = 30,
            Fecha = ahora.AddDays(-19),
            Motivo = "Traslado a estante"
        });
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz2,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 30,
            Fecha = ahora.AddDays(-10),
            Motivo = "Ingreso inicial L2"
        });
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz2,
            Tipo = TipoMovimiento.Traslado,
            Cantidad = 20,
            Fecha = ahora.AddDays(-9),
            Motivo = "Traslado a estante"
        });

        // 5. Cajas, roles, empleados y usuarios demo
        var todasCajas = await cajas.ObtenerTodosAsync();
        var caja1 = todasCajas.FirstOrDefault()
            ?? new Caja { Nombre = "Caja 1" };
        if (caja1.Id == 0)
            await cajas.AgregarAsync(caja1);

        var todosRoles = await roles.ObtenerTodosAsync();
        var rolAdmin = todosRoles.First(r => r.Nombre == NombresRol.Admin);
        var rolCajero = todosRoles.First(r => r.Nombre == NombresRol.Cajero);

        var todosCargos = await cargos.ObtenerTodosAsync();
        var cargoAdmin = todosCargos.FirstOrDefault()
            ?? new Cargo { Nombre = NombresRol.Admin };
        if (cargoAdmin.Id == 0)
            await cargos.AgregarAsync(cargoAdmin);

        var empAdmin = new Empleado
        {
            Nombres = "Administrador",
            Apellidos = "Demo",
            Documento = "990001",
            Correo = "admin@sigs.local",
            Rol = rolAdmin,
            Cargo = cargoAdmin,
            SalarioBase = 3500000,
            Activo = true
        };
        var empCajero = new Empleado
        {
            Nombres = "Cajero",
            Apellidos = "Principal",
            Documento = "990002",
            Correo = "cajero@sigs.local",
            Rol = rolCajero,
            Cargo = cargoAdmin,
            SalarioBase = 1500000,
            Activo = true
        };
        await empleados.AgregarAsync(empAdmin);
        await empleados.AgregarAsync(empCajero);

        var usrAdmin = new Usuario
        {
            NombreUsuario = "admin_demo",
            HashClave = hasheador.Hashear("Admin123*"),
            Empleado = empAdmin
        };
        var usrCajero = new Usuario
        {
            NombreUsuario = "cajero_demo",
            HashClave = hasheador.Hashear("Cajero123*"),
            Empleado = empCajero
        };
        await usuarios.AgregarAsync(usrAdmin);
        await usuarios.AgregarAsync(usrCajero);

        // 6. Turno y ventas históricas
        var turno = TurnoCaja.Abrir(
            caja1.Id,
            empCajero.Id,
            efectivoInicial: 100000,
            ahora: ahora.AddHours(-4));
        await turnos.AgregarAsync(turno);

        // Venta 1: 2 Arroz ($9.000) en efectivo
        var v1 = new Venta
        {
            Fecha = ahora.AddHours(-3),
            TurnoCajaId = turno.Id
        };
        v1.AgregarProducto(pArroz, 2);
        v1.AgregarPago(new PagoEfectivo { Monto = 10000 });
        v1.Cerrar();
        await ventas.AgregarAsync(v1);

        // Descontar de estante L2 (FEFO: L2 vence antes)
        seArroz2.Disminuir(2);
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz2,
            Venta = v1,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 2,
            Fecha = ahora.AddHours(-3)
        });

        // Venta 2: 1 Arroz ($4.500) que luego se anula
        var v2 = new Venta
        {
            Fecha = ahora.AddHours(-2),
            TurnoCajaId = turno.Id
        };
        v2.AgregarProducto(pArroz, 1);
        v2.AgregarPago(new PagoEfectivo { Monto = 5000 });
        v2.Cerrar();
        await ventas.AgregarAsync(v2);

        // Descuento inicial por venta
        seArroz2.Disminuir(1);
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz2,
            Venta = v2,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 1,
            Fecha = ahora.AddHours(-2)
        });

        // Anulación y reintegro
        v2.Anular("Cliente canceló pedido");
        seArroz2.Aumentar(1);
        await movimientos.AgregarAsync(new MovimientoInventario
        {
            Lote = lArroz2,
            Venta = v2,
            Tipo = TipoMovimiento.Devolucion,
            Cantidad = 1,
            Fecha = ahora.AddHours(-1),
            Motivo = "Anulación de venta"
        });

        // 7. Gastos del período
        await gastos.AgregarAsync(new Gasto
        {
            Descripcion = "Servicio de energía eléctrica",
            Monto = 180000,
            Fecha = ahora.AddDays(-2),
            RegistradoPorEmpleadoId = empAdmin.Id
        });
        await gastos.AgregarAsync(new Gasto
        {
            Descripcion = "Bolsas plásticas biodegradables",
            Monto = 45000,
            Fecha = ahora.AddDays(-1),
            RegistradoPorEmpleadoId = empAdmin.Id
        });

        await trabajo.GuardarAsync();
    }
}
