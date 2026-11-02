using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly ISesionActual? _sesion;
    private readonly IServicioAutenticacion? _autenticacion;

    public PosViewModel PosVm { get; }
    public InventarioViewModel InventarioVm { get; }
    public CajaTurnoViewModel CajaTurnoVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public RegistroUsuarioViewModel RegistroClienteVm { get; }

    [ObservableProperty]
    public partial ViewModelBase PaginaActual { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsModuloPos))]
    [NotifyPropertyChangedFor(nameof(EsModuloInventario))]
    [NotifyPropertyChangedFor(nameof(EsModuloTurno))]
    [NotifyPropertyChangedFor(nameof(EsModuloDashboard))]
    [NotifyPropertyChangedFor(nameof(EsModuloClientes))]
    public partial string ModuloActivo { get; set; } = "POS";

    [ObservableProperty]
    public partial string TituloModulo { get; set; } = "Punto de Venta (POS Electrónico DIAN)";

    [ObservableProperty]
    public partial string SubtituloModulo { get; set; } = "Facturación de alta velocidad · Resolución DIAN 000165 de 2023";

    [ObservableProperty]
    public partial string NombreUsuarioActivo { get; set; } = "cajero_demo";

    [ObservableProperty]
    public partial string RolUsuarioActivo { get; set; } = "Cajero";

    [ObservableProperty]
    public partial bool TemaOscuroActivo { get; set; }

    public bool EsModuloPos => ModuloActivo == "POS";
    public bool EsModuloInventario => ModuloActivo == "Inventario";
    public bool EsModuloTurno => ModuloActivo == "Turno";
    public bool EsModuloDashboard => ModuloActivo == "Dashboard";
    public bool EsModuloClientes => ModuloActivo == "Clientes";

    public event Action? SesionCerrada;

    public ShellViewModel()
    {
        PosVm = new PosViewModel();
        InventarioVm = new InventarioViewModel();
        CajaTurnoVm = new CajaTurnoViewModel();
        DashboardVm = new DashboardViewModel();
        RegistroClienteVm = new RegistroUsuarioViewModel();
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        PaginaActual = PosVm;
    }

    public ShellViewModel(
        ISesionActual sesion,
        IServicioAutenticacion autenticacion,
        PosViewModel posVm,
        InventarioViewModel inventarioVm,
        CajaTurnoViewModel cajaTurnoVm,
        DashboardViewModel dashboardVm,
        RegistroUsuarioViewModel registroClienteVm)
    {
        _sesion = sesion;
        _autenticacion = autenticacion;
        PosVm = posVm;
        InventarioVm = inventarioVm;
        CajaTurnoVm = cajaTurnoVm;
        DashboardVm = dashboardVm;
        RegistroClienteVm = registroClienteVm;
        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        PaginaActual = PosVm;
    }

    public async Task InicializarSesionAsync()
    {
        if (_sesion?.Usuario is not null)
        {
            NombreUsuarioActivo = _sesion.Usuario.NombreUsuario;
            RolUsuarioActivo = _sesion.Usuario.Empleado?.Rol?.Nombre ?? "Operador";
        }

        await PosVm.InicializarEstadoCajaAsync();
    }

    [RelayCommand]
    public async Task NavegarAsync(string modulo)
    {
        ModuloActivo = modulo;

        switch (modulo)
        {
            case "POS":
                TituloModulo = "Punto de Venta (POS Electrónico DIAN)";
                SubtituloModulo = "Facturación de alta velocidad · Resolución DIAN 000165 de 2023";
                PaginaActual = PosVm;
                await PosVm.InicializarEstadoCajaAsync();
                break;

            case "Inventario":
                TituloModulo = "Catálogo e Inventario FEFO";
                SubtituloModulo = "Control de existencias en estante, bodega, vencimientos y mermas";
                PaginaActual = InventarioVm;
                await InventarioVm.ActualizarListadoAsync();
                break;

            case "Turno":
                TituloModulo = "Control de Turno y Arqueo de Caja";
                SubtituloModulo = "Apertura con base inicial, Cierre Z y estado de numeración DIAN";
                PaginaActual = CajaTurnoVm;
                await CajaTurnoVm.VerificarEstadoCajaAsync();
                break;

            case "Clientes":
                TituloModulo = "Clientes y Cumplimiento Habeas Data";
                SubtituloModulo = "Registro de adquirentes DIAN, validación NIT (Módulo 11) y Ley 1581 de 2012";
                RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
                PaginaActual = RegistroClienteVm;
                break;

            case "Dashboard":
                TituloModulo = "Tablero Ejecutivo y Financiero";
                SubtituloModulo = "Métricas operativas, IVA generado, alertas y desempeño del turno";
                PaginaActual = DashboardVm;
                await DashboardVm.ActualizarDashboardAsync();
                break;
        }
    }

    [RelayCommand]
    private void AlternarTema()
    {
        TemaOscuroActivo = !TemaOscuroActivo;
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = TemaOscuroActivo
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }

    [RelayCommand]
    private void CerrarSesion()
    {
        _autenticacion?.CerrarSesion();
        SesionCerrada?.Invoke();
    }
}
