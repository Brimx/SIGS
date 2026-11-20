using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Xunit;
using Supermercado.Entidades.Configuracion;
using Supermercado.Pruebas.Falsos;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;

namespace Supermercado.Pruebas;

public class SeguridadTests
{
    [Fact]
    public void Usuario_SeBloqueaAlLlegarAlMaximoDeIntentos()
    {
        var usuario = new Usuario();

        usuario.RegistrarFallo(2);
        Assert.False(usuario.Bloqueado);

        usuario.RegistrarFallo(2);
        Assert.True(usuario.Bloqueado);
    }

    [Fact]
    public void Usuario_DesbloquearReiniciaLosIntentos()
    {
        var usuario = new Usuario { Bloqueado = true, IntentosFallidos = 5 };

        usuario.Desbloquear();

        Assert.False(usuario.Bloqueado);
        Assert.Equal(0, usuario.IntentosFallidos);
    }

    [Fact]
    public void Rol_ExigeTodosLosPermisosPedidos()
    {
        var cajero = new Rol
        {
            Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno
        };

        Assert.True(cajero.Tiene(Permiso.Vender));
        Assert.False(cajero.Tiene(Permiso.AnularVenta));
        Assert.False(cajero.Tiene(Permiso.Vender | Permiso.AnularVenta));
    }

    [Fact]
    public void Sesion_SinPermisoLanzaAccesoDenegado()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);

        Assert.Throws<AccesoDenegadoException>(
            () => sesion.Exigir(Permiso.VerFinanzas));
    }

    [Fact]
    public void Sesion_SinIniciarNoTienePermisos()
    {
        Assert.False(new SesionActual().Tiene(Permiso.Vender));
    }

    [Fact]
    public void Sesion_EmpleadoInactivo_CierraLaSesion()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);
        sesion.Usuario!.Empleado!.Activo = false;

        Assert.False(sesion.Tiene(Permiso.Vender));
        Assert.False(sesion.EstaAutenticada);
    }

    [Fact]
    public void Sesion_ConCambioDeClavePendiente_ExigeCambiarla()
    {
        var sesion = Falsos.Sesiones.Con(Permiso.Vender);
        sesion.Usuario!.DebeCambiarClave = true;

        Assert.True(sesion.DebeCambiarClave);
        Assert.Throws<DebeCambiarClaveException>(
            () => sesion.Exigir(Permiso.Vender));
    }

    [Fact]
    public void Usuario_RechazaUnMaximoDeIntentosNoPositivo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Usuario().RegistrarFallo(0));
    }

    [Fact]
    public void Usuario_RechazaUnaClaveVacia()
    {
        Assert.Throws<ArgumentException>(
            () => new Usuario().CambiarClave("  "));
    }

    [Fact]
    public void Bcrypt_GeneraHasheadosVerificables()
    {
        var hasheador = new HasheadorBCrypt();
        var hash = hasheador.Hashear("clave-segura-1");

        Assert.NotEqual("clave-segura-1", hash);
        Assert.True(hasheador.Verificar("clave-segura-1", hash));
        Assert.False(hasheador.Verificar("otra-clave", hash));
    }
}

public class ServicioAutenticacionTests
{
    private const string Clave = "clave-segura-1";

    private class Escenario
    {
        public UsuariosEnMemoria Usuarios { get; } = new();
        public SesionActual Sesion { get; } = new();
        public ServicioAutenticacion Servicio { get; }
        public Usuario Ana { get; }

        public Escenario(int maximoIntentos = 3)
        {
            var hasheador = new HasheadorFalso();
            var parametros = new ParametrosFalsos();
            parametros.Valores[ClavesParametro.MaxIntentosFallidos]
                = maximoIntentos.ToString();

            Ana = new Usuario
            {
                Id = 1,
                NombreUsuario = "ana",
                HashClave = hasheador.Hashear(Clave),
                Empleado = new Empleado
                {
                    Activo = true,
                    Rol = new Rol { Permisos = Permiso.Vender }
                }
            };
            Usuarios.Datos.Add(Ana);

            Servicio = new ServicioAutenticacion(
                Usuarios,
                new IntentosAccesoEnMemoria(),
                new AuditoriaFalsa(),
                parametros,
                hasheador,
                Sesion,
                new RelojFijo(EscenarioInventario.Ahora));
        }
    }

    [Fact]
    public async Task ClaveCorrecta_IniciaSesion()
    {
        var e = new Escenario();

        await e.Servicio.IniciarSesionAsync("ana", Clave);

        Assert.True(e.Sesion.EstaAutenticada);
        Assert.True(e.Sesion.Tiene(Permiso.Vender));
    }

    [Fact]
    public async Task ElUsuarioSeBloqueaTrasLosIntentosConfigurados()
    {
        var e = new Escenario(maximoIntentos: 3);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-1"));
        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-2"));
        await Assert.ThrowsAsync<CuentaBloqueadaException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala-3"));

        // Ya bloqueada, ni la clave correcta entra.
        await Assert.ThrowsAsync<CuentaBloqueadaException>(
            () => e.Servicio.IniciarSesionAsync("ana", Clave));
        Assert.False(e.Sesion.EstaAutenticada);
    }

