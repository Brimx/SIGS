using System.Text.Json;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Ventas;

public class ServicioVentasEnEspera(
    IRepositorio<VentaEnEspera> ventasEnEspera,
    ISesionActual sesion,
    IServicioAuditoria auditoria,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioVentasEnEspera
{
    public async Task<VentaEnEspera> SuspenderVentaAsync(
        int cajaId,
        IReadOnlyList<LineaVenta> lineas,
        int? clienteId,
        int puntosARedimir,
        string identificadorCliente,
        long totalEstimado,
        string? nota = null)
    {
        sesion.Exigir(Permiso.Vender);

        if (lineas is null || lineas.Count == 0)
            throw new ValidacionException("No se puede suspender una venta sin productos.");

        var ventaEnEspera = new VentaEnEspera
        {
            CajaId = cajaId,
            EmpleadoId = sesion.EmpleadoId,
            FechaCreacion = reloj.AhoraUtc(),
            IdentificadorCliente = string.IsNullOrWhiteSpace(identificadorCliente)
                ? $"Ticket-{reloj.AhoraUtc():HH:mm:ss}"
                : identificadorCliente.Trim(),
            ClienteId = clienteId,
            PuntosARedimir = puntosARedimir,
            LineasJson = JsonSerializer.Serialize(lineas),
            TotalEstimado = totalEstimado,
            Nota = nota?.Trim(),
            Recuperada = false
        };

        await ventasEnEspera.AgregarAsync(ventaEnEspera);
        await auditoria.RegistrarAsync(
            AccionAuditoria.VentaSuspendida,
            nameof(VentaEnEspera),
            ventaEnEspera.Id,
            $"Venta suspendida en caja {cajaId}. Items: {lineas.Count}, Total: ${totalEstimado:N0}");
        await trabajo.GuardarAsync();

        return ventaEnEspera;
    }

    public async Task<IReadOnlyList<ItemVentaEnEspera>> ObtenerVentasEnEsperaAsync(int cajaId)
    {
        sesion.Exigir(Permiso.Vender);

        var lista = await ventasEnEspera.ObtenerTodosAsync();
        return lista
            .Where(v => v.CajaId == cajaId && !v.Recuperada)
            .OrderByDescending(v => v.FechaCreacion)
            .Select(v =>
            {
                var lineas = DeserializarLineas(v.LineasJson);
                var totalItems = lineas.Sum(l => l.Cantidad);
                return new ItemVentaEnEspera(
                    v.Id,
                    v.FechaCreacion,
                    v.IdentificadorCliente,
                    totalItems,
                    v.TotalEstimado,
                    v.Nota);
            })
            .ToList();
    }

    public async Task<(VentaEnEspera Venta, IReadOnlyList<LineaVenta> Lineas)> RecuperarVentaAsync(int id)
    {
        sesion.Exigir(Permiso.Vender);

        var venta = await ventasEnEspera.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("La venta en espera no existe.");

        if (venta.Recuperada)
            throw new ValidacionException("La venta en espera ya fue recuperada o cancelada.");

        venta.Recuperada = true;
        ventasEnEspera.Actualizar(venta);
        await trabajo.GuardarAsync();

        var lineas = DeserializarLineas(venta.LineasJson);
        return (venta, lineas);
    }

    public async Task CancelarVentaEnEsperaAsync(int id)
    {
        sesion.Exigir(Permiso.Vender);

        var venta = await ventasEnEspera.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("La venta en espera no existe.");

        venta.Recuperada = true;
        ventasEnEspera.Actualizar(venta);
        await trabajo.GuardarAsync();
    }

    private static IReadOnlyList<LineaVenta> DeserializarLineas(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<LineaVenta>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
