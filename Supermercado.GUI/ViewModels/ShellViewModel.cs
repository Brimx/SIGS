using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supermercado.Entidades.Seguridad;
using Supermercado.GUI.Servicios.Preferencias;
using Supermercado.GUI.Servicios.Tema;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly ISesionActual? _sesion;
    private readonly IServicioPreferencias? _preferencias;

    public PosViewModel PosVm { get; }
    public InventarioViewModel InventarioVm { get; }
    public CajaTurnoViewModel CajaTurnoVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public RegistroUsuarioViewModel RegistroClienteVm { get; }
    public PersonalRolesViewModel PersonalRolesVm { get; }
    public ComprasProveedoresViewModel ComprasProveedoresVm { get; }
    public ContabilidadDianViewModel ContabilidadDianVm { get; }

    [ObservableProperty]
    public partial ViewModelBase PaginaActual { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsModuloPos))]
    [NotifyPropertyChangedFor(nameof(EsModuloInventario))]
    [NotifyPropertyChangedFor(nameof(EsModuloTurno))]
    [NotifyPropertyChangedFor(nameof(EsModuloDashboard))]
    [NotifyPropertyChangedFor(nameof(EsModuloClientes))]
    [NotifyPropertyChangedFor(nameof(EsModuloPersonal))]
    [NotifyPropertyChangedFor(nameof(EsModuloCompras))]
    [NotifyPropertyChangedFor(nameof(EsModuloContabilidad))]
    public partial string ModuloActivo { get; set; } = "POS";

    [ObservableProperty]
    public partial string TituloModulo { get; set; } = "Punto de Venta (POS)";

    [ObservableProperty]
    public partial string SubtituloModulo { get; set; } = "Registro de ventas, balanzas EAN-13 y pagos mixtos";

    [ObservableProperty]
    public partial string NombreUsuarioActivo { get; set; } = "Sin sesión";

    [ObservableProperty]
    public partial string RolUsuarioActivo { get; set; } = "";

    [ObservableProperty]
    public partial string TemaActual { get; set; } = TemaSigs.ClaveClaro;

    // Filtrado Dinámico RBAC y Modo Pantalla Completa (Cajero Puro)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnchoColumnaSidebar))]
    [NotifyPropertyChangedFor(nameof(EsModoCajeroPantallaCompleta))]
    public partial bool MostrarBarraLateral { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerPos { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerTurno { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerClientes { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerInventario { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerCompras { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerPersonal { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerContabilidad { get; set; } = true;

    [ObservableProperty]
    public partial bool PuedeVerDashboard { get; set; } = true;

    public GridLength AnchoColumnaSidebar => MostrarBarraLateral
        ? new GridLength(240)
        : new GridLength(0);

    public bool EsModoCajeroPantallaCompleta => !MostrarBarraLateral;

    public bool EsModuloPos => ModuloActivo == "POS";
    public bool EsModuloInventario => ModuloActivo == "Inventario";
    public bool EsModuloTurno => ModuloActivo == "Turno";
    public bool EsModuloDashboard => ModuloActivo == "Dashboard";
    public bool EsModuloClientes => ModuloActivo == "Clientes";
    public bool EsModuloPersonal => ModuloActivo == "Personal";
    public bool EsModuloCompras => ModuloActivo == "Compras";
    public bool EsModuloContabilidad => ModuloActivo == "Contabilidad";

    public event Action? SesionCerrada;
    public event Action? ConfiguracionSolicitada;

    public ShellViewModel()
    {
        PosVm = new PosViewModel();
        InventarioVm = new InventarioViewModel();
        CajaTurnoVm = new CajaTurnoViewModel();
        DashboardVm = new DashboardViewModel();
        RegistroClienteVm = new RegistroUsuarioViewModel();
        PersonalRolesVm = new PersonalRolesViewModel();
        ComprasProveedoresVm = new ComprasProveedoresViewModel();
        ContabilidadDianVm = new ContabilidadDianViewModel();

        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        PaginaActual = PosVm;
        TemaActual = TemaSigs.ClaveDe(Application.Current?.RequestedThemeVariant ?? ThemeVariant.Light);
    }

    public ShellViewModel(
        ISesionActual sesion,
        PosViewModel posVm,
        InventarioViewModel inventarioVm,
        CajaTurnoViewModel cajaTurnoVm,
        DashboardViewModel dashboardVm,
        RegistroUsuarioViewModel registroClienteVm,
        PersonalRolesViewModel? personalRolesVm = null,
        ComprasProveedoresViewModel? comprasProveedoresVm = null,
        ContabilidadDianViewModel? contabilidadDianVm = null,
        IServicioPreferencias? preferencias = null)
    {
        _sesion = sesion;
        _preferencias = preferencias;
        TemaActual = preferencias?.LeerTema()
            ?? TemaSigs.ClaveDe(Application.Current?.RequestedThemeVariant ?? ThemeVariant.Light);
        PosVm = posVm;
        InventarioVm = inventarioVm;
        CajaTurnoVm = cajaTurnoVm;
        DashboardVm = dashboardVm;
        RegistroClienteVm = registroClienteVm;
        PersonalRolesVm = personalRolesVm ?? new PersonalRolesViewModel();
        ComprasProveedoresVm = comprasProveedoresVm ?? new ComprasProveedoresViewModel();
        ContabilidadDianVm = contabilidadDianVm ?? new ContabilidadDianViewModel();

        RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
        PaginaActual = PosVm;
    }

    public async Task InicializarSesionAsync()
    {
        if (_sesion?.Usuario is not null)
        {
            NombreUsuarioActivo = _sesion.Usuario.NombreUsuario;
            RolUsuarioActivo = _sesion.Usuario.Empleado?.Rol?.Nombre ?? "Operador";
            AplicarPermisosDeNavegacion(_sesion);
        }
        else
        {
            HabilitarTodosLosModulos();
        }

        var moduloInicial = DeterminarModuloInicial();
        await NavegarAsync(moduloInicial);
    }

    public void AplicarPermisosDeNavegacion(ISesionActual sesion)
    {
        PuedeVerPos = sesion.Tiene(Permiso.Vender);
        PuedeVerTurno = sesion.Tiene(Permiso.AbrirCerrarTurno);
        PuedeVerClientes = sesion.Tiene(Permiso.GestionarClientes) || sesion.Tiene(Permiso.Vender);
        PuedeVerInventario = sesion.Tiene(Permiso.VerInventario) || sesion.Tiene(Permiso.AjustarInventario);
        PuedeVerCompras = sesion.Tiene(Permiso.GestionarCompras) || sesion.Tiene(Permiso.Reabastecer) || sesion.Tiene(Permiso.GestionarProductos);
        PuedeVerPersonal = sesion.Tiene(Permiso.GestionarPersonal) || sesion.Tiene(Permiso.GestionarRoles);
        PuedeVerContabilidad = sesion.Tiene(Permiso.GestionarContabilidad) || sesion.Tiene(Permiso.VerFinanzas) || sesion.Tiene(Permiso.VerAuditoria);
        PuedeVerDashboard = sesion.Tiene(Permiso.VerReportes) || sesion.Tiene(Permiso.VerFinanzas) || sesion.Tiene(Permiso.VerAuditoria);

        var tieneModulosBackOffice = PuedeVerInventario || PuedeVerCompras || PuedeVerPersonal || PuedeVerContabilidad || PuedeVerDashboard;
        var esCajeroPuro = PuedeVerPos && !tieneModulosBackOffice && !sesion.Tiene(Permiso.AutorizarOperacionPos);

        // Si el rol es Cajero puro, se oculta la barra lateral y opera en pantalla completa POS
        MostrarBarraLateral = !esCajeroPuro;
    }

    private void HabilitarTodosLosModulos()
    {
        PuedeVerPos = true;
        PuedeVerTurno = true;
        PuedeVerClientes = true;
        PuedeVerInventario = true;
        PuedeVerCompras = true;
        PuedeVerPersonal = true;
        PuedeVerContabilidad = true;
        PuedeVerDashboard = true;
        MostrarBarraLateral = true;
    }

    private string DeterminarModuloInicial()
    {
        if (PuedeVerPos) return "POS";
        if (PuedeVerInventario) return "Inventario";
        if (PuedeVerCompras) return "Compras";
        if (PuedeVerContabilidad) return "Contabilidad";
        if (PuedeVerDashboard) return "Dashboard";
        if (PuedeVerPersonal) return "Personal";
        if (PuedeVerTurno) return "Turno";
        return "POS";
    }

    [RelayCommand]
    public async Task NavegarAsync(string modulo)
    {
        ModuloActivo = modulo;

        switch (modulo)
        {
            case "POS":
                TituloModulo = "Punto de Venta (POS)";
                SubtituloModulo = "Registro de ventas, balanzas EAN-13 y pagos mixtos";
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
                SubtituloModulo = "Apertura con base inicial, retiros parciales y Cierre Z";
                PaginaActual = CajaTurnoVm;
                await CajaTurnoVm.VerificarEstadoCajaAsync();
                break;

            case "Clientes":
                TituloModulo = "Clientes y Cumplimiento Habeas Data";
                SubtituloModulo = "Registro de clientes, validación NIT (Módulo 11) y Ley 1581 de 2012";
                RegistroClienteVm.ConfigurarModo(ModoRegistroUsuario.ClientePos);
                PaginaActual = RegistroClienteVm;
                break;

            case "Personal":
                TituloModulo = "Administración de Personal y Roles RBAC";
                SubtituloModulo = "Empleados, cargos, credenciales de usuario y permisos por rol";
                PaginaActual = PersonalRolesVm;
                await PersonalRolesVm.CargarDatosAsync();
                break;

            case "Compras":
                TituloModulo = "Compras, Proveedores y Reabastecimiento";
                SubtituloModulo = "Directorio de proveedores NIT, categorías y sugeridos automáticos por stock mínimo";
                PaginaActual = ComprasProveedoresVm;
                await ComprasProveedoresVm.CargarDatosAsync();
                break;

            case "Contabilidad":
                TituloModulo = "Finanzas y Reportes de IVA";
                SubtituloModulo = "Discriminación de IVA (0%, 5%, 19%), gastos operativos, cierres Z y reportes CSV/PDF";
                PaginaActual = ContabilidadDianVm;
                await ContabilidadDianVm.ActualizarConsolidadoFiscalAsync();
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
        var siguiente = TemaSigs.Siguiente(TemaActual);
        TemaActual = siguiente;
        TemaSigs.Aplicar(siguiente);
        _preferencias?.GuardarTema(siguiente);
    }

    [RelayCommand]
    private void AbrirConfiguracion() => ConfiguracionSolicitada?.Invoke();

    [RelayCommand]
    private void CerrarSesion()
    {
        _sesion?.Cerrar();
        SesionCerrada?.Invoke();
    }
}