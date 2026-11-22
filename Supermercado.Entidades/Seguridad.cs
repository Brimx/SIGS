using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Seguridad
{
public enum AccionAuditoria
{
    InicioSesion,
    InicioSesionFallido,
    CambioClave,
    VentaAnulada,
    AjusteInventario,
    Merma,
    CambioRol,
    CambioTarifa,
    AperturaTurno,
    CierreTurno,
    EmpleadoCreado,
    EmpleadoDesactivado,
    GastoRegistrado,
    ParametroCambiado,
    UsuarioCreado,
    ProductoModificado,
    PrecioCambiado,
    CategoriaCreada,
    CategoriaModificada,
    ProveedorCreado,
    ProveedorModificado,
    CajaCreada,
    CajaModificada,
    CargoCreado,
    CargoModificado,
    RolCreado,
    RolModificado,
    UsuarioDesbloqueado,
    ClaveRestablecida,
    RespaldoBaseDatos,
    ClienteCreado,
    ClienteModificado,
    AutorizacionSupervisor,
    RetiroCajaRegistrado,
    VentaSuspendida,
    ReabastecimientoGenerado,
    VinculoTelegram,
    ProductoCreado,
    IngresoBodegaRegistrado
}

[Flags]
public enum Permiso : long
{
    Ninguno = 0,
    Vender = 1 << 0,
    AnularVenta = 1 << 1,
    AbrirCerrarTurno = 1 << 2,
    VerInventario = 1 << 3,
    AjustarInventario = 1 << 4,
    Reabastecer = 1 << 5,
    GestionarProductos = 1 << 6,
    VerReportes = 1 << 7,
    VerFinanzas = 1 << 8,
    GestionarPersonal = 1 << 9,
    GestionarRoles = 1 << 10,
    GestionarTarifas = 1 << 11,
    VerAuditoria = 1 << 12,
    GestionarClientes = 1 << 13,
    AutorizarOperacionPos = 1 << 14,
    GestionarCompras = 1 << 15,
    GestionarContabilidad = 1 << 16,
    MantenimientoSistema = 1 << 17,
}

public class RegistroAuditoria : EntidadBase
{
    public DateTime Fecha { get; set; }
    public int? EmpleadoId { get; set; }
    public AccionAuditoria Accion { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string Detalle { get; set; } = string.Empty;
}

public class IntentoAcceso : EntidadBase
{
    public string NombreUsuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool Exitoso { get; set; }
    public bool UsoPin { get; set; }
}

public record FiltroAuditoria(
    DateTime? Desde = null,
    DateTime? Hasta = null,
    AccionAuditoria? Accion = null,
    int? EmpleadoId = null,
    int Pagina = 1,
    int TamanoPagina = 20);

public record ResultadoPaginado<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int Pagina,
    int TamanoPagina)
{
    public int TotalPaginas
        => TamanoPagina > 0
            ? (int)Math.Ceiling((double)TotalItems / TamanoPagina)
            : 0;
}
}

namespace Supermercado.Entidades.Configuracion
{
public class Parametro : EntidadBase
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public static class ClavesParametro
{
    public const string MaxIntentosFallidos = "seguridad.max_intentos";
    public const string DiasAlertaVencimiento = "inventario.dias_alerta";
    public const string ToleranciaArqueo = "caja.tolerancia_arqueo";
    public const string SalarioMinimo = "nomina.salario_minimo";
    public const string AuxilioTransporte = "nomina.auxilio_transporte";
    public const string TopeAuxilioEnSmmlv = "nomina.tope_auxilio_smmlv";
    public const string SaludPuntosBasicos = "nomina.salud_pb";
    public const string PensionPuntosBasicos = "nomina.pension_pb";
    public const string TimeoutSegundos = "integraciones.timeout_seg";
    public const string Reintentos = "integraciones.reintentos";
    public const string TrmHabilitada = "integraciones.trm_habilitada";

    // Régimen tributario
    public const string ResponsableIva = "tributario.responsable_iva";

    public const string TrmToken = "integraciones.trm_token";
    public const string ResendToken = "integraciones.resend_token";
    public const string ResendRemitente = "integraciones.resend_remitente";
    public const string CodigoBarrasHabilitado = "integraciones.codigo_barras_habilitado";
    public const string TrmUltimaCentavos = "integraciones.trm_centavos";
    public const string TrmUltimaFecha = "integraciones.trm_fecha";
    public const string NegocioNombre = "negocio.nombre";
    public const string NegocioNit = "negocio.nit";
    public const string NegocioDireccion = "negocio.direccion";
    public const string NegocioTelefono = "negocio.telefono";
    public const string CarpetaRespaldos = "sistema.carpeta_respaldos";
}
}
