using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Personal;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.ViewModels;

public partial class PermisoBanderaItemViewModel : ObservableObject
{
    public Permiso Valor { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;

    [ObservableProperty]
    public partial bool Activo { get; set; }
}

public sealed class EmpleadoListadoItemViewModel
{
    private static readonly CultureInfo CulturaCo = new("es-CO");

    public Empleado Empleado { get; init; } = null!;
    public Usuario? UsuarioAsociado { get; init; }

    public int Id => Empleado.Id;
    public string Documento => Empleado.Documento;
    public string NombreCompleto => Empleado.NombreCompleto;
    public string Correo => Empleado.Correo;
    public string NombreCargo => Empleado.Cargo?.Nombre ?? $"Cargo #{Empleado.CargoId}";
    public string NombreRol => Empleado.Rol?.Nombre ?? $"Rol #{Empleado.RolId}";
    public string SalarioFormateado => $"$ {Empleado.SalarioBase.ToString("N0", CulturaCo)}";
    public string EstadoEmpleado => Empleado.Activo ? "ACTIVO" : "INACTIVO";
    public string EstadoUsuario => UsuarioAsociado is null
        ? "Sin usuario"
        : UsuarioAsociado.Bloqueado
            ? $"{UsuarioAsociado.NombreUsuario} (BLOQUEADO)"
            : UsuarioAsociado.NombreUsuario;
    public bool TieneUsuario => UsuarioAsociado is not null;
    public bool UsuarioBloqueado => UsuarioAsociado?.Bloqueado == true;
}

public partial class PersonalRolesViewModel : ViewModelBase
{
    private readonly IServicioEmpleados? _servicioEmpleados;
    private readonly IServicioEstructura? _servicioEstructura;
    private readonly IServicioUsuarios? _servicioUsuarios;
    private readonly ITraductorExcepciones? _traductor;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ObservableCollection<EmpleadoListadoItemViewModel> Empleados { get; } = [];
    public ObservableCollection<Cargo> Cargos { get; } = [];
    public ObservableCollection<Rol> Roles { get; } = [];
    public ObservableCollection<PermisoBanderaItemViewModel> BanderasRolSeleccionado { get; } = [];

    [ObservableProperty]
    public partial EmpleadoListadoItemViewModel? EmpleadoSeleccionado { get; set; }

    [ObservableProperty]
    public partial Rol? RolSeleccionado { get; set; }

    // Formulario de Nuevo Empleado
    [ObservableProperty]
    public partial string DocumentoInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NombresInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ApellidosInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CorreoInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial long SalarioBaseInput { get; set; } = 1423500;

    [ObservableProperty]
    public partial Cargo? CargoFormularioSeleccionado { get; set; }

    [ObservableProperty]
    public partial Rol? RolFormularioSeleccionado { get; set; }

    // Formulario de Creación de Usuario para Empleado Seleccionado
    [ObservableProperty]
    public partial string NuevoUsuarioInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ClaveTemporalInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NuevoRolNombreInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NuevoCargoNombreInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MensajeEstado { get; set; } = "Gestión de personal, cargos y matriz RBAC de 18 permisos.";

    [ObservableProperty]
    public partial bool TieneError { get; set; }

    [ObservableProperty]
    public partial bool Cargando { get; set; }

    public PersonalRolesViewModel()
    {
        InicializarCatalogoBanderas(Permiso.Ninguno);
    }

    public PersonalRolesViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        InicializarCatalogoBanderas(Permiso.Ninguno);
    }

    public PersonalRolesViewModel(
        IServicioEmpleados servicioEmpleados,
        IServicioEstructura servicioEstructura,
        IServicioUsuarios? servicioUsuarios = null,
        ITraductorExcepciones? traductor = null)
    {
        _servicioEmpleados = servicioEmpleados;
        _servicioEstructura = servicioEstructura;
        _servicioUsuarios = servicioUsuarios;
        _traductor = traductor;
        InicializarCatalogoBanderas(Permiso.Ninguno);
    }

    partial void OnRolSeleccionadoChanged(Rol? value)
    {
        var mascara = value?.Permisos ?? Permiso.Ninguno;
        foreach (var item in BanderasRolSeleccionado)
        {
            item.Activo = (mascara & item.Valor) == item.Valor;
        }
    }

    private void InicializarCatalogoBanderas(Permiso mascaraActiva)
    {
        BanderasRolSeleccionado.Clear();
        foreach (var perm in Enum.GetValues<Permiso>())
        {
            if (perm == Permiso.Ninguno)
                continue;

            BanderasRolSeleccionado.Add(new PermisoBanderaItemViewModel
            {
                Valor = perm,
                Nombre = perm.ToString(),
                Descripcion = DescribirPermiso(perm),
                Activo = (mascaraActiva & perm) == perm
            });
        }
    }

