using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos.Semilla;

// Valores de arranque: todos se pueden editar después desde la aplicación.
public static class ValoresIniciales
{
    private static readonly Permiso Todos = Enum.GetValues<Permiso>()
        .Aggregate(Permiso.Ninguno, (acumulado, p) => acumulado | p);

    public static IEnumerable<Rol> Roles() =>
    [
        new Rol { Nombre = NombresRol.Admin, Permisos = Todos },
        new Rol
        {
            Nombre = NombresRol.Gerente,
            Permisos = Permiso.VerReportes | Permiso.VerFinanzas
                | Permiso.GestionarPersonal | Permiso.VerInventario
                | Permiso.AnularVenta | Permiso.GestionarClientes
                | Permiso.AutorizarOperacionPos
        },
        new Rol
        {
            Nombre = NombresRol.Supervisor,
            Permisos = Permiso.Vender | Permiso.AnularVenta
                | Permiso.AbrirCerrarTurno | Permiso.AutorizarOperacionPos
                | Permiso.VerInventario | Permiso.GestionarClientes
        },
        new Rol
        {
            Nombre = NombresRol.Cajero,
            Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno
                | Permiso.GestionarClientes
        },
        new Rol
        {
            Nombre = NombresRol.Bodeguero,
            Permisos = Permiso.VerInventario | Permiso.AjustarInventario
                | Permiso.Reabastecer | Permiso.GestionarProductos
        },
        new Rol
        {
            Nombre = NombresRol.Compras,
            Permisos = Permiso.VerInventario | Permiso.Reabastecer
                | Permiso.GestionarProductos | Permiso.GestionarCompras
        },
        new Rol
        {
            Nombre = NombresRol.Contador,
            Permisos = Permiso.VerFinanzas | Permiso.VerReportes
                | Permiso.GestionarContabilidad | Permiso.VerAuditoria
        },
        new Rol
        {
            Nombre = NombresRol.Soporte,
            Permisos = Permiso.MantenimientoSistema | Permiso.VerAuditoria
        },
        new Rol
        {
            Nombre = NombresRol.AtencionCliente,
            Permisos = Permiso.GestionarClientes | Permiso.VerInventario
        },
        new Rol
        {
            Nombre = NombresRol.AnalistaDatos,
            Permisos = Permiso.VerReportes | Permiso.VerFinanzas | Permiso.VerAuditoria
        }
    ];

    public static IEnumerable<Cargo> Cargos() =>
    [
        new Cargo { Nombre = NombresRol.Admin },
        new Cargo { Nombre = NombresRol.Gerente },
        new Cargo { Nombre = NombresRol.Supervisor },
        new Cargo { Nombre = NombresRol.Cajero },
        new Cargo { Nombre = NombresRol.Bodeguero },
        new Cargo { Nombre = NombresRol.Compras },
        new Cargo { Nombre = NombresRol.Contador },
        new Cargo { Nombre = NombresRol.Soporte },
        new Cargo { Nombre = NombresRol.AtencionCliente },
        new Cargo { Nombre = NombresRol.AnalistaDatos }
    ];

    public static IEnumerable<Impuesto> Impuestos() =>
    [
        new Iva { Nombre = "IVA 0 %", PuntosBasicos = 0 },
        new Iva { Nombre = "IVA 5 %", PuntosBasicos = 500 },
        new Iva { Nombre = "IVA 19 %", PuntosBasicos = 1900 }
    ];

    public static IEnumerable<Caja> Cajas() =>
    [
        new Caja { Nombre = "Caja 1" }
    ];

