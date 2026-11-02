namespace Supermercado.Logica.Productos;

public record ResultadoCodigoBalanza(
    bool EsCodigoBalanza,
    string Prefijo,
    string Plu,
    int Gramos,
    string CodigoOriginal,
    int DigitoControl = 0)
{
    public bool EsValido => EsCodigoBalanza;
    public decimal PesoKg => Gramos / 1000m;
}
