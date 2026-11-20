using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Xunit;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Inventario;

namespace Supermercado.Pruebas;

public class VentasTests
{
    private static Producto Arroz()
        => new ProductoUnitario
        {
            Id = 1,
            PrecioVenta = 3000,
            Impuesto = new Iva()
        };

    [Fact]
    public void Total_SumaLosSubtotales()
    {
        var venta = new Venta();
        var queso = new ProductoPesado
        {
            Id = 2,
            PrecioVenta = 8000,
            Impuesto = new Iva()
        };

        venta.AgregarProducto(Arroz(), 2);
        venta.AgregarProducto(queso, 500);

        Assert.Equal(10000L, venta.Total);
    }

    [Fact]
    public void Detalle_CongelaElIvaDelMomentoDeLaVenta()
    {
        var leche = new ProductoUnitario
        {
            Id = 3,
            PrecioVenta = 11900,
            Impuesto = new Iva { PuntosBasicos = 1900 }
        };
        var venta = new Venta();

        venta.AgregarProducto(leche, 1);

        Assert.Equal(1900L, venta.TotalIva);
        Assert.Equal(1900, venta.Detalles[0].PuntosBasicosIva);
    }

    [Fact]
    public void Detalle_ExigeElImpuestoCargado()
    {
        var sinImpuesto = new ProductoUnitario { Id = 4, PrecioVenta = 1000 };

        Assert.Throws<ValidacionException>(
            () => new Venta().AgregarProducto(sinImpuesto, 1));
    }

    [Fact]
    public void PagoMixto_CalculaElCambio()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 10);
        venta.AgregarPago(new PagoTarjeta
        {
            Monto = 20000,
            UltimosDigitos = "1234"
        });
        venta.AgregarPago(new PagoEfectivo { Monto = 15000 });

        venta.Cerrar();

        Assert.Equal(5000L, venta.Cambio);
        Assert.Equal(10000L, venta.EfectivoNeto);
        Assert.Equal(EstadoVenta.Completada, venta.Estado);
    }

    [Fact]
    public void Cerrar_FallaSiElPagoNoAlcanza()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 1);
        venta.AgregarPago(new PagoEfectivo { Monto = 1000 });

        Assert.Throws<VentaInvalidaException>(() => venta.Cerrar());
    }

    [Fact]
    public void Cerrar_NoDaCambioDeUnaTarjeta()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 1);
        venta.AgregarPago(new PagoTarjeta
        {
            Monto = 5000,
            UltimosDigitos = "1234"
        });

        Assert.Throws<VentaInvalidaException>(() => venta.Cerrar());
    }

    [Fact]
    public void Tarjeta_ExigeLosDigitosRequeridos()
    {
        var venta = new Venta();
        var pago = new PagoTarjeta { Monto = 5000, UltimosDigitos = "12" };

        Assert.Throws<VentaInvalidaException>(
            () => venta.AgregarPago(pago));
    }

    [Fact]
    public void Anular_SoloSirveConVentasCerradas()
    {
        var venta = new Venta();

        Assert.Throws<VentaInvalidaException>(() => venta.Anular("error"));
    }

    [Fact]
    public void Anular_ExigeUnMotivo()
    {
        var venta = new Venta();
        venta.AgregarProducto(Arroz(), 1);
        venta.AgregarPago(new PagoEfectivo { Monto = 3000 });
        venta.Cerrar();

        Assert.Throws<VentaInvalidaException>(() => venta.Anular(" "));
    }
}

public class VentasEnEsperaYSupervisionTests
{
    private const string ClaveSupervisor = "supervisor123";
    private const string PinSupervisor = "9988";

