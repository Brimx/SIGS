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

public class HasheadorFalso : IHasheadorClave
{
    public string Hashear(string secreto) => "h:" + secreto;

    public bool Verificar(string secreto, string hash)
        => hash == "h:" + secreto;
}

public class AuditoriaFalsa : ServicioAuditoria
{
    public AuditoriaFalsa() : base(null!, null!, null!)
    {
    }

    public List<AccionAuditoria> Acciones { get; } = [];

    public override Task RegistrarAsync(
        AccionAuditoria accion,
        string entidad,
        int? entidadId = null,
        string detalle = "")
    {
        Acciones.Add(accion);
        return Task.CompletedTask;
    }
}

public class ParametrosFalsos : ServicioParametros
{
    public ParametrosFalsos() : base(null!, null!, null!)
    {
    }

    public Dictionary<string, string> Valores { get; } = [];

    public override Task<string> ObtenerTextoAsync(string clave)
        => Valores.TryGetValue(clave, out var valor)
            ? Task.FromResult(valor)
            : throw new ParametroFaltanteException(clave);

    public override async Task<int> ObtenerEnteroAsync(string clave)
        => (int)await ObtenerLongAsync(clave);

    public override async Task<long> ObtenerLongAsync(string clave)
        => long.Parse(await ObtenerTextoAsync(clave));

    public override Task GuardarAsync(string clave, string valor)
    {
        Valores[clave] = valor;
        return Task.CompletedTask;
    }

    public override Task GuardarRespaldoAsync(
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
