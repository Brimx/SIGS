using Microsoft.Extensions.DependencyInjection;
using Supermercado.Datos;
using Supermercado.Logica.Dashboards;
using Supermercado.Logica.Dashboards.Widgets;
using Supermercado.Logica.DatosDemo;
using Supermercado.Logica.Finanzas;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Personal;
using Supermercado.Logica.Productos;
using Supermercado.Logica.Reportes;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Servicios;
using Supermercado.Logica.Ventas;

namespace Supermercado.Logica;

public static class ExtensionesLogica
{
    // La GUI solo llama a estos métodos; así no referencia Datos.
    public static IServiceCollection AgregarLogica(
        this IServiceCollection servicios)
        => servicios.AgregarLogica(RutasDatos.BaseDatosPorDefecto());

    public static IServiceCollection AgregarLogica(
        this IServiceCollection servicios,
        string rutaBaseDatos)
    {
        servicios.AgregarDatos(rutaBaseDatos);

        servicios.AddSingleton<ISesionActual, SesionActual>();
        servicios.AddSingleton<IHasheadorClave, HasheadorBCrypt>();
        servicios.AddScoped<IEstrategiaSalida, SalidaFefo>();

        servicios.AddScoped<IServicioAuditoria, ServicioAuditoria>();
        servicios.AddScoped<IServicioConsultaAuditoria,
            ServicioConsultaAuditoria>();
        servicios.AddScoped<IServicioParametros, ServicioParametros>();
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        servicios.AddScoped<IServicioInicializacion, ServicioInicializacion>();
        servicios.AddScoped<IServicioProductos, ServicioProductos>();
        servicios.AddScoped<IServicioBusquedaProductos,
            ServicioBusquedaProductos>();
        servicios.AddScoped<IServicioMantenimientoProductos,
            ServicioMantenimientoProductos>();
        servicios.AddScoped<IServicioCatalogos, ServicioCatalogos>();
        servicios.AddScoped<IServicioInventario, ServicioInventario>();
        servicios.AddScoped<IServicioConsultaInventario,
            ServicioConsultaInventario>();
        servicios.AddScoped<IServicioMermas, ServicioMermas>();
        servicios.AddScoped<IServicioAlertasInventario,
            ServicioAlertasInventario>();
        servicios.AddScoped<IServicioReabastecimiento,
            ServicioReabastecimiento>();
        servicios.AddScoped<IServicioCaja, ServicioCaja>();
        servicios.AddScoped<IServicioVentas, ServicioVentas>();
        servicios.AddScoped<IServicioEmpleados, ServicioEmpleados>();
        servicios.AddScoped<IServicioEstructura, ServicioEstructura>();
        servicios.AddScoped<IServicioImpuestos, ServicioImpuestos>();
        servicios.AddScoped<IServicioFinanzas, ServicioFinanzas>();
        servicios.AddScoped<IServicioNomina, ServicioNomina>();
        servicios.AddScoped<IServicioDashboard, ServicioDashboard>();
        servicios.AddScoped<IServicioTrm, ServicioTrm>();
        servicios.AddScoped<IGeneradorDatosDemo, GeneradorDatosDemo>();

        // Clientes externos: por defecto no hacen nada. Se reemplazan
        // registrando la implementación real después de este método.
        servicios.AddScoped<ITrmClient, ClienteTrmNoConfigurado>();
        servicios.AddScoped<IConsultaCodigoBarras, ConsultaCodigoBarrasNula>();
        servicios.AddScoped<IVerificadorCorreo, VerificadorCorreoNulo>();

        servicios.AddScoped<IWidget, VentasDelDiaWidget>();
        servicios.AddScoped<IWidget, ReabastecimientosPendientesWidget>();
        servicios.AddScoped<IWidget, TrmDelDiaWidget>();
        servicios.AddScoped<IWidget, IvaGeneradoWidget>();
        servicios.AddScoped<IWidget, MargenPorCategoriaWidget>();
        servicios.AddScoped<IWidget, LotesPorVencerWidget>();
        servicios.AddScoped<IWidget, StockBajoMinimoWidget>();
        servicios.AddScoped<IWidget, VentasPorCategoriaWidget>();
        servicios.AddScoped<IWidget, ArqueoTurnoWidget>();

        // Reportes y tickets con QuestPDF
        servicios.AddScoped<IGeneradorTicket, GeneradorTicketQuestPdf>();
        servicios.AddScoped<IServicioReportes, ServicioReportesQuestPdf>();

        return servicios;
    }
}
