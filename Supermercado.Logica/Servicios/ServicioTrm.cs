using System.Globalization;
using Supermercado.Entidades.Configuracion;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Servicios;

public class ServicioTrm(
    ITrmClient cliente,
    IServicioParametros parametros,
    TimeProvider reloj) : IServicioTrm
{
    private const string FormatoFecha = "yyyy-MM-dd";
    private static readonly TimeSpan EsperaBase =
        TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(2);

    public async Task<Trm?> ObtenerAsync(CancellationToken ct = default)
    {
        var reintentos = Math.Max(0, await parametros.ObtenerEnteroAsync(
            ClavesParametro.Reintentos));
        var timeout = await parametros.ObtenerEnteroAsync(
            ClavesParametro.TimeoutSegundos);

        for (var intento = 0; intento <= reintentos; intento++)
        {
            if (intento > 0)
                await EsperarAsync(intento, ct);

            var trm = await IntentarAsync(timeout, ct);

            if (trm is null)
                continue;

            await GuardarRespaldoAsync(trm);
            return trm;
        }

        return await LeerRespaldoAsync();
    }

    private Task EsperarAsync(int intento, CancellationToken ct)
    {
        var milisegundos = Math.Min(
            EsperaBase.TotalMilliseconds * Math.Pow(2, intento - 1),
            EsperaMaxima.TotalMilliseconds);

        return Task.Delay(
            TimeSpan.FromMilliseconds(milisegundos), reloj, ct);
    }

    private async Task<Trm?> IntentarAsync(int segundos, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromSeconds(segundos));

        try
        {
            return await cliente.ObtenerAsync(limite.Token);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private Task GuardarRespaldoAsync(Trm trm)
        => parametros.GuardarRespaldoAsync(new Dictionary<string, string>
        {
            [ClavesParametro.TrmUltimaCentavos] =
                trm.Centavos.ToString(CultureInfo.InvariantCulture),
            [ClavesParametro.TrmUltimaFecha] =
                trm.Fecha.ToString(FormatoFecha, CultureInfo.InvariantCulture)
        });

    private async Task<Trm?> LeerRespaldoAsync()
    {
        var centavos = await parametros.ObtenerLongAsync(
            ClavesParametro.TrmUltimaCentavos);

        if (centavos <= 0)
            return null;

        var texto = await parametros.ObtenerTextoAsync(
            ClavesParametro.TrmUltimaFecha);
        var fecha = DateOnly.ParseExact(
            texto, FormatoFecha, CultureInfo.InvariantCulture);

        return new Trm(centavos, fecha);
    }
}