    private static string DescribirPermiso(Permiso permiso) => permiso switch
    {
        Permiso.Vender => "Facturar en terminal POS",
        Permiso.AnularVenta => "Anular ventas cerradas y revertir inventario",
        Permiso.AbrirCerrarTurno => "Apertura de caja, retiros y Cierre Z",
        Permiso.VerInventario => "Consultar existencias FEFO y kárdex",
        Permiso.AjustarInventario => "Registrar bajas contables por merma",
        Permiso.Reabastecer => "Trasladar stock de bodega a estante",
        Permiso.GestionarProductos => "Crear/editar productos y precios",
        Permiso.VerReportes => "Generar reportes ejecutivos y de ventas",
        Permiso.VerFinanzas => "Consultar ingresos, gastos y margen",
        Permiso.GestionarPersonal => "Crear/editar empleados y nómina",
        Permiso.GestionarRoles => "Modificar matriz RBAC y restablecer claves",
        Permiso.GestionarTarifas => "Configurar tarifas de impuestos IVA",
        Permiso.VerAuditoria => "Inspeccionar bitácora de seguridad",
        Permiso.GestionarClientes => "Administrar clientes y Habeas Data",
        Permiso.AutorizarOperacionPos => "Autorizar anulaciones y retiros con PIN en caja",
        Permiso.GestionarCompras => "Órdenes de compra y proveedores",
        Permiso.GestionarContabilidad => "Reportes fiscales DIAN y exportación CSV/PDF",
        Permiso.MantenimientoSistema => "Respaldos y mantenimiento técnico",
        _ => permiso.ToString()
    };

