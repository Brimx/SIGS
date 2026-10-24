using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Dashboards;

public enum TamanoWidget
{
    Pequeno,
    Mediano,
    Grande
}

public record PuntoDato(string Etiqueta, long Valor);

public record DatoWidget(IReadOnlyList<PuntoDato> Puntos);

public interface IWidget
{
    string Id { get; }
    string Titulo { get; }
    TamanoWidget Tamano { get; }
    TimeSpan FrecuenciaActualizacion { get; }
    Permiso PermisoRequerido { get; }

    Task<DatoWidget> ObtenerDatosAsync();
}
