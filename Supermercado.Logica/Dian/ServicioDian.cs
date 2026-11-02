using System.Globalization;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Dian;

public class ServicioDian(
    IServicioParametros parametros,
    TimeProvider reloj) : IServicioDian
{
    public async Task<EstadoResolucionDian> ConsultarEstadoResolucionAsync(
        CancellationToken ct = default)
    {
        var numero = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianResolucionNumero);
        var prefijo = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianPrefijo);
        var vigenciaStr = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianResolucionVigenciaHasta);
        var rangoDesde = await parametros.ObtenerLongAsync(
            ClavesParametro.DianRangoDesde);
        var rangoHasta = await parametros.ObtenerLongAsync(
            ClavesParametro.DianRangoHasta);
        var siguiente = await parametros.ObtenerLongAsync(
            ClavesParametro.DianSiguienteConsecutivo);

        var vigenciaHasta = DateOnly.ParseExact(
            vigenciaStr.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var hoy = reloj.HoyLocal();

        var estaVigente = hoy <= vigenciaHasta;
        var rangoDisponible = siguiente <= rangoHasta;
        var diasRestantes = vigenciaHasta.DayNumber - hoy.DayNumber;
        var foliosRestantes = Math.Max(0, rangoHasta - siguiente + 1);

        var alertaVencimiento = diasRestantes <= 30;
        var alertaAgotamiento = foliosRestantes <= 1000;

        return new EstadoResolucionDian(
            numero,
            prefijo,
            vigenciaHasta,
            rangoDesde,
            rangoHasta,
            siguiente,
            estaVigente,
            rangoDisponible,
            alertaVencimiento,
            alertaAgotamiento,
            diasRestantes,
            foliosRestantes);
    }

    public async Task<ResultadoEmisionDian> EmitirDocumentoPosAsync(
        Venta venta,
        CancellationToken ct = default)
    {
        var habilitado = await parametros.ObtenerBoolAsync(
            ClavesParametro.DianHabilitado, porDefecto: true);

        if (!habilitado)
            return new ResultadoEmisionDian("", "", "", 0, DateTime.MinValue, "");

        var estado = await ConsultarEstadoResolucionAsync(ct);

        if (!estado.EstaVigente)
            throw new ValidacionException(
                $"La resolución DIAN {estado.Numero} venció el {estado.VigenciaHasta:yyyy-MM-dd}. No se pueden emitir tiquetes POS.");

        if (!estado.RangoDisponible)
            throw new ValidacionException(
                $"Se agotó el rango autorizado por la DIAN (máximo {estado.RangoHasta}). Renueve la numeración.");

        var consecutivo = estado.SiguienteConsecutivo;
        var prefijo = estado.Prefijo;
        var numDoc = $"{prefijo}{consecutivo}";

        var claveTecnica = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianClaveTecnica);
        var tipoAmbiente = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianTipoAmbiente);
        var nitEmisor = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioNit);
        var nombreEmisor = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioNombre);
        var direccionEmisor = await parametros.ObtenerTextoAsync(
            ClavesParametro.NegocioDireccion);
        var fechaResolucion = await parametros.ObtenerTextoAsync(
            ClavesParametro.DianResolucionFecha);

        var subtotal = venta.Total - venta.TotalIva;
        var cude = GeneradorCudeDian.CalcularCude(
            numDoc,
            venta.Fecha,
            subtotal,
            venta.TotalIva,
            venta.Total,
            nitEmisor,
            venta.DocumentoAdquirente,
            claveTecnica,
            tipoAmbiente);

        var qr = GeneradorCudeDian.GenerarCadenaQr(cude);

        venta.PrefijoDian = prefijo;
        venta.ConsecutivoDian = consecutivo;
        venta.Cude = cude;
        venta.CadenaQr = qr;
        venta.FechaValidacionDian = reloj.AhoraUtc();

        var xml = GeneradorUblDian.GenerarXml(
            venta,
            estado.Numero,
            fechaResolucion,
            estado.VigenciaHasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            nitEmisor,
            nombreEmisor,
            direccionEmisor);

        venta.XmlUbl = xml;

        // Avanzar el consecutivo para la siguiente emisión
        await parametros.GuardarRespaldoAsync(new Dictionary<string, string>
        {
            [ClavesParametro.DianSiguienteConsecutivo] = (consecutivo + 1).ToString(CultureInfo.InvariantCulture)
        });

        return new ResultadoEmisionDian(
            cude,
            qr,
            prefijo,
            consecutivo,
            venta.FechaValidacionDian.Value,
            xml);
    }
}