    [RelayCommand]
    public async Task CargarDatosAsync()
    {
        if (Cargando)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var estructura = _servicioEstructura ?? servicios?.GetService<IServicioEstructura>();
        var servicioUsuarios = _servicioUsuarios ?? servicios?.GetService<IServicioUsuarios>();
        var repoEmpleados = servicios?.GetService<IRepositorio<Empleado>>();
        if (estructura is null)
            return;

        Cargando = true;
        TieneError = false;
        try
        {
            var listaCargos = await estructura.ObtenerCargosAsync();
            var listaRoles = await estructura.ObtenerRolesAsync();

            Cargos.Clear();
            foreach (var c in listaCargos)
                Cargos.Add(c);

            Roles.Clear();
            foreach (var r in listaRoles)
                Roles.Add(r);

            CargoFormularioSeleccionado ??= Cargos.FirstOrDefault();
            RolFormularioSeleccionado ??= Roles.FirstOrDefault();
            RolSeleccionado ??= Roles.FirstOrDefault();

            IReadOnlyList<Usuario> usuarios = [];
            if (servicioUsuarios is not null)
            {
                try
                {
                    usuarios = await servicioUsuarios.ObtenerTodosAsync();
                }
                catch
                {
                    // Si el rol actual no tiene permiso de listar usuarios, continúa con cargos/roles
                }
            }

            if (repoEmpleados is not null)
            {
                var todosEmpleados = await repoEmpleados.ObtenerTodosAsync();
                var cargosDict = listaCargos.ToDictionary(x => x.Id);
                var rolesDict = listaRoles.ToDictionary(x => x.Id);
                var usuariosPorEmp = usuarios.ToDictionary(u => u.EmpleadoId);

                Empleados.Clear();
                foreach (var emp in todosEmpleados)
                {
                    emp.Cargo ??= cargosDict.GetValueOrDefault(emp.CargoId);
                    emp.Rol ??= rolesDict.GetValueOrDefault(emp.RolId);
                    Empleados.Add(new EmpleadoListadoItemViewModel
                    {
                        Empleado = emp,
                        UsuarioAsociado = usuariosPorEmp.GetValueOrDefault(emp.Id)
                    });
                }
            }

            MensajeEstado = $"Cargados {Empleados.Count} empleados, {Roles.Count} roles y {Cargos.Count} cargos.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    public async Task RegistrarEmpleadoAsync()
    {
        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEmpleados = _servicioEmpleados ?? servicios?.GetService<IServicioEmpleados>();
        if (servicioEmpleados is null || CargoFormularioSeleccionado is null || RolFormularioSeleccionado is null)
            return;

        TieneError = false;
        try
        {
            var nuevo = new Empleado
            {
                Documento = DocumentoInput.Trim(),
                Nombres = NombresInput.Trim(),
                Apellidos = ApellidosInput.Trim(),
                Correo = CorreoInput.Trim(),
                SalarioBase = SalarioBaseInput,
                CargoId = CargoFormularioSeleccionado.Id,
                RolId = RolFormularioSeleccionado.Id,
                Activo = true
            };

            await servicioEmpleados.CrearAsync(nuevo);

            DocumentoInput = string.Empty;
            NombresInput = string.Empty;
            ApellidosInput = string.Empty;
            CorreoInput = string.Empty;

            await CargarDatosAsync();
            MensajeEstado = $"Empleado {nuevo.NombreCompleto} registrado exitosamente.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task CrearUsuarioParaEmpleadoAsync()
    {
        if (EmpleadoSeleccionado is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEmpleados = _servicioEmpleados ?? servicios?.GetService<IServicioEmpleados>();
        if (servicioEmpleados is null)
            return;

        TieneError = false;
        try
        {
            await servicioEmpleados.CrearUsuarioAsync(
                EmpleadoSeleccionado.Id,
                NuevoUsuarioInput.Trim(),
                ClaveTemporalInput);

            var usuarioCreado = NuevoUsuarioInput.Trim();
            NuevoUsuarioInput = string.Empty;
            ClaveTemporalInput = string.Empty;

            await CargarDatosAsync();
            MensajeEstado = $"Usuario '{usuarioCreado}' asignado con cambio obligatorio de clave en primer ingreso.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task DesactivarEmpleadoSeleccionadoAsync()
    {
        if (EmpleadoSeleccionado is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioEmpleados = _servicioEmpleados ?? servicios?.GetService<IServicioEmpleados>();
        if (servicioEmpleados is null)
            return;

        TieneError = false;
        try
        {
            var nombre = EmpleadoSeleccionado.NombreCompleto;
            await servicioEmpleados.DesactivarAsync(EmpleadoSeleccionado.Id);
            await CargarDatosAsync();
            MensajeEstado = $"Empleado {nombre} desactivado.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task DesbloquearUsuarioSeleccionadoAsync()
    {
        if (EmpleadoSeleccionado?.UsuarioAsociado is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioUsuarios = _servicioUsuarios ?? servicios?.GetService<IServicioUsuarios>();
        if (servicioUsuarios is null)
            return;

        TieneError = false;
        try
        {
            await servicioUsuarios.DesbloquearAsync(EmpleadoSeleccionado.UsuarioAsociado.Id);
            await CargarDatosAsync();
            MensajeEstado = $"Cuenta '{EmpleadoSeleccionado.UsuarioAsociado.NombreUsuario}' desbloqueada.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task RestablecerClaveTemporalAsync()
    {
        if (EmpleadoSeleccionado?.UsuarioAsociado is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var servicioUsuarios = _servicioUsuarios ?? servicios?.GetService<IServicioUsuarios>();
        if (servicioUsuarios is null)
            return;

        TieneError = false;
        try
        {
            var temporal = await servicioUsuarios.RestablecerClaveTemporalAsync(EmpleadoSeleccionado.UsuarioAsociado.Id);
            await CargarDatosAsync();
            MensajeEstado = $"Clave temporal generada para '{EmpleadoSeleccionado.UsuarioAsociado.NombreUsuario}': {temporal} (entregar al empleado).";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task GuardarPermisosRolSeleccionadoAsync()
    {
        if (RolSeleccionado is null)
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var estructura = _servicioEstructura ?? servicios?.GetService<IServicioEstructura>();
        if (estructura is null)
            return;

        TieneError = false;
        try
        {
            var mascara = Permiso.Ninguno;
            foreach (var b in BanderasRolSeleccionado)
            {
                if (b.Activo)
                    mascara |= b.Valor;
            }

            await estructura.EditarPermisosRolAsync(RolSeleccionado.Id, mascara);
            RolSeleccionado.Permisos = mascara;
            MensajeEstado = $"Matriz de permisos actualizada para el rol '{RolSeleccionado.Nombre}'.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task CrearNuevoRolAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoRolNombreInput))
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var estructura = _servicioEstructura ?? servicios?.GetService<IServicioEstructura>();
        if (estructura is null)
            return;

        TieneError = false;
        try
        {
            var mascara = Permiso.Ninguno;
            foreach (var b in BanderasRolSeleccionado.Where(x => x.Activo))
                mascara |= b.Valor;

            var creado = await estructura.CrearRolAsync(NuevoRolNombreInput.Trim(), mascara);
            NuevoRolNombreInput = string.Empty;
            await CargarDatosAsync();
            RolSeleccionado = Roles.FirstOrDefault(r => r.Id == creado.Id);
            MensajeEstado = $"Rol '{creado.Nombre}' creado.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    [RelayCommand]
    public async Task CrearNuevoCargoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoCargoNombreInput))
            return;

        using var scope = _scopeFactory?.CreateScope();
        var servicios = scope?.ServiceProvider;
        var estructura = _servicioEstructura ?? servicios?.GetService<IServicioEstructura>();
        if (estructura is null)
            return;

        TieneError = false;
        try
        {
            var creado = await estructura.CrearCargoAsync(NuevoCargoNombreInput.Trim());
            NuevoCargoNombreInput = string.Empty;
            await CargarDatosAsync();
            MensajeEstado = $"Cargo '{creado.Nombre}' creado.";
        }
        catch (Exception ex)
        {
            TieneError = true;
            MensajeEstado = Traducir(servicios, ex);
        }
    }

    private string Traducir(IServiceProvider? servicios, Exception ex)
        => _traductor?.Traducir(ex)
           ?? servicios?.GetService<ITraductorExcepciones>()?.Traducir(ex)
           ?? ex.Message;
}