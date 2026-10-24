using System.Globalization;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;

namespace Supermercado.Logica.Seguridad;

public class ServicioParametros(
    IParametroRepositorio parametros,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioParametros
{
    public async Task<string> ObtenerTextoAsync(string clave)
    {
        var parametro = await parametros.ObtenerPorClaveAsync(clave);
        return parametro?.Valor ?? throw new ParametroFaltanteException(clave);
    }

    public async Task<int> ObtenerEnteroAsync(string clave)
        => (int)await ObtenerLongAsync(clave);

    public async Task<long> ObtenerLongAsync(string clave)
    {
        var texto = await ObtenerTextoAsync(clave);
        var valido = long.TryParse(
            texto, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var valor);

        return valido
            ? valor
            : throw new ValidacionException($"'{clave}' no es un número.");
    }

    public async Task GuardarAsync(string clave, string valor)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        await AsignarAsync(clave, valor);

        await auditoria.RegistrarAsync(
            AccionAuditoria.ParametroCambiado,
            nameof(Parametro),
            detalle: clave);

        await trabajo.GuardarAsync();
    }

    public async Task GuardarRespaldoAsync(
        IReadOnlyDictionary<string, string> valores)
    {
        foreach (var (clave, valor) in valores)
            await AsignarAsync(clave, valor);

        await trabajo.GuardarAsync();
    }

    private async Task AsignarAsync(string clave, string valor)
    {
        var existente = await parametros.ObtenerPorClaveAsync(clave);

        if (existente is null)
            await parametros.AgregarAsync(
                new Parametro { Clave = clave, Valor = valor });
        else
        {
            existente.Valor = valor;
            parametros.Actualizar(existente);
        }
    }
}
