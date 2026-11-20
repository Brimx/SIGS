using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Inventario;
using Supermercado.Logica.Seguridad;
using Supermercado.Logica.Ventas;

namespace Supermercado.Logica.Servicios
{
public sealed partial class ProcesadorTelegram(
    IClienteTelegram telegram,
    IAsistente asistente,
    IVinculosTelegram vinculos,
    IUsuarioRepositorio usuarios,
    ISesionActual sesion,
    IServicioAlertasInventario alertas,
    IServicioVentas ventas,
    IVentaRepositorio repositorioVentas,
    IServicioCaja caja,
    IServicioReabastecimiento reabastecimiento,
    IServicioAuditoria auditoria,
    TimeProvider reloj)
{
    private static readonly TimeSpan DuracionConfirmacion = TimeSpan.FromMinutes(3);

    public async Task ProcesarAsync(
        ActualizacionTelegram actualizacion,
        CancellationToken cancelacion = default)
    {
        if (!actualizacion.ChatPrivado
            || actualizacion.ChatId is not long chatId
            || actualizacion.UsuarioId is not long usuarioTelegramId)
            return;

        if (actualizacion.CallbackData is not null)
        {
            await ProcesarCallbackAsync(
                actualizacion, chatId, usuarioTelegramId, cancelacion);
            return;
        }

        if (actualizacion.Texto is not string texto)
            return;

        var partes = texto.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var comando = partes.Length == 0
            ? null
            : partes[0].Split('@')[0].ToLowerInvariant();

        if (comando is "/start" or "/desvincular" or "/quien")
        {
            await ProcesarComandoCuentaAsync(
                comando, partes, chatId, usuarioTelegramId, cancelacion);
            return;
        }

        var enlace = vinculos.Buscar(chatId);
        if (enlace is null)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este bot solo responde a chats vinculados. Genera un código en Configuración y envía /start CODIGO en un chat privado.",
                cancelacion: cancelacion);
            return;
        }

