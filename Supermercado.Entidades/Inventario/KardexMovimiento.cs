namespace Supermercado.Entidades.Inventario;

public record KardexMovimiento(
    int MovimientoId,
    DateTime Fecha,
    TipoMovimiento Tipo,
    int LoteId,
    string NumeroLote,
    int Cantidad,
    int SaldoPosterior,
    string Motivo);
