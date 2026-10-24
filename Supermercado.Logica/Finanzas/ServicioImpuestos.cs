using Supermercado.Entidades.Base;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Finanzas;

public class ServicioImpuestos(
    IRepositorio<Impuesto> impuestos,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioImpuestos
{
    public async Task<IReadOnlyList<Impuesto>> ObtenerActivosAsync()
        => (await impuestos.ObtenerTodosAsync()).Where(i => i.Activo).ToList();

    public async Task CambiarTarifaAsync(int impuestoId, int puntosBasicos)
    {
        sesion.Exigir(Permiso.GestionarTarifas);

        if (puntosBasicos is < 0 or > Dinero.PuntosBasicosCien)
            throw new ValidacionException("La tarifa va de 0 a 100 %.");

        var impuesto = await impuestos.ObtenerPorIdAsync(impuestoId)
            ?? throw new ValidacionException("El impuesto no existe.");
        var anterior = impuesto.PuntosBasicos;

        impuesto.PuntosBasicos = puntosBasicos;
        impuestos.Actualizar(impuesto);
        await auditoria.RegistrarAsync(
            AccionAuditoria.CambioTarifa,
            nameof(Impuesto),
            impuestoId,
            $"{anterior} a {puntosBasicos} puntos básicos");
        await trabajo.GuardarAsync();
    }
}
