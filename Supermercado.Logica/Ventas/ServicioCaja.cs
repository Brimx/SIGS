using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Ventas;

public class ServicioCaja(
    ITurnoCajaRepositorio turnos,
    IVentaRepositorio ventas,
    IRepositorio<Caja> cajas,
    IServicioParametros parametros,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo,
    TimeProvider reloj) : IServicioCaja
{
    public async Task<TurnoCaja> AbrirTurnoAsync(
        int cajaId,
        long efectivoInicial)
    {
        sesion.Exigir(Permiso.AbrirCerrarTurno);

        var caja = await cajas.ObtenerPorIdAsync(cajaId)
            ?? throw new ValidacionException("La caja no existe.");

        if (!caja.Activa)
            throw new ValidacionException("La caja está inactiva.");

        if (await ObtenerTurnoActualAsync() is not null)
            throw new TurnoInvalidoException("Ya tienes un turno abierto.");

        if (await turnos.ObtenerAbiertoPorCajaAsync(cajaId) is not null)
            throw new TurnoInvalidoException("La caja ya tiene un turno abierto.");

        var turno = TurnoCaja.Abrir(
            cajaId, sesion.EmpleadoId, efectivoInicial, reloj.AhoraUtc());

        await turnos.AgregarAsync(turno);
        await auditoria.RegistrarAsync(
            AccionAuditoria.AperturaTurno, nameof(TurnoCaja), turno.Id);
        await trabajo.GuardarAsync();
        return turno;
    }

    public async Task<ResultadoArqueo> CerrarTurnoAsync(long efectivoContado)
    {
        sesion.Exigir(Permiso.AbrirCerrarTurno);

        var turno = await ObtenerTurnoActualAsync()
            ?? throw new TurnoInvalidoException("No hay un turno abierto.");
        var delTurno = await ventas.ObtenerPorTurnoAsync(turno.Id);
        var esperado = turno.CalcularEfectivoEsperado(delTurno);

        turno.Cerrar(reloj.AhoraUtc(), esperado, efectivoContado);
        turnos.Actualizar(turno);

        var diferencia = efectivoContado - esperado;
        var tolerancia = await parametros.ObtenerLongAsync(
            ClavesParametro.ToleranciaArqueo);

        await auditoria.RegistrarAsync(
            AccionAuditoria.CierreTurno,
            nameof(TurnoCaja),
            turno.Id,
            $"Diferencia de arqueo: {diferencia}");
        await trabajo.GuardarAsync();

        return new ResultadoArqueo(
            esperado,
            efectivoContado,
            diferencia,
            Math.Abs(diferencia) <= tolerancia);
    }

    public Task<TurnoCaja?> ObtenerTurnoActualAsync()
        => turnos.ObtenerAbiertoPorEmpleadoAsync(sesion.EmpleadoId);
}
