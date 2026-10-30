using System.Text;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Finanzas;
using Supermercado.Entidades.Inventario;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Reportes;
using Supermercado.Logica.Ventas;
using Supermercado.Pruebas.Falsos;

namespace Supermercado.Pruebas;

public class ReportesPdfTests
{
    private static void ValidarCabeceraPdf(byte[] pdf)
    {
        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 100);

        // Los primeros 4 bytes de un archivo PDF siempre son "%PDF"
        var cabecera = Encoding.ASCII.GetString(pdf.AsSpan(0, 4));
        Assert.Equal("%PDF", cabecera);
    }

    [Fact]
    public async Task GeneradorTicket_GeneraCabeceraPdfValida()
    {
        var parametros = new ParametrosFalsos();
        parametros.Valores[ClavesParametro.NegocioNombre] = "Negocio Prueba";
        parametros.Valores[ClavesParametro.NegocioNit] = "NIT de prueba";
        parametros.Valores[ClavesParametro.NegocioDireccion] = "Dirección";
        parametros.Valores[ClavesParametro.NegocioTelefono] = "Teléfono";
        var generador = new GeneradorTicketQuestPdf(parametros);

        var venta = new Venta
        {
            Id = 1,
            Fecha = DateTime.UtcNow,
            TurnoCajaId = 1
        };
        venta.Detalles.Add(new DetalleVenta
        {
            ProductoId = 1,
            Cantidad = 2,
            PrecioUnitario = 5000,
            Subtotal = 10000,
            Iva = 1600
        });
        venta.AgregarPago(new PagoEfectivo { Monto = 10000 });
        venta.Cerrar();

        var pdf = await generador.GenerarPdfAsync(venta);

        ValidarCabeceraPdf(pdf);
    }

    [Fact]
    public async Task DesprendibleNomina_GeneraCabeceraPdfValida()
    {
        var sesion = Sesiones.Con(Permiso.GestionarPersonal);
        var servicio = new ServicioReportesQuestPdf(sesion);

        var nomina = new Nomina
        {
            EmpleadoId = 10,
            Anio = 2026,
            Mes = 10,
            SalarioBase = 1500000,
            AuxilioTransporte = 162000,
            DeduccionSalud = 60000,
            DeduccionPension = 60000,
            Neto = 1542000
        };

        var pdf = await servicio.GenerarDesprendibleNominaAsync(nomina);

        ValidarCabeceraPdf(pdf);
    }

    [Fact]
    public async Task ReporteVentas_GeneraCabeceraPdfValida()
    {
        var sesion = Sesiones.Con(Permiso.VerReportes);
        var servicio = new ServicioReportesQuestPdf(sesion);

        var v = new Venta { Id = 1, Fecha = DateTime.UtcNow };
        v.Detalles.Add(new DetalleVenta { Subtotal = 5000 });
        v.AgregarPago(new PagoEfectivo { Monto = 5000 });
        v.Cerrar();

        var pdf = await servicio.GenerarReporteVentasAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            [v]);

        ValidarCabeceraPdf(pdf);
    }

    [Fact]
    public async Task ReporteInventario_GeneraCabeceraPdfValida()
    {
        var sesion = Sesiones.Con(Permiso.VerInventario);
        var servicio = new ServicioReportesQuestPdf(sesion);

        var item = new StockConsolidado(
            1, "Arroz", "7701", 10, "LOT-1",
            new DateOnly(2027, 1, 1), 20, 30, 50);

        var pdf = await servicio.GenerarReporteInventarioAsync([item]);

        ValidarCabeceraPdf(pdf);
    }

    [Fact]
    public async Task ReporteArqueo_GeneraCabeceraPdfValida()
    {
        var sesion = Sesiones.Con(Permiso.AbrirCerrarTurno);
        var servicio = new ServicioReportesQuestPdf(sesion);

        var turno = TurnoCaja.Abrir(1, 1, 50000, DateTime.UtcNow);
        turno.Cerrar(DateTime.UtcNow.AddHours(8), 80000, 80000);

        var arqueo = new ResultadoArqueo(80000, 80000, 0, true);

        var pdf = await servicio.GenerarReporteArqueoAsync(turno, arqueo);

        ValidarCabeceraPdf(pdf);
    }

    [Fact]
    public async Task Reportes_SinPermisoRequerido_LanzaAccesoDenegado()
    {
        var sesion = Sesiones.Con(Permiso.Ninguno);
        var servicio = new ServicioReportesQuestPdf(sesion);

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.GenerarDesprendibleNominaAsync(new Nomina()));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.GenerarReporteVentasAsync(
                DateTime.UtcNow, DateTime.UtcNow, []));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.GenerarReporteInventarioAsync([]));

        await Assert.ThrowsAsync<AccesoDenegadoException>(
            () => servicio.GenerarReporteArqueoAsync(
                new TurnoCaja(), new ResultadoArqueo(0, 0, 0, true)));
    }
}
