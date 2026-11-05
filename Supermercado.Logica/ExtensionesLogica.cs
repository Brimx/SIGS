using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Supermercado.Datos;
using Supermercado.Logica.Clientes;
using Supermercado.Logica.Comunes;
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

        // Servicios de dominio consolidados (8 módulos de negocio)
        servicios.AddScoped<ServicioAuditoria>();
        servicios.AddScoped<IServicioAuditoria>(sp => sp.GetRequiredService<ServicioAuditoria>());
        servicios.AddScoped<IServicioConsultaAuditoria>(sp => sp.GetRequiredService<ServicioAuditoria>());

        servicios.AddScoped<IServicioParametros, ServicioParametros>();
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioInicializacion, ServicioInicializacion>();

        servicios.AddScoped<ServicioPersonal>();
        servicios.AddScoped<IServicioPersonal>(sp => sp.GetRequiredService<ServicioPersonal>());
        servicios.AddScoped<IServicioEmpleados>(sp => sp.GetRequiredService<ServicioPersonal>());
        servicios.AddScoped<IServicioEstructura>(sp => sp.GetRequiredService<ServicioPersonal>());
        servicios.AddScoped<IServicioUsuarios>(sp => sp.GetRequiredService<ServicioPersonal>());

        servicios.AddScoped<ServicioProductos>();
        servicios.AddScoped<IServicioProductos>(sp => sp.GetRequiredService<ServicioProductos>());
        servicios.AddScoped<IServicioBusquedaProductos>(sp => sp.GetRequiredService<ServicioProductos>());
        servicios.AddScoped<IServicioMantenimientoProductos>(sp => sp.GetRequiredService<ServicioProductos>());
        servicios.AddScoped<IServicioCatalogos>(sp => sp.GetRequiredService<ServicioProductos>());

        servicios.AddScoped<ServicioInventario>();
        servicios.AddScoped<IServicioInventario>(sp => sp.GetRequiredService<ServicioInventario>());
        servicios.AddScoped<IServicioConsultaInventario>(sp => sp.GetRequiredService<ServicioInventario>());
        servicios.AddScoped<IServicioMermas>(sp => sp.GetRequiredService<ServicioInventario>());
        servicios.AddScoped<IServicioAlertasInventario>(sp => sp.GetRequiredService<ServicioInventario>());
        servicios.AddScoped<IServicioReabastecimiento>(sp => sp.GetRequiredService<ServicioInventario>());

        servicios.AddScoped<IServicioCaja, ServicioCaja>();
        servicios.AddScoped<ServicioVentas>();
        servicios.AddScoped<IServicioVentas>(sp => sp.GetRequiredService<ServicioVentas>());
        servicios.AddScoped<IServicioVentasEnEspera>(sp => sp.GetRequiredService<ServicioVentas>());

        servicios.AddScoped<ServicioFinanzas>();
        servicios.AddScoped<IServicioFinanzas>(sp => sp.GetRequiredService<ServicioFinanzas>());
        servicios.AddScoped<IServicioImpuestos>(sp => sp.GetRequiredService<ServicioFinanzas>());
        servicios.AddScoped<IServicioNomina>(sp => sp.GetRequiredService<ServicioFinanzas>());

        servicios.AddScoped<IServicioClientes, ServicioClientes>();
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

        // Respaldo, traducción de errores y registro a archivo
        servicios.AddScoped<IServicioRespaldo, ServicioRespaldo>();
        servicios.AddSingleton<ILoggerProvider>(
            sp => new ProveedorLogArchivo(
                RutasDatos.ArchivoLogPorDefecto(),
                sp.GetService<TimeProvider>()));
        servicios.AddScoped<ITraductorExcepciones, TraductorExcepciones>();

        // APIs abiertas activas por defecto; sin internet degradan solas.
        servicios.AgregarClientesExternos();

        return servicios;
    }

    public static IServiceCollection AgregarClientesExternos(
        this IServiceCollection servicios,
        OpcionesApisExternas? opciones = null,
        HttpClient? http = null)
    {
        servicios.AddSingleton(http ?? new HttpClient());
        var config = opciones ?? OpcionesApisExternas.DesdeEntorno();
        servicios.AddSingleton(config);

        if (!string.IsNullOrWhiteSpace(config.TrmUrl))
            servicios.AddScoped<ITrmClient, ClienteTrmDatosGov>();
        if (!string.IsNullOrWhiteSpace(config.CodigoBarrasBaseUrl))
            servicios.AddScoped<
                IConsultaCodigoBarras, ClienteCodigoBarrasOff>();
        if (!string.IsNullOrWhiteSpace(config.VerificadorCorreoUrl))
            servicios.AddScoped<
                IVerificadorCorreo, ClienteVerificadorCorreoHttp>();

        return servicios;
    }
}
