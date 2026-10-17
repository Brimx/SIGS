namespace Supermercado.Entidades.Excepciones;

public class StockInsuficienteException(string detalle)
    : Exception($"Stock insuficiente: {detalle}");