    [Fact]
    public async Task UnExitoReiniciaElContadorDeFallos()
    {
        var e = new Escenario(maximoIntentos: 3);
        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", "mala"));

        await e.Servicio.IniciarSesionAsync("ana", Clave);

        Assert.Equal(0, e.Ana.IntentosFallidos);
    }

    [Fact]
    public async Task UsuarioInexistente_NoRevelaQueNoExiste()
    {
        var e = new Escenario();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("nadie", Clave));
    }

    [Fact]
    public async Task EmpleadoInactivo_NoEntraYCuentaElIntento()
    {
        var e = new Escenario();
        e.Ana.Empleado!.Activo = false;

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => e.Servicio.IniciarSesionAsync("ana", Clave));

        Assert.False(e.Sesion.EstaAutenticada);
        Assert.Equal(1, e.Ana.IntentosFallidos);
    }

    [Fact]
    public async Task PinCorrecto_IniciaSesion()
    {
        var e = new Escenario();
        e.Ana.HashPin = "h:1234";

        await e.Servicio.IniciarSesionConPinAsync("ana", "1234");

        Assert.True(e.Sesion.EstaAutenticada);
        Assert.True(e.Sesion.Tiene(Permiso.Vender));
    }

    [Fact]
    public async Task CambiarClave_LimpiaElCambioObligatorio()
    {
        var e = new Escenario();
        e.Ana.DebeCambiarClave = true;
        await e.Servicio.IniciarSesionAsync("ana", Clave);

        await e.Servicio.CambiarClaveAsync(Clave, "nueva-clave-1");

        Assert.False(e.Ana.DebeCambiarClave);
        Assert.True(new HasheadorFalso()
            .Verificar("nueva-clave-1", e.Ana.HashClave));
    }
}

public class ServicioParametrosTests
{
    private class Escenario
    {
        public ParametrosEnMemoria Parametros { get; } = new();
        public AuditoriaFalsa Auditoria { get; } = new();
        public ServicioParametros Servicio { get; }

        public Escenario(Permiso permisos = Permiso.GestionarPersonal)
            => Servicio = new ServicioParametros(
                Parametros,
                Auditoria,
                Sesiones.Con(permisos));
    }

    [Fact]
    public async Task Guardar_ConPermiso_AuditaYGuarda()
    {
        var e = new Escenario();

        await e.Servicio.GuardarAsync("clave.prueba", "42");

        Assert.Equal("42", await e.Servicio.ObtenerTextoAsync("clave.prueba"));
        Assert.Equal(1, e.Parametros.Guardados);
        Assert.Equal(
            AccionAuditoria.ParametroCambiado,
            Assert.Single(e.Auditoria.Acciones));
    }

    [Fact]
    public async Task Guardar_SinPermiso_SeNiega()
    {
        var e = new Escenario(Permiso.Vender);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => e.Servicio.GuardarAsync("clave.prueba", "42"));

        Assert.Empty(e.Parametros.Datos);
        Assert.Equal(0, e.Parametros.Guardados);
    }

    [Fact]
    public async Task GuardarRespaldo_NoExigePermisoNiAudita()
    {
        var servicio = new ServicioParametros(
            new ParametrosEnMemoria(),
            new AuditoriaFalsa(),
            new SesionActual());

        await servicio.GuardarRespaldoAsync(new Dictionary<string, string>
        {
            [ClavesParametro.TrmUltimaCentavos] = "401234",
            [ClavesParametro.TrmUltimaFecha] = "2026-10-17"
        });

        Assert.Equal(
            "401234",
            await servicio.ObtenerTextoAsync(ClavesParametro.TrmUltimaCentavos));
        Assert.Equal(
            "2026-10-17",
            await servicio.ObtenerTextoAsync(ClavesParametro.TrmUltimaFecha));
    }

    [Fact]
    public async Task ParametroFaltante_LanzaLaExcepcionDelCatalogo()
    {
        var parametros = new ParametrosFalsos();

        await Assert.ThrowsAsync<ParametroFaltanteException>(
            () => parametros.ObtenerTextoAsync("no.existe"));
    }
}

