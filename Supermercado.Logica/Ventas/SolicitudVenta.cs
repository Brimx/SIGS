using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Ventas;

public record LineaVenta(string CodigoBarras, int Cantidad);

public record SolicitudVenta(
    IReadOnlyList<LineaVenta> Lineas,
    IReadOnlyList<Pago> Pagos);
