using System.Data;
using Microsoft.EntityFrameworkCore;
using Supermercado.Entidades.Base;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;

namespace Supermercado.Datos;

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
                | Permiso.AutorizarOperacionPos | Permiso.GestionarCompras
                | Permiso.GestionarContabilidad | Permiso.VerAuditoria
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
                | Permiso.GestionarCompras
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
        Nuevo(ClavesParametro.TrmHabilitada, "1",
            "Consultar la TRM en línea (1 sí, 0 no). Sin internet usa la última"),
        Nuevo(ClavesParametro.ResponsableIva, "1",
            "Empresa responsable de IVA (1 sí, 0 régimen no responsable / simple sin IVA descontable)"),
        Nuevo(ClavesParametro.TrmToken, "",
            "App Token de datos.gov.co para consultas ilimitadas de TRM"),
        Nuevo(ClavesParametro.ResendToken, "",
            "API Key de Resend para envío de recibos y alertas por correo"),
        Nuevo(ClavesParametro.ResendRemitente, "recibos@supermercado.local",
            "Dirección remitente autorizada en Resend"),
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
        Nuevo(ClavesParametro.CarpetaRespaldos,
            RutasDatos.CarpetaRespaldosPorDefecto(),
            "Carpeta donde se guardan las copias de respaldo de la base")
    ];

    private static Parametro Nuevo(string clave, string valor, string texto)
        => new() { Clave = clave, Valor = valor, Descripcion = texto };
}

public class InicializadorBaseDatos(SigsDbContext db)
{
    public async Task CrearYSembrarAsync()
    {
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await AsegurarIndiceIdempotenciaUnicoAsync();
        await AsegurarColumnasPanelAsync();

        await SembrarSiVacioAsync(db.Roles, ValoresIniciales.Roles());
        await AsegurarPermisosFaltantesAsync();
        await SembrarSiVacioAsync(db.Cargos, ValoresIniciales.Cargos());
        await SembrarSiVacioAsync(db.Impuestos, ValoresIniciales.Impuestos());
        await SembrarSiVacioAsync(db.Cajas, ValoresIniciales.Cajas());
        await SembrarParametrosFaltantesAsync();
    }

    private async Task AsegurarIndiceIdempotenciaUnicoAsync()
    {
        var claveDuplicada = await db.Ventas
            .Where(v => v.ClaveIdempotencia != null)
            .GroupBy(v => v.ClaveIdempotencia)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        if (claveDuplicada is not null)
            throw new InvalidOperationException(
                "Hay ventas con claves de idempotencia duplicadas; resuélvelas antes de iniciar SIGS.");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Ventas_ClaveIdempotencia\" ON \"Ventas\" (\"ClaveIdempotencia\");");
    }

    // La migración Inicial se genera desde el modelo (ADR-018), así que una base
    // creada antes no tiene las columnas de personalización del panel.
    private async Task AsegurarColumnasPanelAsync()
    {
        var columnas = await ObtenerColumnasAsync("Widgets");

        if (!columnas.Contains("Tamano"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Widgets\" ADD COLUMN \"Tamano\" INTEGER NOT NULL DEFAULT 1;");

        if (!columnas.Contains("TituloPersonalizado"))
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Widgets\" ADD COLUMN \"TituloPersonalizado\" TEXT NULL;");
    }

    private async Task<IReadOnlyList<string>> ObtenerColumnasAsync(string tabla)
    {
        var conexion = db.Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
            await conexion.OpenAsync();

        await using var comando = conexion.CreateCommand();
        comando.CommandText = $"SELECT name FROM pragma_table_info('{tabla}')";

        var columnas = new List<string>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            columnas.Add(lector.GetString(0));

        return columnas;
    }

    private async Task SembrarSiVacioAsync<T>(
        DbSet<T> conjunto,
        IEnumerable<T> valores) where T : EntidadBase
    {
        if (await conjunto.AnyAsync())
            return;

        await conjunto.AddRangeAsync(valores);
        await db.SaveChangesAsync();
    }

    private async Task AsegurarPermisosFaltantesAsync()
    {
        var esperados = ValoresIniciales.Roles()
            .ToDictionary(r => r.Nombre, r => r.Permisos);
        var roles = await db.Roles.ToListAsync();
        var hayCambios = false;

        foreach (var rol in roles)
        {
            if (!esperados.TryGetValue(rol.Nombre, out var esperado))
                continue;

            var resultante = rol.Permisos | esperado;
            if (resultante == rol.Permisos)
                continue;

            rol.Permisos = resultante;
            hayCambios = true;
        }

        if (hayCambios)
            await db.SaveChangesAsync();
    }

    // Agrega solo los parámetros nuevos; nunca pisa los ya editados.
    private async Task SembrarParametrosFaltantesAsync()
    {
        var existentes = await db.Parametros
            .Select(p => p.Clave)
            .ToListAsync();
        var faltantes = ValoresIniciales.Parametros()
            .Where(p => !existentes.Contains(p.Clave))
            .ToList();

        await db.Parametros.AddRangeAsync(faltantes);
        await db.SaveChangesAsync();
    }
}
