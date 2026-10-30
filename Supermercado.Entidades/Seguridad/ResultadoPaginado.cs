namespace Supermercado.Entidades.Seguridad;

public record ResultadoPaginado<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int Pagina,
    int TamanoPagina)
{
    public int TotalPaginas
        => TamanoPagina > 0
            ? (int)Math.Ceiling((double)TotalItems / TamanoPagina)
            : 0;
}
