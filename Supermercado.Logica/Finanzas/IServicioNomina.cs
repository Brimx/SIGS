using Supermercado.Entidades.Finanzas;

namespace Supermercado.Logica.Finanzas;

public interface IServicioNomina
{
    Task<Nomina> CalcularAsync(int empleadoId, int anio, int mes);
}
