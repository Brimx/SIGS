using Supermercado.Entidades.Base;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Finanzas;

public class ServicioFinanzas(
    IVentaRepositorio ventas,
    IGastoRepositorio gastos,
    IServicioAuditoria? auditoria = null,
    ISesionActual? sesion = null,
    IUnidadDeTrabajo? trabajo = null,
    TimeProvider? reloj = null,
    IRepositorio<Impuesto>? impuestos = null,
    IRepositorio<Empleado>? empleados = null,
    IRepositorio<Nomina>? nominas = null,
    IServicioParametros? parametros = null) : IServicioFinanzas, IServicioImpuestos, IServicioNomina
{
    private readonly TimeProvider _reloj = reloj ?? TimeProvider.System;

    // --- Gastos y resumen financiero ---

    public async Task RegistrarGastoAsync(string descripcion, long monto)
    {
        sesion?.Exigir(Permiso.VerFinanzas);

        if (string.IsNullOrWhiteSpace(descripcion) || monto <= 0)
            throw new ValidacionException("Descripción o monto inválidos.");

        await gastos.AgregarAsync(new Gasto
        {
            Descripcion = descripcion,
            Monto = monto,
            Fecha = _reloj.AhoraUtc(),
            RegistradoPorEmpleadoId = sesion?.EmpleadoId ?? 0
        });

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.GastoRegistrado,
                nameof(Gasto),
                detalle: descripcion);

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    public async Task<ResumenFinanciero> ObtenerResumenAsync(
        DateTime desde,
        DateTime hasta)
    {
        sesion?.Exigir(Permiso.VerFinanzas);

        var completadas = (await ventas.ObtenerPorRangoAsync(desde, hasta))
            .Where(v => v.Estado == EstadoVenta.Completada)
            .ToList();
        var delPeriodo = await gastos.ObtenerPorRangoAsync(desde, hasta);

        var ingresos = completadas.Sum(v => v.Total);
        var egresos = delPeriodo.Sum(g => g.Monto);

        return new ResumenFinanciero(
            ingresos,
            egresos,
            completadas.Sum(v => v.TotalIva),
            ingresos - egresos);
    }

    // --- Impuestos ---

    public async Task<IReadOnlyList<Impuesto>> ObtenerActivosAsync()
    {
        if (impuestos is null)
            return [];

        return (await impuestos.ObtenerTodosAsync()).Where(i => i.Activo).ToList();
    }

    public async Task CambiarTarifaAsync(int impuestoId, int puntosBasicos)
    {
        sesion?.Exigir(Permiso.GestionarTarifas);

        if (puntosBasicos is < 0 or > Dinero.PuntosBasicosCien)
            throw new ValidacionException("La tarifa va de 0 a 100 %.");

        if (impuestos is null)
            return;

        var impuesto = await impuestos.ObtenerPorIdAsync(impuestoId)
            ?? throw new ValidacionException("El impuesto no existe.");
        var anterior = impuesto.PuntosBasicos;

        impuesto.PuntosBasicos = puntosBasicos;
        impuestos.Actualizar(impuesto);

        if (auditoria is not null)
            await auditoria.RegistrarAsync(
                AccionAuditoria.CambioTarifa,
                nameof(Impuesto),
                impuestoId,
                $"{anterior} a {puntosBasicos} puntos básicos");

        if (trabajo is not null)
            await trabajo.GuardarAsync();
    }

    // --- Nómina ---

    public async Task<Nomina> CalcularAsync(int empleadoId, int anio, int mes)
    {
        sesion?.Exigir(Permiso.GestionarPersonal);

        if (mes is < 1 or > 12)
            throw new ValidacionException("El mes debe estar entre 1 y 12.");
        if (anio < 2000 || anio > _reloj.AhoraUtc().Year + 1)
            throw new ValidacionException("El año no es válido.");

        if (nominas is null || empleados is null)
            throw new InvalidOperationException("Repositorios de nómina no configurados.");

        var existente = (await nominas.ObtenerTodosAsync())
            .FirstOrDefault(n => n.EmpleadoId == empleadoId
                && n.Anio == anio && n.Mes == mes);
        if (existente is not null)
            throw new ValidacionException("Ya existe la nómina de ese periodo.");

        var empleado = await empleados.ObtenerPorIdAsync(empleadoId)
            ?? throw new ValidacionException("El empleado no existe.");

        var parametrosNomina = await LeerParametrosNominaAsync();
        var nomina = Nomina.Calcular(
            empleado, anio, mes, parametrosNomina, _reloj.AhoraUtc());

        await nominas.AgregarAsync(nomina);

        if (trabajo is not null)
            await trabajo.GuardarAsync();

        return nomina;
    }

    private async Task<ParametrosNomina> LeerParametrosNominaAsync()
    {
        if (parametros is null)
            return new ParametrosNomina(0, 0, 0, 0, 0);

        var minimo = await parametros.ObtenerLongAsync(ClavesParametro.SalarioMinimo);
        var auxilio = await parametros.ObtenerLongAsync(ClavesParametro.AuxilioTransporte);
        var tope = await parametros.ObtenerEnteroAsync(ClavesParametro.TopeAuxilioEnSmmlv);
        var salud = await parametros.ObtenerEnteroAsync(ClavesParametro.SaludPuntosBasicos);
        var pension = await parametros.ObtenerEnteroAsync(ClavesParametro.PensionPuntosBasicos);

        return new ParametrosNomina(minimo, auxilio, tope, salud, pension);
    }
}

// Adaptadores para compatibilidad de pruebas existentes
public class ServicioImpuestos(
    IRepositorio<Impuesto> impuestos,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : ServicioFinanzas(
        null!,
        null!,
        auditoria: auditoria,
        sesion: sesion,
        trabajo: trabajo,
        impuestos: impuestos);

public class ServicioNomina(
    IRepositorio<Empleado> empleados,
    IRepositorio<Nomina> nominas,
    IServicioParametros parametros,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : ServicioFinanzas(
        null!,
        null!,
        sesion: sesion,
        trabajo: trabajo,
        reloj: reloj,
        empleados: empleados,
        nominas: nominas,
        parametros: parametros);
