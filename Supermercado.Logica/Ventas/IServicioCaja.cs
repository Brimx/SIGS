using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Ventas;

public record ResultadoArqueo(
    long Esperado,
    long Contado,
    long Diferencia,
    bool DentroDeTolerancia);

public interface IServicioCaja
{
    Task<TurnoCaja> AbrirTurnoAsync(int cajaId, long efectivoInicial);
    Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado);
    Task<TurnoCaja?> ObtenerTurnoActualAsync();
}