public class ConsultasLecturaTests
{
    [Fact]
    public async Task ConsultaAuditoria_FiltraYPagina()
    {
        var registros = new RegistrosAuditoriaEnMemoria();
        var ahora = new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 1,
            Fecha = ahora.AddHours(-3),
            Accion = AccionAuditoria.InicioSesion,
            EmpleadoId = 1
        });
        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 2,
            Fecha = ahora.AddHours(-2),
            Accion = AccionAuditoria.VentaAnulada,
            EmpleadoId = 2
        });
        await registros.AgregarAsync(new RegistroAuditoria
        {
            Id = 3,
            Fecha = ahora.AddHours(-1),
            Accion = AccionAuditoria.VentaAnulada,
            EmpleadoId = 2
        });

        var servicio = new ServicioAuditoria(
            registros,
            Sesiones.Con(Permiso.VerAuditoria),
            TimeProvider.System);

        var filtro = new FiltroAuditoria(
            Accion: AccionAuditoria.VentaAnulada,
            EmpleadoId: 2,
            Pagina: 1,
            TamanoPagina: 10);

        var resultado = await servicio.ConsultarAsync(filtro);

        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(3, resultado.Items[0].Id); // Orden descendente
    }

    [Fact]
    public async Task ConsultaAuditoria_SinPermiso_LanzaAccesoDenegado()
    {
        var registros = new RegistrosAuditoriaEnMemoria();
        var servicio = new ServicioAuditoria(
            registros,
            Sesiones.Con(Permiso.Vender),
            TimeProvider.System);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.ConsultarAsync(new FiltroAuditoria()));
    }

    [Fact]
    public async Task ConsultaInventario_Consolidado_CalculaEstanteYBodega()
    {
        var productos = new ProductosEnMemoria();
        var lotes = new LotesEnMemoria();
        var estante = new StockEstanteEnMemoria();
        var bodega = new StockBodegaEnMemoria();
        var movs = new MovimientosEnMemoria();

        var impuesto = new Iva { Nombre = "IVA 0", PuntosBasicos = 0 };
        var prod = new ProductoUnitario
        {
            Id = 1,
            Nombre = "Harina",
            CodigoBarras = "999",
            Impuesto = impuesto
        };
        await productos.AgregarAsync(prod);

        var lote = new Lote
        {
            Id = 10,
            ProductoId = 1,
            NumeroLote = "LOT-H",
            FechaVencimiento = new DateOnly(2027, 1, 1)
        };
        await lotes.AgregarAsync(lote);

        var se = new StockEstante { ProductoId = 1, LoteId = 10, Lote = lote };
        se.Aumentar(7);
        await estante.AgregarAsync(se);

        var sb = new StockBodega { ProductoId = 1, LoteId = 10, Lote = lote };
        sb.Aumentar(13);
        await bodega.AgregarAsync(sb);

        var servicio = new ServicioInventario(
            estante,
            bodega,
            movs,
            null!,
            null!,
            TimeProvider.System,
            productos: productos,
            lotes: lotes,
            sesion: Sesiones.Con(Permiso.VerInventario));

        var consolidado = await servicio.ObtenerStockConsolidadoAsync(1);

        var item = Assert.Single(consolidado);
        Assert.Equal("Harina", item.NombreProducto);
        Assert.Equal(7, item.CantidadEstante);
        Assert.Equal(13, item.CantidadBodega);
        Assert.Equal(20, item.TotalStock);
    }

    [Fact]
    public async Task ConsultaInventario_Kardex_CalculaSaldosCronologicos()
    {
        var productos = new ProductosEnMemoria();
        var lotes = new LotesEnMemoria();
        var estante = new StockEstanteEnMemoria();
        var bodega = new StockBodegaEnMemoria();
        var movs = new MovimientosEnMemoria();

        var lote = new Lote { Id = 1, NumeroLote = "L1", ProductoId = 1 };
        await lotes.AgregarAsync(lote);

        var fechaBase = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 1,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = 50,
            Fecha = fechaBase
        });
        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 2,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Venta,
            Cantidad = 10,
            Fecha = fechaBase.AddHours(2)
        });
        await movs.AgregarAsync(new MovimientoInventario
        {
            Id = 3,
            ProductoId = 1,
            LoteId = 1,
            Tipo = TipoMovimiento.Devolucion,
            Cantidad = 2,
            Fecha = fechaBase.AddHours(4)
        });

        var servicio = new ServicioInventario(
            estante,
            bodega,
            movs,
            null!,
            null!,
            TimeProvider.System,
            productos: productos,
            lotes: lotes,
            sesion: Sesiones.Con(Permiso.VerInventario));

        var kardex = await servicio.ObtenerKardexAsync(
            1,
            fechaBase.AddHours(1),
            fechaBase.AddDays(1));

        Assert.Equal(2, kardex.Count);
        Assert.Equal(40, kardex[0].SaldoPosterior);
        Assert.Equal(42, kardex[1].SaldoPosterior);
    }

    [Fact]
    public async Task BusquedaProductos_FiltraPorNombreYActivo()
    {
        var productos = new ProductosEnMemoria();
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 1,
            Nombre = "Arroz Diana",
            Activo = true
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 2,
            Nombre = "Arroz Roa",
            Activo = true
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 3,
            Nombre = "Arroz Florhuila",
            Activo = false
        });
        await productos.AgregarAsync(new ProductoUnitario
        {
            Id = 4,
            Nombre = "Aceite Diana",
            Activo = true
        });

        var servicio = new ServicioProductos(productos);

        var resultados = await servicio.BuscarPorNombreAsync("arroz", 1);
        Assert.Single(resultados);

        var todosArroces = await servicio.BuscarPorNombreAsync("arroz", 10);
        Assert.Equal(2, todosArroces.Count);
        Assert.DoesNotContain(todosArroces, p => !p.Activo);

        var vacio = await servicio.BuscarPorNombreAsync("   ");
        Assert.Empty(vacio);
    }
}
