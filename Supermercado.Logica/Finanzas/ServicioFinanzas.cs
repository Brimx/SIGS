using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Finanzas;

public class ServicioFinanzas(
    IVentaRepositorio ventas,
    IGastoRepositorio gastos,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioFinanzas
{
    public async Task RegistrarGastoAsync(string descripcion, long monto)
    {
        sesion.Exigir(Permiso.VerFinanzas);

        if (string.IsNullOrWhiteSpace(descripcion) || monto <= 0)
            throw new ValidacionException("Descripción o monto inválidos.");

        await gastos.AgregarAsync(new Gasto
        {
            Descripcion = descripcion,
            Monto = monto,
            Fecha = reloj.AhoraUtc(),
            RegistradoPorEmpleadoId = sesion.EmpleadoId
        });
        await auditoria.RegistrarAsync(
            AccionAuditoria.GastoRegistrado,
            nameof(Gasto),
            detalle: descripcion);
        await trabajo.GuardarAsync();
    }

    public async Task<ResumenFinanciero> ObtenerResumenAsync(
        DateTime desde,
        DateTime hasta)
    {
        sesion.Exigir(Permiso.VerFinanzas);

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
}
