using Supermercado.Entidades.Finanzas;

namespace Supermercado.Logica.Finanzas;

public interface IServicioImpuestos
{
    Task<IReadOnlyList<Impuesto>> ObtenerActivosAsync();
    Task CambiarTarifaAsync(int impuestoId, int puntosBasicos);
}