    [Fact]
    public async Task ValidarSupervisor_ConCredencialesYPermisoCorrectos_RetornaEmpleadoSinAfectarSesion()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesionCajero = new SesionActual();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var cajero = new Usuario
        {
            Id = 1,
            NombreUsuario = "cajero1",
            HashClave = hasheador.Hashear("clave123"),
            Empleado = new Empleado { Id = 10, Activo = true, Rol = new Rol { Permisos = Permiso.Vender } }
        };
        usuarios.Datos.Add(cajero);
        sesionCajero.Iniciar(cajero);

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashClave = hasheador.Hashear(ClaveSupervisor),
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Supervisor",
                    Permisos = Permiso.AutorizarOperacionPos | Permiso.AnularVenta
                }
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new IntentosAccesoEnMemoria(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesionCajero,
            new RelojFijo(EscenarioInventario.Ahora));

        var empAutorizado = await servicioAuth.ValidarSupervisorAsync(
            "super1", PinSupervisor, Permiso.AutorizarOperacionPos);

        Assert.NotNull(empAutorizado);
        Assert.Equal(20, empAutorizado.Id);
        // La sesión activa debe seguir perteneciendo al cajero:
        Assert.Equal("cajero1", sesionCajero.Usuario?.NombreUsuario);
    }

    [Fact]
    public async Task ValidarSupervisor_ConPinInvalido_LanzaCredencialesInvalidas()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesion = new SesionActual();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol { Permisos = Permiso.AutorizarOperacionPos }
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new IntentosAccesoEnMemoria(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesion,
            new RelojFijo(EscenarioInventario.Ahora));

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            servicioAuth.ValidarSupervisorAsync("super1", "0000", Permiso.AutorizarOperacionPos));
    }

    [Fact]
    public async Task ValidarSupervisor_SinPermisoRequerido_LanzaAccesoDenegado()
    {
        var hasheador = new HasheadorFalso();
        var usuarios = new UsuariosEnMemoria();
        var sesion = new SesionActual();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.MaxIntentosFallidos] = "3";

        var supervisor = new Usuario
        {
            Id = 2,
            NombreUsuario = "super1",
            HashPin = hasheador.Hashear(PinSupervisor),
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol { Permisos = Permiso.Vender } // No tiene AutorizarOperacionPos
            }
        };
        usuarios.Datos.Add(supervisor);

        var servicioAuth = new ServicioAutenticacion(
            usuarios,
            new IntentosAccesoEnMemoria(),
            new AuditoriaFalsa(),
            parametros,
            hasheador,
            sesion,
            new RelojFijo(EscenarioInventario.Ahora));

        await Assert.ThrowsAsync<AccesoDenegadoException>(() =>
            servicioAuth.ValidarSupervisorAsync("super1", PinSupervisor, Permiso.AutorizarOperacionPos));
    }

    [Fact]
    public async Task RegistrarRetiro_ConMontoValido_ReduceEfectivoEsperado()
    {
        var turnos = new TurnosEnMemoria();
        var ventas = new VentasEnMemoria();
        var cajas = new CajasEnMemoria();
        var retiros = new RetirosCajaEnMemoria();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.ToleranciaArqueo] = "1000";

        cajas.Datos.Add(new Caja { Id = 1, Nombre = "Caja 1", Activa = true });

        var servicioCaja = new ServicioCaja(
            turnos,
            ventas,
            cajas,
            parametros,
            new AuditoriaFalsa(),
            Sesiones.Con(Permiso.AbrirCerrarTurno),
            new RelojFijo(EscenarioInventario.Ahora),
            retiros);

        var turno = await servicioCaja.AbrirTurnoAsync(1, 200000); // Base $200.000

        var retiro = await servicioCaja.RegistrarRetiroAsync(150000, "Recogida de seguridad a caja fuerte", supervisorId: 99);

        Assert.Equal(150000, retiro.Monto);
        Assert.Single(turno.Retiros);
        Assert.Equal(50000, turno.CalcularEfectivoEsperado([])); // 200.000 - 150.000 = 50.000

        var resultado = await servicioCaja.CerrarTurnoAsync(50000);
        Assert.Equal(50000, resultado.Esperado);
        Assert.Equal(0, resultado.Diferencia);
        Assert.True(resultado.DentroDeTolerancia);
    }

    [Fact]
    public async Task RegistrarRetiro_MayorAEfectivoDisponible_LanzaValidacionException()
    {
        var turnos = new TurnosEnMemoria();
        var ventas = new VentasEnMemoria();
        var cajas = new CajasEnMemoria();
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.ToleranciaArqueo] = "1000";

        cajas.Datos.Add(new Caja { Id = 1, Nombre = "Caja 1", Activa = true });

        var servicioCaja = new ServicioCaja(
            turnos,
            ventas,
            cajas,
            parametros,
            new AuditoriaFalsa(),
            Sesiones.Con(Permiso.AbrirCerrarTurno),
            new RelojFijo(EscenarioInventario.Ahora));

        await servicioCaja.AbrirTurnoAsync(1, 50000);

        await Assert.ThrowsAsync<ValidacionException>(() =>
            servicioCaja.RegistrarRetiroAsync(80000, "Excede saldo", supervisorId: 99));
    }

    [Fact]
    public async Task VentasEnEspera_SuspenderYRecuperar_RestauraLineasCorrectamente()
    {
        var repo = new VentasEnEsperaEnMemoria();
        var servicio = new ServicioVentas(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new AuditoriaFalsa(),
            Sesiones.Con(Permiso.Vender),
            new RelojFijo(EscenarioInventario.Ahora),
            repo);

        var lineas = new List<LineaVenta>
        {
            new("7701234567890", 2),
            new("7709876543210", 1)
        };

        var suspendida = await servicio.SuspenderVentaAsync(
            cajaId: 1,
            lineas: lineas,
            clienteId: null,
            identificadorCliente: "Cliente Canasta Roja",
            totalEstimado: 45000,
            nota: "Fue a traer más dinero");

        Assert.NotNull(suspendida);
        Assert.False(suspendida.Recuperada);

        var pendientes = await servicio.ObtenerVentasEnEsperaAsync(1);
        Assert.Single(pendientes);
        Assert.Equal("Cliente Canasta Roja", pendientes[0].IdentificadorCliente);
        Assert.Equal(3, pendientes[0].TotalItems);

        var (venta, lineasRecuperadas) = await servicio.RecuperarVentaAsync(suspendida.Id);
        Assert.True(venta.Recuperada);
        Assert.Equal(2, lineasRecuperadas.Count);
        Assert.Equal("7701234567890", lineasRecuperadas[0].CodigoBarras);
        Assert.Equal(2, lineasRecuperadas[0].Cantidad);

        // No debe poder recuperarse dos veces
        await Assert.ThrowsAsync<ValidacionException>(() =>
            servicio.RecuperarVentaAsync(suspendida.Id));
    }
}

