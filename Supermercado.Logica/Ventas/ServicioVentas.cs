using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Ventas;

public class ServicioVentas(
    IProductoRepositorio productos,
    IVentaRepositorio ventas,
    ITurnoCajaRepositorio turnos,
    IServicioInventario inventario,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioVentas
{
    public async Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud)
    {
        sesion.Exigir(Permiso.Vender);

        var turno = await turnos
            .ObtenerAbiertoPorEmpleadoAsync(sesion.EmpleadoId)
            ?? throw new TurnoInvalidoException("Abre un turno primero.");
        var venta = new Venta
        {
            Fecha = reloj.AhoraUtc(),
            TurnoCajaId = turno.Id
        };

        foreach (var linea in solicitud.Lineas)
            venta.AgregarProducto(await BuscarAsync(linea), linea.Cantidad);

        foreach (var pago in solicitud.Pagos)
            venta.AgregarPago(pago);

        venta.Cerrar();
        await ventas.AgregarAsync(venta);

        foreach (var detalle in venta.Detalles)
            await inventario.DescontarVentaAsync(
                detalle.ProductoId, detalle.Cantidad, venta);

        await trabajo.GuardarAsync();
        return venta;
    }

    public async Task AnularVentaAsync(int ventaId, string motivo)
    {
        sesion.Exigir(Permiso.AnularVenta);

        var venta = await ventas.ObtenerPorIdAsync(ventaId)
            ?? throw new VentaInvalidaException("La venta no existe.");

        if (venta.Estado == EstadoVenta.Anulada)
            throw new VentaInvalidaException("La venta ya está anulada.");

        venta.Anular(motivo);
        await inventario.ReintegrarVentaAsync(ventaId);
        ventas.Actualizar(venta);
        await auditoria.RegistrarAsync(
            AccionAuditoria.VentaAnulada, nameof(Venta), ventaId, motivo);
        await trabajo.GuardarAsync();
    }

    private async Task<Producto> BuscarAsync(
        LineaVenta linea)
    {
        var producto = await productos
            .ObtenerPorCodigoBarrasAsync(linea.CodigoBarras)
            ?? throw new VentaInvalidaException(
                $"No existe el código {linea.CodigoBarras}.");

        if (!producto.Activo)
            throw new VentaInvalidaException(
                $"{producto.Nombre} no está activo.");

        return producto;
    }
}
