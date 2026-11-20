using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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

        servicios.AddSingleton<SesionActual>();
        servicios.AddSingleton<IHasheadorClave, HasheadorBCrypt>();
        servicios.AddScoped<EstrategiaSalida, SalidaFefo>();

        // Servicios de dominio consolidados (8 módulos de negocio)
        servicios.AddScoped<ServicioAuditoria>();
        servicios.AddScoped<ServicioParametros>();
        servicios.AddScoped<ServicioAutenticacion>();
        servicios.AddScoped<ServicioInicializacion>();

        servicios.AddScoped<ServicioPersonal>();
        servicios.AddScoped<ServicioProductos>();
        servicios.AddScoped<ServicioInventario>();
        servicios.AddScoped<ServicioCaja>();
        servicios.AddScoped<ServicioVentas>();
        servicios.AddScoped<ServicioFinanzas>();
        servicios.AddScoped<ServicioClientes>();
        servicios.AddScoped<ServicioDashboard>();
        servicios.AddScoped<IServicioTrm, ServicioTrm>();

        servicios.AddScoped<IClienteGemini, ClienteGeminiHttp>();
        servicios.AddSingleton<IClienteTelegram, ClienteTelegramHttp>();
        servicios.AddSingleton<IVinculosTelegram, VinculosTelegram>();
        servicios.AddScoped<Asistente>();
        servicios.AddScoped<ProcesadorTelegram>();
        servicios.AddHostedService<TelegramWorker>();
        servicios.AddScoped<GeneradorDatosDemo>();

        // Clientes externos: por defecto no hacen nada. Se reemplazan
        // registrando la implementación real después de este método.
        servicios.AddScoped<ITrmClient, ClienteTrmNoConfigurado>();
        servicios.AddScoped<IConsultaCodigoBarras, ConsultaCodigoBarrasNula>();
        servicios.AddScoped<IVerificadorCorreo, VerificadorCorreoNulo>();

        servicios.AddScoped<IWidget, VentasDelDiaWidget>();
        servicios.AddScoped<IWidget, ResumenOperativoWidget>();
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
        servicios.AddScoped<TraductorExcepciones>();

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