public class ServicioVentasTests
{
    private class Escenario
    {
        public EscenarioInventario Inventario { get; } = new();
        public ProductosEnMemoria Productos { get; } = new();
        public VentasEnMemoria Ventas { get; } = new();
        public TurnosEnMemoria Turnos { get; } = new();
        public ClientesEnMemoria Clientes { get; } = new();
        public ParametrosFalsos Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public StockEstante Fila { get; }
        public ServicioVentas Servicio { get; }

        public Escenario(Permiso permisos, bool conTurno = true)
        {
            Productos.Datos.Add(new ProductoUnitario
            {
                Id = 1,
                CodigoBarras = "123",
                PrecioVenta = 11900,
                Impuesto = new Iva { PuntosBasicos = 1900 }
            });
            Fila = Inventario.AgregarEstante(1, 10, 20, 10);

            if (conTurno)
                Turnos.Datos.Add(TurnoCaja.Abrir(
                    1, 1, 0, EscenarioInventario.Ahora.UtcDateTime));

            Servicio = new ServicioVentas(
                Productos,
                Ventas,
                Turnos,
                Inventario.Servicio,
                Clientes,
                Parametros,
                Auditoria,
                Sesiones.Con(permisos),
                new RelojFijo(EscenarioInventario.Ahora));
        }
    }

