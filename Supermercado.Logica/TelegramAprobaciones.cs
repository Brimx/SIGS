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
public sealed partial class ProcesadorTelegram
{
    private async Task SolicitarAprobacionGerenteAsync(
        SolicitudHerramienta solicitud,
        VinculoTelegram solicitante,
        CancellationToken cancelacion)
    {
        VinculoTelegram? gerente = null;
        foreach (var enlace in vinculos.ObtenerTodos())
        {
            var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
            if (usuario?.Empleado?.Rol?.Nombre == NombresRol.Gerente
                && !usuario.DebeCambiarClave)
            {
                gerente = enlace;
                break;
            }
        }

        if (gerente is null)
        {
            await telegram.EnviarMensajeAsync(
                solicitante.ChatId,
                "No hay un Gerente vinculado para aprobar anulaciones.",
                cancelacion: cancelacion);
            return;
        }

        var aprobacion = vinculos.CrearAprobacion(
            HerramientaAsistente.SolicitarAnulacion,
            solicitud.Id,
            solicitud.Motivo,
            gerente.ChatId,
            gerente.UsuarioTelegramId,
            reloj.GetUtcNow().Add(DuracionConfirmacion));
        await telegram.EnviarMensajeAsync(
            gerente.ChatId,
            $"Solicitud de anulación de venta {solicitud.Id}. Motivo: {solicitud.Motivo}",
            [
                new BotonTelegram("Aprobar", $"a:{aprobacion.Token}"),
                new BotonTelegram("Rechazar", $"r:{aprobacion.Token}")
            ],
            cancelacion);
        await telegram.EnviarMensajeAsync(
            solicitante.ChatId,
            "Solicitud enviada al Gerente vinculado para aprobación.",
            cancelacion: cancelacion);
    }

    private async Task ProcesarCallbackAsync(
        ActualizacionTelegram actualizacion,
        long chatId,
        long usuarioTelegramId,
        CancellationToken cancelacion)
    {
        var partes = actualizacion.CallbackData!.Split(':', 2);
        if (partes.Length != 2 || actualizacion.CallbackId is null)
            return;

        var enlace = vinculos.Buscar(chatId);
        if (enlace is null || enlace.UsuarioTelegramId != usuarioTelegramId)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, "Chat no vinculado.", cancelacion);
            return;
        }

        var usuario = await ObtenerUsuarioVinculadoAsync(enlace);
        if (usuario is null)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, "La cuenta vinculada está inactiva.", cancelacion);
            return;
        }

        var esAprobacionGerente = partes[0] is "a" or "r";
        if (esAprobacionGerente
            && usuario.Empleado?.Rol?.Nombre != NombresRol.Gerente)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId,
                "Solo el Gerente vinculado puede resolver esta aprobación.",
                cancelacion);
            return;
        }
        if (usuario.DebeCambiarClave)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId,
                "Debes cambiar tu clave desde SIGS.",
                cancelacion);
            return;
        }

        if (!vinculos.TomarAprobacion(
            partes[1], chatId, usuarioTelegramId, reloj.GetUtcNow(), out var aprobacion)
            || aprobacion is null)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId,
                "Esta confirmación ya se usó o venció.",
                cancelacion);
            return;
        }

        using var contexto = sesion.UsarUsuario(usuario);
        try
        {
            if (partes[0] is "a" or "y")
            {
                if (aprobacion.Herramienta == HerramientaAsistente.SolicitarAnulacion)
                {
                    await ventas.AnularVentaAsync(
                        aprobacion.Id!.Value,
                        aprobacion.Motivo!,
                        "Telegram");
                    await telegram.EnviarMensajeAsync(
                        chatId,
                        $"Venta {aprobacion.Id} anulada.",
                        cancelacion: cancelacion);
                }
                else
                {
                    var generadas = await reabastecimiento.GenerarSolicitudesAsync("Telegram");
                    await telegram.EnviarMensajeAsync(
                        chatId,
                        $"Solicitudes de reabastecimiento generadas: {generadas.Count}.",
                        cancelacion: cancelacion);
                }
            }
            else
            {
                await telegram.EnviarMensajeAsync(
                    chatId,
                    "Solicitud rechazada o cancelada.",
                    cancelacion: cancelacion);
            }
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, "Listo.", cancelacion);
        }
        catch (AccesoDenegadoException)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, "No tienes permiso para esta acción.", cancelacion);
        }
        catch (DebeCambiarClaveException)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, "Debes cambiar tu clave desde SIGS.", cancelacion);
        }
        catch (Exception ex) when (
            ex is ValidacionException
                or VentaInvalidaException
                or TurnoInvalidoException)
        {
            await telegram.ResponderCallbackAsync(
                actualizacion.CallbackId, ex.Message, cancelacion);
        }
    }
}
}
