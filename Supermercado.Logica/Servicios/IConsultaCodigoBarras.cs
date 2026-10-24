namespace Supermercado.Logica.Servicios;

public interface IConsultaCodigoBarras
{
    Task<InfoProductoExterno?> ConsultarAsync(
        string codigo,
        CancellationToken ct);
}