    private static SolicitudVenta Solicitud(int cantidad, long efectivo)
        => new(
            [new LineaVenta("123", cantidad)],
            [new PagoEfectivo { Monto = efectivo }]);

    [Fact]
    public async Task VentaCompleta_DescuentaStockYGuardaUnaSolaVez()
    {
        var e = new Escenario(Permiso.Vender);

        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        Assert.Equal(23800L, venta.Total);
        Assert.Equal(3800L, venta.TotalIva);
        Assert.Equal(6200L, venta.Cambio);
        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(8, e.Fila.Cantidad);
        Assert.Equal(1, e.Ventas.Guardados);
    }

    [Fact]
    public async Task SinStockSuficiente_NoGuardaNada()
    {
        var e = new Escenario(Permiso.Vender);

        await Assert.ThrowsAsync<StockInsuficienteException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(11, 200000)));

        Assert.Equal(0, e.Ventas.Guardados);
    }

    [Fact]
    public async Task SinPermisoDeVender_SeNiega()
    {
        var e = new Escenario(Permiso.VerInventario);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000)));
    }

    [Fact]
    public async Task SinTurnoAbierto_NoSeVende()
    {
        var e = new Escenario(Permiso.Vender, conTurno: false);

        await Assert.ThrowsAsync<TurnoInvalidoException>(
            () => e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000)));
    }

    [Fact]
    public async Task CodigoDesconocido_SeRechaza()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("999", 1)],
            [new PagoEfectivo { Monto = 20000 }]);

        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.RegistrarVentaAsync(solicitud));
    }

    [Fact]
    public async Task Anulacion_DevuelveElStockYQuedaAuditada()
    {
        var e = new Escenario(Permiso.Vender | Permiso.AnularVenta);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        await e.Servicio.AnularVentaAsync(venta.Id, "cliente se arrepintió");

        Assert.Equal(EstadoVenta.Anulada, venta.Estado);
        Assert.Equal(10, e.Fila.Cantidad);
        var accion = e.Auditoria.Acciones.Single();
        Assert.Equal(AccionAuditoria.VentaAnulada, accion);
    }

    [Fact]
    public async Task Anulacion_ExigeElPermiso()
    {
        var e = new Escenario(Permiso.Vender);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(1, 20000));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.AnularVentaAsync(venta.Id, "motivo"));
    }

    [Fact]
    public async Task PagoConTarjeta_CompletaLaVenta()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoTarjeta { Monto = 11900, UltimosDigitos = "1234" }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(0L, venta.Cambio);
    }

    [Fact]
    public async Task PagoPorTransferencia_CompletaLaVenta()
    {
        var e = new Escenario(Permiso.Vender);
        var solicitud = new SolicitudVenta(
            [new LineaVenta("123", 1)],
            [new PagoTransferencia { Monto = 11900, Referencia = "TRX-1" }]);

        var venta = await e.Servicio.RegistrarVentaAsync(solicitud);

        Assert.Equal(EstadoVenta.Completada, venta.Estado);
        Assert.Equal(0L, venta.Cambio);
    }

    [Fact]
    public async Task AnularDosVeces_SoloDevuelveElStockUnaVez()
    {
        var e = new Escenario(Permiso.Vender | Permiso.AnularVenta);
        var venta = await e.Servicio.RegistrarVentaAsync(Solicitud(2, 30000));

        await e.Servicio.AnularVentaAsync(venta.Id, "motivo");
        await Assert.ThrowsAsync<VentaInvalidaException>(
            () => e.Servicio.AnularVentaAsync(venta.Id, "otra vez"));

        Assert.Equal(10, e.Fila.Cantidad);
        Assert.Single(
            e.Inventario.Movimientos.Datos,
            m => m.Tipo == TipoMovimiento.Devolucion);
    }
}
