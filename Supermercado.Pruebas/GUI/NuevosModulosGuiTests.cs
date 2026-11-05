using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.ViewModels;
using Supermercado.Logica.Seguridad;
using Supermercado.Pruebas.Falsos;
using Xunit;

namespace Supermercado.Pruebas.GUI;

public class NuevosModulosGuiTests
{
    [Fact]
    public void ShellViewModel_CajeroPuro_ActivaModoPantallaCompletaSinSidebar()
    {
        var sesionCajero = new SesionActual();
        var cajero = new Usuario
        {
            Id = 1,
            NombreUsuario = "cajero1",
            Empleado = new Empleado
            {
                Id = 10,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Cajero",
                    Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno | Permiso.GestionarClientes
                }
            }
        };
        sesionCajero.Iniciar(cajero);

        var shellVm = new ShellViewModel(sesionCajero, new PosViewModel(), new InventarioViewModel(), new CajaTurnoViewModel(), new DashboardViewModel(), new RegistroUsuarioViewModel());
        shellVm.AplicarPermisosDeNavegacion(sesionCajero);

        Assert.True(shellVm.PuedeVerPos);
        Assert.True(shellVm.PuedeVerTurno);
        Assert.False(shellVm.PuedeVerCompras);
        Assert.False(shellVm.PuedeVerPersonal);
        Assert.False(shellVm.PuedeVerContabilidad);
        Assert.False(shellVm.PuedeVerDashboard);

        // Al ser cajero puro sin módulos de back-office, la barra lateral se oculta
        Assert.False(shellVm.MostrarBarraLateral);
        Assert.True(shellVm.EsModoCajeroPantallaCompleta);
        Assert.Equal(0, shellVm.AnchoColumnaSidebar.Value);
    }

    [Fact]
    public void ShellViewModel_SupervisorYAdmin_MantieneSidebarVisible()
    {
        var sesionAdmin = new SesionActual();
        var admin = new Usuario
        {
            Id = 2,
            NombreUsuario = "admin",
            Empleado = new Empleado
            {
                Id = 20,
                Activo = true,
                Rol = new Rol
                {
                    Nombre = "Admin",
                    Permisos = Enum.GetValues<Permiso>().Aggregate(Permiso.Ninguno, (acc, p) => acc | p)
                }
            }
        };
        sesionAdmin.Iniciar(admin);

        var shellVm = new ShellViewModel(sesionAdmin, new PosViewModel(), new InventarioViewModel(), new CajaTurnoViewModel(), new DashboardViewModel(), new RegistroUsuarioViewModel());
        shellVm.AplicarPermisosDeNavegacion(sesionAdmin);

        Assert.True(shellVm.PuedeVerPos);
        Assert.True(shellVm.PuedeVerCompras);
        Assert.True(shellVm.PuedeVerPersonal);
        Assert.True(shellVm.PuedeVerContabilidad);
        Assert.True(shellVm.PuedeVerDashboard);

        Assert.True(shellVm.MostrarBarraLateral);
        Assert.False(shellVm.EsModoCajeroPantallaCompleta);
        Assert.Equal(240, shellVm.AnchoColumnaSidebar.Value);
    }

    [Fact]
    public void PosViewModel_PagosMixtos_AgregaYRemueveRenglonesCorrectamente()
    {
        var posVm = new PosViewModel
        {
            MetodoPagoSeleccionado = "Efectivo",
            EfectivoEntregadoPesos = 50000
        };

        posVm.AgregarRenglonPago();
        Assert.Single(posVm.PagosAgregados);
        Assert.Equal("Efectivo", posVm.PagosAgregados[0].Metodo);
        Assert.Equal(50000, posVm.PagosAgregados[0].Monto);

        posVm.MetodoPagoSeleccionado = "Tarjeta";
        posVm.EfectivoEntregadoPesos = 30000;
        posVm.UltimosDigitosTarjeta = "4321";
        posVm.AgregarRenglonPago();

        Assert.Equal(2, posVm.PagosAgregados.Count);
        Assert.Equal(80000, posVm.TotalAbonadoPesos);

        posVm.QuitarRenglonPago(posVm.PagosAgregados[0]);
        Assert.Single(posVm.PagosAgregados);
        Assert.Equal(30000, posVm.TotalAbonadoPesos);
    }

    [Fact]
    public void LoginViewModel_ComandosPin_ModificanPinAcceso()
    {
        var loginVm = new LoginViewModel();
        loginVm.AgregarDigitoPinCommand.Execute("1");
        loginVm.AgregarDigitoPinCommand.Execute("2");
        loginVm.AgregarDigitoPinCommand.Execute("3");
        loginVm.AgregarDigitoPinCommand.Execute("4");

        Assert.Equal("1234", loginVm.PinAcceso);

        loginVm.BorrarDigitoPinCommand.Execute(null);
        Assert.Equal("123", loginVm.PinAcceso);

        loginVm.LimpiarPinCommand.Execute(null);
        Assert.Equal(string.Empty, loginVm.PinAcceso);
    }

    [Theory]
    [InlineData("admin", "admin", "Admin123*", "1111")]
    [InlineData("cajero", "cajero", "Cajero123*", "1234")]
    [InlineData("gerente", "gerente", "Gerente123*", "3344")]
    [InlineData("bodega", "bodega", "Bodega123*", "2233")]
    public void LoginViewModel_SeleccionarCredencialDemo_Carga4RolesCorrectamente(
        string rolKey, string usuarioEsperado, string claveEsperada, string pinEsperado)
    {
        var loginVm = new LoginViewModel();
        loginVm.SeleccionarCredencialDemoCommand.Execute(rolKey);

        Assert.Equal(usuarioEsperado, loginVm.NombreUsuario);
        Assert.Equal(claveEsperada, loginVm.Clave);
        Assert.Equal(pinEsperado, loginVm.PinAcceso);
        Assert.True(loginVm.TieneExito);
        Assert.False(loginVm.TieneError);
    }
}
