using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Inventario;

public class ServicioMermas(
    IServicioInventario inventario,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioMermas
{
    public async Task RegistrarAsync(
        int productoId,
        int loteId,
        UbicacionStock ubicacion,
        int cantidad,
        string motivo)
    {
        sesion.Exigir(Permiso.AjustarInventario);

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ValidacionException("Falta el motivo de la merma.");

        await inventario.RegistrarMermaAsync(
            productoId, loteId, ubicacion, cantidad, motivo);
        await auditoria.RegistrarAsync(
            AccionAuditoria.Merma, nameof(Merma), loteId, motivo);
        await trabajo.GuardarAsync();
    }
}
