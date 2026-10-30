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
                | Permiso.AnularVenta
        },
        new Rol
        {
            Nombre = NombresRol.Cajero,
            Permisos = Permiso.Vender | Permiso.AbrirCerrarTurno
        },
        new Rol
        {
            Nombre = NombresRol.Bodeguero,
            Permisos = Permiso.VerInventario | Permiso.AjustarInventario
                | Permiso.Reabastecer | Permiso.GestionarProductos
        }
    ];

    public static IEnumerable<Cargo> Cargos() =>
    [
        new Cargo { Nombre = NombresRol.Admin },
        new Cargo { Nombre = NombresRol.Gerente },
        new Cargo { Nombre = NombresRol.Cajero },
        new Cargo { Nombre = NombresRol.Bodeguero }
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
        Nuevo(ClavesParametro.CarpetaRespaldos,
            RutasDatos.CarpetaRespaldosPorDefecto(),
            "Carpeta donde se guardan las copias de respaldo de la base")
    ];

    private static Parametro Nuevo(string clave, string valor, string texto)
        => new() { Clave = clave, Valor = valor, Descripcion = texto };
}