    public static IEnumerable<Parametro> Parametros() =>
    [
        Nuevo(ClavesParametro.MaxIntentosFallidos, "5",
            "Intentos fallidos antes de bloquear la cuenta"),
        Nuevo(ClavesParametro.DiasAlertaVencimiento, "15",
            "Días de anticipación para alertar vencimientos"),
        Nuevo(ClavesParametro.ToleranciaArqueo, "0",
            "Diferencia de arqueo aceptada, en pesos"),
        Nuevo(ClavesParametro.SalarioMinimo, "1423500",
            "Salario mínimo mensual legal vigente, en pesos"),
        Nuevo(ClavesParametro.AuxilioTransporte, "200000",
            "Auxilio de transporte mensual legal vigente, en pesos"),
        Nuevo(ClavesParametro.TopeAuxilioEnSmmlv, "2",
            "Veces el salario mínimo hasta donde aplica el auxilio"),
        Nuevo(ClavesParametro.SaludPuntosBasicos, "400",
            "Aporte de salud del empleado (400 = 4 %). Verificar vigencia"),
        Nuevo(ClavesParametro.PensionPuntosBasicos, "400",
            "Aporte de pensión del empleado (400 = 4 %). Verificar vigencia"),
        Nuevo(ClavesParametro.TimeoutSegundos, "10",
            "Tiempo máximo de espera de una API externa"),
        Nuevo(ClavesParametro.Reintentos, "2",
            "Reintentos de una API externa antes de usar el respaldo"),
        Nuevo(ClavesParametro.TrmHabilitada, "1",
            "Consultar la TRM en línea (1 sí, 0 no). Sin internet usa la última"),
                Nuevo(ClavesParametro.ResponsableIva, "1",
            "Empresa responsable de IVA (1 sí, 0 régimen no responsable / simple sin IVA descontable)"),
        Nuevo(ClavesParametro.DianHabilitado, "1",
            "Habilitar emisión de Documento Equivalente Electrónico POS DIAN (1 sí, 0 no)"),
        Nuevo(ClavesParametro.DianResolucionNumero, "18764102352817",
            "Número de resolución de autorización de numeración DIAN"),
        Nuevo(ClavesParametro.DianResolucionFecha, "2024-01-15",
            "Fecha de expedición de la resolución DIAN (yyyy-MM-dd)"),
        Nuevo(ClavesParametro.DianResolucionVigenciaHasta, "2026-12-31",
            "Fecha de vencimiento de la resolución DIAN (yyyy-MM-dd)"),
        Nuevo(ClavesParametro.DianPrefijo, "POS",
            "Prefijo de facturación autorizado por la DIAN"),
        Nuevo(ClavesParametro.DianRangoDesde, "1",
            "Rango inicial de numeración autorizado"),
        Nuevo(ClavesParametro.DianRangoHasta, "500000",
            "Rango final de numeración autorizado"),
        Nuevo(ClavesParametro.DianClaveTecnica, "fc8eac422eba16e22ffd8c6f94b3f40a6e38162c",
            "Clave técnica asignada por la DIAN para generación del CUDE"),
        Nuevo(ClavesParametro.DianSiguienteConsecutivo, "1",
            "Siguiente consecutivo formal a emitir"),
        Nuevo(ClavesParametro.DianTipoAmbiente, "2",
            "Ambiente DIAN: 1 Producción, 2 Pruebas/Habilitación"),
        Nuevo(ClavesParametro.SupabaseUrl, "",
            "URL del proyecto Supabase (ej: https://xyz.supabase.co)"),
        Nuevo(ClavesParametro.SupabaseKey, "",
            "API Key (anon o service_role) del proyecto Supabase"),
        Nuevo(ClavesParametro.TrmToken, "",
            "App Token de datos.gov.co para consultas ilimitadas de TRM"),
        Nuevo(ClavesParametro.ResendToken, "",
            "API Key de Resend para envío de recibos y alertas por correo"),
        Nuevo(ClavesParametro.ResendRemitente, "recibos@supermercado.local",
            "Dirección remitente autorizada en Resend"),
        Nuevo(ClavesParametro.SupabaseHabilitado, "1",
            "Sincronizar eventos con Supabase/Neon remoto (1 sí, 0 no)"),
        Nuevo(ClavesParametro.CodigoBarrasHabilitado, "1",
            "Consultar el catálogo externo de códigos de barras (1 sí, 0 no)"),
        Nuevo(ClavesParametro.TrmUltimaCentavos, "0",
            "Última TRM obtenida, en centavos"),
        Nuevo(ClavesParametro.TrmUltimaFecha, "1970-01-01",
            "Fecha de la última TRM obtenida"),
        Nuevo(ClavesParametro.NegocioNombre, "COMPLETAR",
            "Nombre del negocio que aparece en el ticket"),
        Nuevo(ClavesParametro.NegocioNit, "COMPLETAR",
            "NIT del negocio que aparece en el ticket"),
        Nuevo(ClavesParametro.NegocioDireccion, "COMPLETAR",
            "Dirección del negocio que aparece en el ticket"),
        Nuevo(ClavesParametro.NegocioTelefono, "COMPLETAR",
            "Teléfono del negocio que aparece en el ticket"),
        Nuevo(ClavesParametro.PesosPorPunto, "1000",
            "Pesos de compra por cada punto ganado (0 desactiva la acumulación)"),
        Nuevo(ClavesParametro.ValorPuntoPesos, "10",
            "Valor en pesos de un punto al redimir (0 desactiva la redención)"),
        Nuevo(ClavesParametro.CarpetaRespaldos,
            RutasDatos.CarpetaRespaldosPorDefecto(),
            "Carpeta donde se guardan las copias de respaldo de la base")
    ];

    private static Parametro Nuevo(string clave, string valor, string texto)
        => new() { Clave = clave, Valor = valor, Descripcion = texto };
}
