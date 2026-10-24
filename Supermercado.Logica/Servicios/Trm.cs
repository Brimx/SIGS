namespace Supermercado.Logica.Servicios;

// La TRM no es dinero: se guarda en centavos para no usar decimales.
public record Trm(long Centavos, DateOnly Fecha);