        if (enlace.UsuarioTelegramId != usuarioTelegramId)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este chat está vinculado a otra cuenta de Telegram.",
                cancelacion: cancelacion);
            return;
        }

        if (!vinculos.PermitirMensaje(chatId, reloj.GetUtcNow()))
            return;

        var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
        if (usuario is null)
        {
            vinculos.Desvincular(chatId);
            await telegram.EnviarMensajeAsync(
                chatId,
                "La cuenta vinculada ya no está activa. Genera un nuevo código desde Configuración.",
                cancelacion: cancelacion);
            return;
        }

        using var contexto = sesion.UsarUsuario(usuario);
        if (sesion.DebeCambiarClave)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Debes cambiar tu clave desde SIGS antes de usar Telegram.",
                cancelacion: cancelacion);
            return;
        }

        SolicitudHerramienta? solicitud;
        try
        {
            solicitud = await asistente.InterpretarAsync(texto, cancelacion);
        }
        catch (System.Text.Json.JsonException)
        {
            await telegram.EnviarMensajeAsync(chatId, "Gemini respondió con un formato inválido. Usa los comandos fijos.", cancelacion: cancelacion);
            return;
        }
        catch (GeminiNoDisponibleException ex)
        {
            await telegram.EnviarMensajeAsync(chatId, ex.Message, cancelacion: cancelacion);
            return;
        }

        if (solicitud is null)
        {
            var mensaje = texto.StartsWith('/')
                ? "Comandos disponibles: /stock, /ventas, /caja y /resumen."
                : "No entendí la solicitud. Prueba /stock, /ventas, /caja o /resumen.";
            await telegram.EnviarMensajeAsync(chatId, mensaje, cancelacion: cancelacion);
            return;
        }

        try
        {
            await EjecutarHerramientaAsync(
                solicitud, enlace, usuarioTelegramId, cancelacion);
        }
        catch (AccesoDenegadoException)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Tu usuario no tiene autorización para esa consulta o acción.",
                cancelacion: cancelacion);
        }
        catch (DebeCambiarClaveException)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Debes cambiar tu clave desde SIGS antes de usar Telegram.",
                cancelacion: cancelacion);
        }
        catch (Exception ex) when (
            ex is ValidacionException
                or VentaInvalidaException
                or TurnoInvalidoException)
        {
            await telegram.EnviarMensajeAsync(chatId, ex.Message, cancelacion: cancelacion);
        }
    }

    public async Task EnviarResumenProgramadoAsync(CancellationToken cancelacion)
    {
        foreach (var enlace in vinculos.ObtenerTodos())
        {
            var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
            if (usuario?.Empleado?.Rol?.Nombre != NombresRol.Gerente
                || usuario.DebeCambiarClave)
                continue;

            using var contexto = sesion.UsarUsuario(usuario);
            var resumen = await ConstruirResumenAsync();
            await telegram.EnviarMensajeAsync(
                enlace.ChatId,
                $"Resumen diario programado\n{resumen}",
                cancelacion: cancelacion);
        }
    }

    private async Task ProcesarComandoCuentaAsync(
        string comando,
        string[] partes,
        long chatId,
        long usuarioTelegramId,
        CancellationToken cancelacion)
    {
        if (!vinculos.PermitirMensaje(chatId, reloj.GetUtcNow()))
            return;

        var enlace = vinculos.Buscar(chatId);

        switch (comando)
        {
            case "/start":
                if (partes.Length < 2)
                {
                    await ResponderStartSinCodigoAsync(enlace, chatId, cancelacion);
                    return;
                }

                await VincularAsync(
                    partes[1], chatId, usuarioTelegramId,
                    enlace is not null, cancelacion);
                return;

            case "/desvincular":
                await DesvincularAsync(
                    enlace, chatId, usuarioTelegramId, cancelacion);
                return;

            case "/quien":
                await ResponderQuienAsync(
                    enlace, chatId, usuarioTelegramId, cancelacion);
                return;
        }
    }

    private async Task ResponderStartSinCodigoAsync(
        VinculoTelegram? enlace,
        long chatId,
        CancellationToken cancelacion)
    {
        if (enlace is null)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Genera un código en Configuración y envía /start CODIGO en un chat privado para vincular este chat.",
                cancelacion: cancelacion);
            return;
        }

        var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
        if (usuario is null)
        {
            vinculos.Desvincular(chatId);
            await telegram.EnviarMensajeAsync(
                chatId,
                "La cuenta vinculada ya no está activa. Genera un nuevo código desde Configuración.",
                cancelacion: cancelacion);
            return;
        }

        await telegram.EnviarMensajeAsync(
            chatId,
            $"Este chat está vinculado con {usuario.NombreUsuario} (rol {RolDelUsuario(usuario)}). Para cambiarlo, genera un código en Configuración y envía /start CODIGO.",
            cancelacion: cancelacion);
    }

    private async Task DesvincularAsync(
        VinculoTelegram? enlace,
        long chatId,
        long usuarioTelegramId,
        CancellationToken cancelacion)
    {
        if (enlace is null)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este chat no está vinculado.",
                cancelacion: cancelacion);
            return;
        }

        if (enlace.UsuarioTelegramId != usuarioTelegramId)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este chat está vinculado a otra cuenta de Telegram.",
                cancelacion: cancelacion);
            return;
        }

        var usuario = await usuarios.ObtenerPorNombreAsync(enlace.NombreUsuario);
        vinculos.Desvincular(chatId);
        if (usuario?.EmpleadoId == enlace.EmpleadoId)
        {
            using var contexto = sesion.UsarUsuario(usuario);
            await auditoria.RegistrarAsync(
                AccionAuditoria.VinculoTelegram,
                nameof(Usuario),
                usuario.Id,
                "Origen: Telegram | Chat desvinculado");
            await usuarios.GuardarAsync();
        }

        await telegram.EnviarMensajeAsync(
            chatId,
            "Desvinculación completada. Genera un código en Configuración para volver a vincular.",
            cancelacion: cancelacion);
    }

    private async Task ResponderQuienAsync(
        VinculoTelegram? enlace,
        long chatId,
        long usuarioTelegramId,
        CancellationToken cancelacion)
    {
        if (enlace is null)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este chat no está vinculado. Genera un código en Configuración y envía /start CODIGO.",
                cancelacion: cancelacion);
            return;
        }

        if (enlace.UsuarioTelegramId != usuarioTelegramId)
        {
            await telegram.EnviarMensajeAsync(
                chatId,
                "Este chat está vinculado a otra cuenta de Telegram.",
                cancelacion: cancelacion);
            return;
        }

        var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
        if (usuario is null)
        {
            vinculos.Desvincular(chatId);
            await telegram.EnviarMensajeAsync(
                chatId,
                "La cuenta vinculada ya no está activa. Genera un nuevo código desde Configuración.",
                cancelacion: cancelacion);
            return;
        }

        await telegram.EnviarMensajeAsync(
            chatId,
            $"Vinculado como {usuario.NombreUsuario} (rol {RolDelUsuario(usuario)}).",
            cancelacion: cancelacion);
    }

    private static string RolDelUsuario(Usuario usuario)
        => usuario.Empleado?.Rol?.Nombre ?? "sin rol";

    private async Task VincularAsync(
        string codigo,
        long chatId,
        long usuarioTelegramId,
        bool yaVinculado,
        CancellationToken cancelacion)
    {
        var resultado = vinculos.Vincular(codigo, chatId, usuarioTelegramId);
        if (resultado != ResultadoVinculacion.Vinculado)
        {
            var mensaje = resultado == ResultadoVinculacion.CodigoVencido
                ? "El código venció. Genera uno nuevo desde Configuración."
                : "El código es inválido o ya fue usado. Genera uno nuevo desde Configuración.";
            await telegram.EnviarMensajeAsync(
                chatId, mensaje, cancelacion: cancelacion);
            return;
        }

        var enlace = vinculos.Buscar(chatId)!;
        var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
        if (usuario is null)
        {
            vinculos.Desvincular(chatId);
            await telegram.EnviarMensajeAsync(
                chatId,
                "La cuenta dejó de estar activa. Genera un código nuevo desde SIGS.",
                cancelacion: cancelacion);
            return;
        }

        using var contexto = sesion.UsarUsuario(usuario);
        await auditoria.RegistrarAsync(
            AccionAuditoria.VinculoTelegram,
            nameof(Usuario),
            usuario.Id,
            yaVinculado
                ? "Origen: Telegram | Chat revinculado con código de un solo uso"
                : "Origen: Telegram | Chat vinculado con código de un solo uso");
        await usuarios.GuardarAsync();
        await telegram.EnviarMensajeAsync(
            chatId,
            "Vinculación completada. Usa /stock, /ventas, /caja o /resumen.",
            cancelacion: cancelacion);
    }

    private async Task EjecutarHerramientaAsync(
        SolicitudHerramienta solicitud,
        VinculoTelegram enlace,
        long usuarioTelegramId,
        CancellationToken cancelacion)
    {
        switch (solicitud.Herramienta)
        {
            case HerramientaAsistente.StockBajo:
                sesion.Exigir(Permiso.VerInventario);
                var bajoMinimo = await alertas.ObtenerBajoMinimoAsync();
                var lista = bajoMinimo.Count == 0
                    ? "No hay productos por debajo del mínimo."
                    : string.Join("\n", bajoMinimo.Take(15).Select(item =>
                        $"• {item.Producto.Nombre}: {item.CantidadEnEstante} (mínimo {item.Producto.StockMinimoEstante})"));
                await telegram.EnviarMensajeAsync(
                    enlace.ChatId, lista, cancelacion: cancelacion);
                break;

            case HerramientaAsistente.VentasDelDia:
                sesion.Exigir(Permiso.VerReportes);
                await telegram.EnviarMensajeAsync(
                    enlace.ChatId,
                    await ResumenVentasAsync(),
                    cancelacion: cancelacion);
                break;

            case HerramientaAsistente.EstadoCaja:
                sesion.Exigir(Permiso.VerFinanzas);
                var turno = await caja.ObtenerTurnoActualAsync();
                var estado = turno is null
                    ? "No tienes un turno de caja abierto."
                    : $"Turno {turno.Id} abierto desde {turno.FechaApertura.ToLocalTime():HH:mm}. Efectivo inicial: ${turno.EfectivoInicial:N0}.";
                await telegram.EnviarMensajeAsync(
                    enlace.ChatId, estado, cancelacion: cancelacion);
                break;

            case HerramientaAsistente.ResumenDiario:
                await telegram.EnviarMensajeAsync(
                    enlace.ChatId,
                    await ConstruirResumenAsync(),
                    cancelacion: cancelacion);
                break;

            case HerramientaAsistente.SolicitarAnulacion:
                if (solicitud.Id is not > 0
                    || string.IsNullOrWhiteSpace(solicitud.Motivo))
                {
                    await telegram.EnviarMensajeAsync(
                        enlace.ChatId,
                        "Indica el número de venta y el motivo para solicitar su anulación.",
                        cancelacion: cancelacion);
                    break;
                }
                await SolicitarAprobacionGerenteAsync(solicitud, enlace, cancelacion);
                break;

            case HerramientaAsistente.GenerarReabastecimiento:
                sesion.Exigir(Permiso.Reabastecer);
                var aprobacion = vinculos.CrearAprobacion(
                    HerramientaAsistente.GenerarReabastecimiento,
                    null,
                    null,
                    enlace.ChatId,
                    usuarioTelegramId,
                    reloj.GetUtcNow().Add(DuracionConfirmacion));
                await telegram.EnviarMensajeAsync(
                    enlace.ChatId,
                    "¿Confirmas generar solicitudes para los productos con stock bajo?",
                    [
                        new BotonTelegram("Confirmar", $"y:{aprobacion.Token}"),
                        new BotonTelegram("Cancelar", $"n:{aprobacion.Token}")
                    ],
                    cancelacion);
                break;
        }
    }

    private async Task<Usuario?> ObtenerUsuarioVinculadoAsync(VinculoTelegram enlace)
    {
        var usuario = await usuarios.ObtenerPorNombreAsync(enlace.NombreUsuario);
        return usuario?.EmpleadoId == enlace.EmpleadoId
            && usuario.Empleado?.Activo == true
            && !usuario.Bloqueado
            ? usuario
            : null;
    }

    private async Task<string> ResumenVentasAsync()
    {
        var hoy = reloj.HoyLocal();
        var inicioLocal = hoy.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var finLocal = hoy.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var ventasDelDia = await repositorioVentas.ObtenerPorRangoAsync(
            inicioLocal.ToUniversalTime(), finLocal.ToUniversalTime());
        var completadas = ventasDelDia
            .Where(venta => venta.Estado == EstadoVenta.Completada)
            .ToList();
        var total = completadas.Sum(venta => venta.Total);
        return $"Ventas de hoy: {completadas.Count} | Total: ${total:N0}.";
    }

    private async Task<string> ConstruirResumenAsync()
    {
        var filas = new List<string>();
        if (sesion.Tiene(Permiso.VerReportes))
            filas.Add(await ResumenVentasAsync());
        if (sesion.Tiene(Permiso.VerFinanzas))
        {
            var turno = await caja.ObtenerTurnoActualAsync();
            filas.Add(turno is null
                ? "Caja: sin turno abierto para este usuario."
                : $"Caja: turno {turno.Id} abierto, inicial ${turno.EfectivoInicial:N0}.");
        }
        if (sesion.Tiene(Permiso.VerInventario))
        {
            var bajoMinimo = await alertas.ObtenerBajoMinimoAsync();
            filas.Add($"Stock bajo: {bajoMinimo.Count} productos.");
        }
        return filas.Count == 0
            ? "Tu rol no tiene permisos para consultar el resumen."
            : string.Join("\n", filas);
    }
}
}
