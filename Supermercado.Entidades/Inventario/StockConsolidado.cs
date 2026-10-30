namespace Supermercado.Entidades.Inventario;

public record StockConsolidado(
    int ProductoId,
    string NombreProducto,
    string CodigoBarras,
    int LoteId,
    string NumeroLote,
    DateOnly FechaVencimiento,
    int CantidadEstante,
    int CantidadBodega,
    int TotalStock);
