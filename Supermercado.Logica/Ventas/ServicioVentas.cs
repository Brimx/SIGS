using System.Text.Json;
using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Ventas;

public class ServicioVentas(
    IProductoRepositorio productos,
    IVentaRepositorio ventas,
    ITurnoCajaRepositorio turnos,
    IServicioInventario inventario,
    IClienteRepositorio clientes,
    IServicioParametros parametros,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj,
    IRepositorio<VentaEnEspera>? ventasEnEspera = null) : IServicioVentas, IServicioVentasEnEspera
{
    public async Task<Venta> RegistrarVentaAsync(SolicitudVenta solicitud)
    {
        sesion.Exigir(Permiso.Vender);

        if (!string.IsNullOrWhiteSpace(solicitud.ClaveIdempotencia))
        {
            var existente = await ventas
                .ObtenerPorClaveIdempotenciaAsync(solicitud.ClaveIdempotencia.Trim());
            if (existente is not null)
                return existente;
        }

        var turno = await turnos
            .ObtenerAbiertoPorEmpleadoAsync(sesion.EmpleadoId)
            ?? throw new TurnoInvalidoException("Abre un turno primero.");
        var venta = new Venta
        {
            Fecha = reloj.AhoraUtc(),
            TurnoCajaId = turno.Id,
            ClaveIdempotencia = string.IsNullOrWhiteSpace(solicitud.ClaveIdempotencia)
                ? null
                : solicitud.ClaveIdempotencia.Trim()
        };

        foreach (var linea in solicitud.Lineas)
        {
            var (prod, cant) = await BuscarYResolverCantidadAsync(linea);
            venta.AgregarProducto(prod, cant);
        }

        // Switch de régimen tributario: si no es responsable de IVA, desactiva extracción de IVA
        var responsableIva = await parametros.ObtenerBoolAsync(
            ClavesParametro.ResponsableIva, porDefecto: true);
        if (!responsableIva)
        {
            foreach (var detalle in venta.Detalles)
            {
                detalle.Iva = 0;
                detalle.PuntosBasicosIva = 0;
            }
        }

        var cliente = await BuscarClienteAsync(solicitud);
        if (cliente is not null)
            venta.ClienteId = cliente.Id;

        // Protección de datos personales (Ley 1581 / Habeas Data):
        // Si el cliente no autorizó tratamiento de datos, se fuerza Consumidor Final
        if (cliente is not null && cliente.AutorizaTratamientoDatos)
        {
            venta.DocumentoAdquirente = cliente.Documento;
            venta.NombreAdquirente = cliente.NombreCompleto;
        }
        else
        {
            venta.DocumentoAdquirente = Venta.DocumentoConsumidorFinal;
            venta.NombreAdquirente = Venta.NombreConsumidorFinal;
        }

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

    private async Task<Cliente?> BuscarClienteAsync(SolicitudVenta solicitud)
    {
        if (solicitud.ClienteId is null)
            return null;

        var cliente = await clientes.ObtenerPorIdAsync(solicitud.ClienteId.Value)
            ?? throw new VentaInvalidaException("El cliente no existe.");
        if (!cliente.Activo)
            throw new VentaInvalidaException(
                $"{cliente.NombreCompleto} está inactivo.");

        return cliente;
    }

    private async Task<(Producto Producto, int Cantidad)> BuscarYResolverCantidadAsync(
        LineaVenta linea)
    {
        var balanza = ParserCodigoBalanza.Parsear(linea.CodigoBarras);
        var producto = await productos
            .ObtenerPorCodigoBarrasAsync(linea.CodigoBarras);

        if (producto is null && balanza.EsCodigoBalanza)
        {
            // Intentar buscar por PLU directo (ej: 20890) o sin ceros
            producto = await productos.ObtenerPorCodigoBarrasAsync(balanza.Plu)
                ?? await productos.ObtenerPorCodigoBarrasAsync(balanza.Plu.TrimStart('0'));
        }

        if (producto is null)
            throw new VentaInvalidaException(
                $"No existe el código {linea.CodigoBarras}.");

        if (!producto.Activo)
            throw new VentaInvalidaException(
                $"{producto.Nombre} no está activo.");

        var cantidad = linea.Cantidad;
        if (producto is ProductoPesado && balanza.EsCodigoBalanza && balanza.Gramos > 0 && cantidad <= 1)
        {
            cantidad = balanza.Gramos;
        }

        return (producto, cantidad);
    }

    // --- Ventas en Espera ---

    public async Task<VentaEnEspera> SuspenderVentaAsync(
        int cajaId,
        IReadOnlyList<LineaVenta> lineas,
        int? clienteId,
        string identificadorCliente,
        long totalEstimado,
        string? nota = null)
    {
        sesion.Exigir(Permiso.Vender);

        if (ventasEnEspera is null)
            throw new InvalidOperationException("Repositorio de ventas en espera no configurado.");

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

        if (ventasEnEspera is null)
            return [];

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

        if (ventasEnEspera is null)
            throw new InvalidOperationException("Repositorio de ventas en espera no configurado.");

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

        if (ventasEnEspera is null)
            return;

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

// Adaptador para compatibilidad de pruebas existentes
public class ServicioVentasEnEspera(
    IRepositorio<VentaEnEspera> ventasEnEspera,
    ISesionActual sesion,
    IServicioAuditoria auditoria,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : ServicioVentas(
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        auditoria,
        sesion,
        trabajo,
        reloj,
        ventasEnEspera: ventasEnEspera);

