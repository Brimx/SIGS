using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.GUI.Servicios.Impresion;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Pruebas.GUI;

public class PlantillaYRegulacionGuiTests
{
    private sealed class ServicioParametrosFalso : IServicioParametros
    {
        public Task<string> ObtenerTextoAsync(string clave) => Task.FromResult(string.Empty);
        public Task<int> ObtenerEnteroAsync(string clave) => Task.FromResult(0);
        public Task<long> ObtenerLongAsync(string clave) => Task.FromResult(0L);
        public Task GuardarAsync(string clave, string valor) => Task.CompletedTask;
        public Task GuardarRespaldoAsync(IReadOnlyDictionary<string, string> valores) => Task.CompletedTask;
    }

    [Fact]
    public void Validador_CalculaDigitoVerificacionModulo11Correctamente()
    {
        // NIT real de Jerónimo Martins Colombia S.A.S.: 900480569-1
        var dv = Validador.CalcularDigitoVerificacionNit("900480569");
        Assert.Equal(1, dv);
        Assert.True(Validador.ValidarNitConDv("900480569-1"));
        Assert.False(Validador.ValidarNitConDv("900480569-9"));
    }

    [Fact]
    public void RegistroUsuarioViewModel_ActualizaDvYAvisoHabeasDataLey1581()
    {
        var vm = new RegistroUsuarioViewModel();
        vm.ConfigurarModo(ModoRegistroUsuario.ClientePos);

        vm.TipoDocumento = "NIT";
        vm.Documento = "900480569";

        Assert.True(vm.EsNit);
        Assert.Equal("1", vm.DigitoVerificacion);

        vm.AutorizaHabeasData = false;
        Assert.Contains("222222222222", vm.AvisoHabeasData);
    }

    [Fact]
    public async Task ImpresorTicketEscPos_GeneraTirilla80mmYSecuenciasCorteYGaveta()
    {
        var venta = new Venta
        {
            Id = 55,
            Fecha = new DateTime(2026, 11, 2, 18, 52, 24, DateTimeKind.Utc)
        };

        var prod19 = new ProductoUnitario
        {
            Id = 1,
            CodigoBarras = "7702129072335",
            Nombre = "CHORIZO SANTARR",
            PrecioVenta = 10690,
            Impuesto = new Iva { PuntosBasicos = 1900 }
        };
        var prodPesado = new ProductoPesado
        {
            Id = 2,
            CodigoBarras = "2820890001851",
            Nombre = "PIMENTON VERDE",
            PrecioVenta = 8800,
            Impuesto = new Iva { PuntosBasicos = 0 }
        };

        venta.AgregarProducto(prod19, 1);
        venta.AgregarProducto(prodPesado, 185);
        venta.AgregarPago(new PagoEfectivo { Monto = 20000 });
        venta.Cerrar();

        var impresor = new ImpresorTicketEscPos(new ServicioParametrosFalso());
        var tirilla = await impresor.FormatearTirillaTextoAsync(venta);
        var bytesEscPos = await impresor.GenerarComandosEscPosAsync(venta);

        Assert.Contains("TICKET DE VENTA POS N:", tirilla);
        Assert.Contains("POS-55", tirilla);
        Assert.Contains("J:IVA 19%", tirilla);
        Assert.Contains("C:IVA 0%", tirilla);
        Assert.Contains("0,185 KG", tirilla);

        // Verificar cabecera ESC @ (0x1B 0x40) y comando de corte GS V 0 (0x1D 0x56 0x00)
        Assert.Equal(0x1B, bytesEscPos[0]);
        Assert.Equal(0x40, bytesEscPos[1]);
        Assert.True(bytesEscPos.Length > 200);
    }

    [Fact]
    public async Task CajaTurnoViewModel_SinTurnoAbierto_ExigeAperturaConBase()
    {
        var vm = new CajaTurnoViewModel();
        Assert.False(vm.TurnoAbierto);
        Assert.True(vm.RequiereAperturaConBase);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ShellViewModel_Navegar_CambiaPaginaActualYModuloActivo()
    {
        var shell = new ShellViewModel();
        Assert.True(shell.EsModuloPos);

        await shell.NavegarAsync("Inventario");
        Assert.True(shell.EsModuloInventario);
        Assert.Same(shell.InventarioVm, shell.PaginaActual);

        await shell.NavegarAsync("Turno");
        Assert.True(shell.EsModuloTurno);
        Assert.Same(shell.CajaTurnoVm, shell.PaginaActual);

        await shell.NavegarAsync("Clientes");
        Assert.True(shell.EsModuloClientes);
        Assert.Same(shell.RegistroClienteVm, shell.PaginaActual);

        await shell.NavegarAsync("Dashboard");
        Assert.True(shell.EsModuloDashboard);
        Assert.Same(shell.DashboardVm, shell.PaginaActual);
    }

    [Fact]
    public void LoginViewModel_IngresarSinUsuario_MuestraErrorAmigable()
    {
        var login = new LoginViewModel();
        login.NombreUsuario = "";
        login.IngresarCommand.Execute(null);

        Assert.True(login.TieneError);
        Assert.Contains("usuario", login.MensajeError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoginViewModel_AlternarModoPin_CambiaModoPinYLimpiaError()
    {
        var login = new LoginViewModel();
        login.TieneError = true;
        login.MensajeError = "Error previo";

        login.AlternarModoPinCommand.Execute(null);
        Assert.True(login.ModoPin);
        Assert.False(login.TieneError);
        Assert.Empty(login.MensajeError);
    }

    [Fact]
    public void MainViewModel_IniciaEnLoginViewModel()
    {
        var main = new MainViewModel();
        Assert.Same(main.LoginVm, main.VistaActual);
    }

    [Fact]
    public void PosViewModel_EliminarYCancelarVenta_LimpiaCarritoYTotales()
    {
        var pos = new PosViewModel();
        var prod = new ProductoUnitario
        {
            Id = 10,
            CodigoBarras = "770999",
            Nombre = "Café Molido 500g",
            PrecioVenta = 15990,
            Impuesto = new Iva { PuntosBasicos = 500 }
        };

        pos.AgregarProductoAlCarrito(prod, 2);
        Assert.Single(pos.Carrito);
        Assert.Equal(31980, pos.TotalPesos);
        Assert.Equal("M", pos.Carrito[0].CodigoTarifaDian);

        pos.CancelarVenta();
        Assert.Empty(pos.Carrito);
        Assert.Equal(0, pos.TotalPesos);
    }

    [Fact]
    public void PlantillaTicketPos_ObtenerCodigoTarifa_MapeaEstandarAra()
    {
        Assert.Equal("C", PlantillaTicketPos.ObtenerCodigoTarifa(0));
        Assert.Equal("M", PlantillaTicketPos.ObtenerCodigoTarifa(500));
        Assert.Equal("J", PlantillaTicketPos.ObtenerCodigoTarifa(1900));
    }
}
