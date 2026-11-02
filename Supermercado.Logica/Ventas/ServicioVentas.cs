using Supermercado.Logica.Productos;
using Supermercado.Logica.Dian;
using Supermercado.Entidades.Sincronizacion;
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
    IEventoOutboxRepositorio? outbox = null,
    IServicioDian? dian = null) : IServicioVentas
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
        var puntos = await PrepararPuntosAsync(venta, cliente, solicitud);

        // Protección de datos personales (Ley 1581 / Habeas Data):
        // Si el cliente no autorizó tratamiento de datos, se fuerza Consumidor Final DIAN
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

        // Emisión DIAN (Resolución 000165 de 2023 - Documento Equivalente POS)
        if (dian is not null)
            await dian.EmitirDocumentoPosAsync(venta);

        // Los puntos se mueven solo cuando la venta ya es válida: así un
        // rechazo no deja el saldo del cliente modificado en memoria.
        if (cliente is not null)
            await AplicarPuntosAsync(venta, cliente, puntos);

        await ventas.AgregarAsync(venta);

        foreach (var detalle in venta.Detalles)
            await inventario.DescontarVentaAsync(
                detalle.ProductoId, detalle.Cantidad, venta);

        if (outbox is not null)
        {
            var payload = JsonSerializer.Serialize(new
            {
                venta.Id,
                venta.Fecha,
                venta.TurnoCajaId,
                venta.Total,
                venta.TotalIva,
                venta.ClienteId,
                venta.PuntosGanados,
                venta.PuntosRedimidos,
                venta.ClaveIdempotencia,
                Detalles = venta.Detalles.Select(d => new
                {
                    d.ProductoId,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal,
                    d.Iva
                }),
                Pagos = venta.Pagos.Select(p => new
                {
                    p.Metodo,
                    p.Monto
                })
            });

            await outbox.AgregarAsync(EventoOutbox.Crear(
                "VentaCompletada",
                venta.Id.ToString(),
                payload));
        }

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
        await RevertirPuntosAsync(venta);
        ventas.Actualizar(venta);
        await auditoria.RegistrarAsync(
            AccionAuditoria.VentaAnulada, nameof(Venta), ventaId, motivo);

        if (outbox is not null)
        {
            var payload = JsonSerializer.Serialize(new
            {
                VentaId = ventaId,
                Motivo = motivo,
                FechaAnulacion = reloj.AhoraUtc()
            });

            await outbox.AgregarAsync(EventoOutbox.Crear(
                "VentaAnulada",
                ventaId.ToString(),
                payload));
        }

        await trabajo.GuardarAsync();
    }

    private async Task<Cliente?> BuscarClienteAsync(SolicitudVenta solicitud)
    {
        if (solicitud.ClienteId is null)
        {
            if (solicitud.PuntosARedimir > 0)
                throw new VentaInvalidaException(
                    "Para redimir puntos hay que indicar el cliente.");

            return null;
        }

        var cliente = await clientes.ObtenerPorIdAsync(solicitud.ClienteId.Value)
            ?? throw new VentaInvalidaException("El cliente no existe.");
        if (!cliente.Activo)
            throw new VentaInvalidaException(
                $"{cliente.NombreCompleto} está inactivo.");

        return cliente;
    }

    // Agrega el pago en puntos (si lo hay) y devuelve cuánto vale la compra
    // pagada con dinero, que es la base para ganar puntos.
    private async Task<(long BaseGanancia, int PuntosRedimidos)> PrepararPuntosAsync(
        Venta venta,
        Cliente? cliente,
        SolicitudVenta solicitud)
    {
        if (cliente is null)
            return (0, 0);

        var redimir = solicitud.PuntosARedimir;
        long montoPuntos = 0;
        if (redimir > 0)
        {
            var valorPunto = await parametros.ObtenerLongAsync(
                ClavesParametro.ValorPuntoPesos);
            if (valorPunto <= 0)
                throw new VentaInvalidaException(
                    "La redención de puntos está desactivada.");
            if (redimir > cliente.PuntosAcumulados)
                throw new VentaInvalidaException(
                    "El cliente no tiene puntos suficientes.");

            montoPuntos = redimir * valorPunto;
            if (montoPuntos > venta.Total)
                throw new VentaInvalidaException(
                    "Los puntos redimidos superan el total de la venta.");

            venta.AgregarPago(new PagoPuntos
            {
                Puntos = redimir,
                Monto = montoPuntos
            });
        }

        return (venta.Total - montoPuntos, redimir);
    }

    private async Task AplicarPuntosAsync(
        Venta venta,
        Cliente cliente,
        (long BaseGanancia, int PuntosRedimidos) puntos)
    {
        var pesosPorPunto = await parametros.ObtenerLongAsync(
            ClavesParametro.PesosPorPunto);
        var ganados = pesosPorPunto > 0
            ? (int)Math.Min(int.MaxValue, puntos.BaseGanancia / pesosPorPunto)
            : 0;

        if (puntos.PuntosRedimidos > 0)
            cliente.RedimirPuntos(puntos.PuntosRedimidos);
        cliente.AcumularPuntos(ganados);

        venta.ClienteId = cliente.Id;
        venta.PuntosGanados = ganados;
        venta.PuntosRedimidos = puntos.PuntosRedimidos;
        clientes.Actualizar(cliente);
    }

    private async Task RevertirPuntosAsync(Venta venta)
    {
        if (venta.ClienteId is null
            || (venta.PuntosGanados == 0 && venta.PuntosRedimidos == 0))
            return;

        var cliente = await clientes.ObtenerPorIdAsync(venta.ClienteId.Value);
        if (cliente is null)
            return;

        cliente.RevertirVenta(venta.PuntosGanados, venta.PuntosRedimidos);
        clientes.Actualizar(cliente);
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
}
