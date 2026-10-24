using Supermercado.Entidades.Finanzas;

namespace Supermercado.Logica.Finanzas;

public record ResumenFinanciero(
    long Ingresos,
    long Egresos,
    long IvaGenerado,
    long Margen);

public interface IServicioFinanzas
{
    Task RegistrarGastoAsync(string descripcion, long monto);

    Task<ResumenFinanciero> ObtenerResumenAsync(
        DateTime desde,
        DateTime hasta);
}
