using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Finanzas;

public class ServicioNomina(
    IRepositorio<Empleado> empleados,
    IRepositorio<Nomina> nominas,
    IServicioParametros parametros,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioNomina
{
    public async Task<Nomina> CalcularAsync(int empleadoId, int anio, int mes)
    {
        sesion.Exigir(Permiso.GestionarPersonal);

        if (mes is < 1 or > 12)
            throw new ValidacionException("El mes debe estar entre 1 y 12.");
        if (anio < 2000 || anio > reloj.AhoraUtc().Year + 1)
            throw new ValidacionException("El año no es válido.");

        var existente = (await nominas.ObtenerTodosAsync())
            .FirstOrDefault(n => n.EmpleadoId == empleadoId
                && n.Anio == anio && n.Mes == mes);
        if (existente is not null)
            throw new ValidacionException("Ya existe la nómina de ese periodo.");

        var empleado = await empleados.ObtenerPorIdAsync(empleadoId)
            ?? throw new ValidacionException("El empleado no existe.");
        var nomina = Nomina.Calcular(
            empleado, anio, mes, await LeerParametrosAsync(), reloj.AhoraUtc());

        await nominas.AgregarAsync(nomina);
        await trabajo.GuardarAsync();
        return nomina;
    }

    private async Task<ParametrosNomina> LeerParametrosAsync()
    {
        var minimo = await parametros.ObtenerLongAsync(
            ClavesParametro.SalarioMinimo);
        var auxilio = await parametros.ObtenerLongAsync(
            ClavesParametro.AuxilioTransporte);
        var tope = await parametros.ObtenerEnteroAsync(
            ClavesParametro.TopeAuxilioEnSmmlv);
        var salud = await parametros.ObtenerEnteroAsync(
            ClavesParametro.SaludPuntosBasicos);
        var pension = await parametros.ObtenerEnteroAsync(
            ClavesParametro.PensionPuntosBasicos);

        return new ParametrosNomina(minimo, auxilio, tope, salud, pension);
    }
}
