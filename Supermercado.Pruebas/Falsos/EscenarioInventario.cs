using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Productos;
using Supermercado.Logica.Inventario;

namespace Supermercado.Pruebas.Falsos;

public class EscenarioInventario
{
    public static readonly DateTimeOffset Ahora
        = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    public StockEstanteEnMemoria Estante { get; } = new();
    public StockBodegaEnMemoria Bodega { get; } = new();
    public MovimientosEnMemoria Movimientos { get; } = new();
    public MermasEnMemoria Mermas { get; } = new();
    public ServicioInventario Servicio { get; }

    public EscenarioInventario()
        => Servicio = new ServicioInventario(
            Estante,
            Bodega,
            Movimientos,
            Mermas,
            new SalidaFefo(),
            new RelojFijo(Ahora));

    public StockEstante AgregarEstante(
        int productoId,
        int loteId,
        int diasParaVencer,
        int cantidad)
    {
        var fila = new StockEstante
        {
            ProductoId = productoId,
            LoteId = loteId,
            Lote = CrearLote(productoId, loteId, diasParaVencer)
        };
        fila.Aumentar(cantidad);
        Estante.Datos.Add(fila);
        return fila;
    }

    public StockBodega AgregarBodega(
        int productoId,
        int loteId,
        int diasParaVencer,
        int cantidad)
    {
        var fila = new StockBodega
        {
            ProductoId = productoId,
            LoteId = loteId,
            Lote = CrearLote(productoId, loteId, diasParaVencer)
        };
        fila.Aumentar(cantidad);
        Bodega.Datos.Add(fila);
        return fila;
    }

    private static Lote CrearLote(int productoId, int loteId, int dias)
        => new()
        {
            Id = loteId,
            ProductoId = productoId,
            FechaVencimiento = DateOnly
                .FromDateTime(Ahora.UtcDateTime)
                .AddDays(dias)
        };
}
