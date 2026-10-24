using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas.Falsos;

public class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public class UnidadDeTrabajoFalsa : IUnidadDeTrabajo
{
    public int Guardados { get; private set; }

    public Task<int> GuardarAsync()
    {
        Guardados++;
        return Task.FromResult(0);
    }
}

public class HasheadorFalso : IHasheadorClave
{
    public string Hashear(string secreto) => "h:" + secreto;

    public bool Verificar(string secreto, string hash)
        => hash == "h:" + secreto;
}

public class AuditoriaFalsa : IServicioAuditoria
{
    public List<AccionAuditoria> Acciones { get; } = [];

    public Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
    {
        Acciones.Add(accion);
        return Task.CompletedTask;
    }
}

public class ParametrosFalsos : IServicioParametros
{
    public Dictionary<string, string> Valores { get; } = [];

    public Task<string> ObtenerTextoAsync(string clave)
        => Valores.TryGetValue(clave, out var valor)
            ? Task.FromResult(valor)
            : throw new ParametroFaltanteException(clave);

    public async Task<int> ObtenerEnteroAsync(string clave)
        => (int)await ObtenerLongAsync(clave);

    public async Task<long> ObtenerLongAsync(string clave)
        => long.Parse(await ObtenerTextoAsync(clave));

    public Task GuardarAsync(string clave, string valor)
    {
        Valores[clave] = valor;
        return Task.CompletedTask;
    }

    public Task GuardarRespaldoAsync(
        IReadOnlyDictionary<string, string> valores)
    {
        foreach (var (clave, valor) in valores)
            Valores[clave] = valor;

        return Task.CompletedTask;
    }
}

public class ClienteTrmFalso : ITrmClient
{
    public Trm? Respuesta { get; set; }
    public int Llamadas { get; private set; }

    public Task<Trm> ObtenerAsync(CancellationToken ct)
    {
        Llamadas++;

        return Respuesta is null
            ? throw new HttpRequestException("sin conexión")
            : Task.FromResult(Respuesta);
    }
}

public static class Sesiones
{
    public static SesionActual Con(Permiso permisos)
    {
        var sesion = new SesionActual();
        sesion.Iniciar(new Usuario
        {
            Id = 1,
            EmpleadoId = 1,
            NombreUsuario = "prueba",
            Empleado = new Empleado
            {
                Id = 1,
                Rol = new Rol { Permisos = permisos }
            }
        });
        return sesion;
    }
}
