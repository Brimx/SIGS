namespace Supermercado.Logica.Servicios;

public class ConsultaCodigoBarrasNula : IConsultaCodigoBarras
{
    public Task<InfoProductoExterno?> ConsultarAsync(
        string codigo,
        CancellationToken ct)
        => Task.FromResult<InfoProductoExterno?>(null);
}
