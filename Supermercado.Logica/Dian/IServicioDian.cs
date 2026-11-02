using Supermercado.Entidades.Ventas;

namespace Supermercado.Logica.Dian;

public record ResultadoEmisionDian(
    string Cude,
    string CadenaQr,
    string Prefijo,
    long Consecutivo,
    DateTime FechaValidacion,
    string XmlUbl);

public interface IServicioDian
{
    Task<ResultadoEmisionDian> EmitirDocumentoPosAsync(
        Venta venta,
        CancellationToken ct = default);

    Task<EstadoResolucionDian> ConsultarEstadoResolucionAsync(
        CancellationToken ct = default);
}
