namespace Supermercado.Logica.Servicios;

public enum HerramientaAsistente
{
    StockBajo,
    VentasDelDia,
    EstadoCaja,
    ResumenDiario,
    SolicitarAnulacion,
    GenerarReabastecimiento
}

public sealed record SolicitudHerramienta(
    HerramientaAsistente Herramienta,
    int? Id = null,
    string? Motivo = null);

public interface IClienteGemini
{
    Task<SolicitudHerramienta?> ElegirHerramientaAsync(
        string mensaje,
        CancellationToken cancelacion = default);
}

public interface IAsistente
{
    Task<SolicitudHerramienta?> InterpretarAsync(
        string mensaje,
        CancellationToken cancelacion = default);
}

public sealed class Asistente(IClienteGemini gemini) : IAsistente
{
    public Task<SolicitudHerramienta?> InterpretarAsync(
        string mensaje,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return Task.FromResult<SolicitudHerramienta?>(null);

        var comando = mensaje.Trim().Split(' ', 2)[0].Split('@')[0];
        var herramienta = comando.ToLowerInvariant() switch
        {
            "/stock" => HerramientaAsistente.StockBajo,
            "/ventas" => HerramientaAsistente.VentasDelDia,
            "/caja" => HerramientaAsistente.EstadoCaja,
            "/resumen" => HerramientaAsistente.ResumenDiario,
            _ => (HerramientaAsistente?)null
        };

        if (herramienta.HasValue)
            return Task.FromResult<SolicitudHerramienta?>(
                new SolicitudHerramienta(herramienta.Value));

        return comando.StartsWith('/')
            ? Task.FromResult<SolicitudHerramienta?>(null)
            : gemini.ElegirHerramientaAsync(mensaje, cancelacion);
    }
}
