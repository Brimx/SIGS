using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Ventas;

namespace Supermercado.Logica.Reportes;

public interface IServicioReportes
{
    Task<byte[]> GenerarDesprendibleNominaAsync(Nomina nomina);

    Task<byte[]> GenerarReporteVentasAsync(
        DateTime desde,
        DateTime hasta,
        IReadOnlyList<Venta> ventas);

    Task<byte[]> GenerarReporteInventarioAsync(
        IReadOnlyList<StockConsolidado> inventario);

    Task<byte[]> GenerarReporteArqueoAsync(
        TurnoCaja turno,
        ResultadoArqueo arqueo);
}
