using Microsoft.Extensions.DependencyInjection;
using Supermercado.Datos;
using Supermercado.Logica.Dashboards;
using Supermercado.Logica.Dashboards.Widgets;
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
        servicios.AddScoped<IServicioParametros, ServicioParametros>();
        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioInicializacion, ServicioInicializacion>();
        servicios.AddScoped<IServicioProductos, ServicioProductos>();
        servicios.AddScoped<IServicioInventario, ServicioInventario>();
        servicios.AddScoped<IServicioMermas, ServicioMermas>();
        servicios.AddScoped<IServicioAlertasInventario,
            ServicioAlertasInventario>();
        servicios.AddScoped<IServicioReabastecimiento,
            ServicioReabastecimiento>();
        servicios.AddScoped<IServicioCaja, ServicioCaja>();
        servicios.AddScoped<IServicioVentas, ServicioVentas>();
        servicios.AddScoped<IServicioEmpleados, ServicioEmpleados>();
        servicios.AddScoped<IServicioImpuestos, ServicioImpuestos>();
        servicios.AddScoped<IServicioFinanzas, ServicioFinanzas>();
        servicios.AddScoped<IServicioNomina, ServicioNomina>();
        servicios.AddScoped<IServicioDashboard, ServicioDashboard>();
        servicios.AddScoped<IServicioTrm, ServicioTrm>();

        // Clientes externos: por defecto no hacen nada. Se reemplazan
        // registrando la implementación real después de este método.
        servicios.AddScoped<ITrmClient, ClienteTrmNoConfigurado>();
        servicios.AddScoped<IConsultaCodigoBarras, ConsultaCodigoBarrasNula>();
        servicios.AddScoped<IVerificadorCorreo, VerificadorCorreoNulo>();

        servicios.AddScoped<IWidget, VentasDelDiaWidget>();
        servicios.AddScoped<IWidget, ReabastecimientosPendientesWidget>();
        servicios.AddScoped<IWidget, TrmDelDiaWidget>();

        // PDF no disponible en esta iteración: implementación nula hasta integrar QuestPDF.
        servicios.AddScoped<IGeneradorTicket, GeneradorTicketNulo>();

        return servicios;
    }
}
