using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Configuracion;

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
