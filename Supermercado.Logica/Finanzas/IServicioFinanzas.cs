using Supermercado.Entidades.Finanzas;

namespace Supermercado.Logica.Finanzas;

public record ResumenFinanciero(
    long Ingresos,
    long Egresos,
    long IvaGenerado,
    long Margen);

public interface IServicioImpuestos
{
    Task<IReadOnlyList<Impuesto>> ObtenerActivosAsync();
    Task CambiarTarifaAsync(int impuestoId, int puntosBasicos);
}

public interface IServicioNomina
{
    Task<Nomina> CalcularAsync(int empleadoId, int anio, int mes);
}

public interface IServicioFinanzas
{
    Task RegistrarGastoAsync(string descripcion, long monto);
    Task<ResumenFinanciero> ObtenerResumenAsync(DateTime desde, DateTime hasta);
}
