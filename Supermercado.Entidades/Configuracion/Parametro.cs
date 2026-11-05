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

    // Facturación y POS Electrónico DIAN (Resolución 000165 de 2023)
    public const string DianHabilitado = "dian.habilitado";
    public const string DianResolucionNumero = "dian.resolucion_numero";
    public const string DianResolucionFecha = "dian.resolucion_fecha";
    public const string DianResolucionVigenciaHasta = "dian.resolucion_vigencia_hasta";
    public const string DianPrefijo = "dian.prefijo";
    public const string DianRangoDesde = "dian.rango_desde";
    public const string DianRangoHasta = "dian.rango_hasta";
    public const string DianClaveTecnica = "dian.clave_tecnica";
    public const string DianSiguienteConsecutivo = "dian.siguiente_consecutivo";
    public const string DianTipoAmbiente = "dian.tipo_ambiente";

    public const string SupabaseUrl = "integraciones.supabase_url";
    public const string SupabaseKey = "integraciones.supabase_key";
    public const string TrmToken = "integraciones.trm_token";
    public const string ResendToken = "integraciones.resend_token";
    public const string ResendRemitente = "integraciones.resend_remitente";
    public const string SupabaseHabilitado = "integraciones.supabase_habilitado";
    public const string CodigoBarrasHabilitado = "integraciones.codigo_barras_habilitado";
    public const string TrmUltimaCentavos = "integraciones.trm_centavos";
    public const string TrmUltimaFecha = "integraciones.trm_fecha";
    public const string NegocioNombre = "negocio.nombre";
    public const string NegocioNit = "negocio.nit";
    public const string NegocioDireccion = "negocio.direccion";
    public const string NegocioTelefono = "negocio.telefono";
    public const string PesosPorPunto = "puntos.pesos_por_punto";
    public const string ValorPuntoPesos = "puntos.valor_punto_pesos";
    public const string CarpetaRespaldos = "sistema.carpeta_respaldos";
}
