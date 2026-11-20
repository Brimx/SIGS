using Supermercado.Entidades.Base;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Personal;

namespace Supermercado.Entidades.Finanzas
{
public class Nomina : EntidadBase
{
    public int EmpleadoId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public long SalarioBase { get; set; }
    public long AuxilioTransporte { get; set; }
    public long DeduccionSalud { get; set; }
    public long DeduccionPension { get; set; }
    public long Neto { get; set; }
    public DateTime FechaCalculo { get; set; }

    public static Nomina Calcular(
        Empleado empleado,
        int anio,
        int mes,
        ParametrosNomina parametros,
        DateTime ahora)
    {
        if (anio <= 0)
            throw new ValidacionException("El año de la nómina no es válido.");

        if (mes is < 1 or > 12)
            throw new ValidacionException("El mes de la nómina no es válido.");

        if (parametros.SalarioMinimo <= 0)
            throw new ParametroFaltanteException(ClavesParametro.SalarioMinimo);

        if (parametros.AuxilioTransporte < 0
            || parametros.TopeAuxilioEnSmmlv < 0
            || parametros.SaludPuntosBasicos < 0
            || parametros.PensionPuntosBasicos < 0)
            throw new ValidacionException(
                "Los parámetros de nómina no pueden ser negativos.");

        var tope = parametros.SalarioMinimo * parametros.TopeAuxilioEnSmmlv;
        var auxilio = empleado.SalarioBase <= tope
            ? parametros.AuxilioTransporte
            : 0;
        var salud = Dinero.Porcentaje(
            empleado.SalarioBase, parametros.SaludPuntosBasicos);
        var pension = Dinero.Porcentaje(
            empleado.SalarioBase, parametros.PensionPuntosBasicos);

        return new Nomina
        {
            EmpleadoId = empleado.Id,
            Anio = anio,
            Mes = mes,
            SalarioBase = empleado.SalarioBase,
            AuxilioTransporte = auxilio,
            DeduccionSalud = salud,
            DeduccionPension = pension,
            Neto = empleado.SalarioBase + auxilio - salud - pension,
            FechaCalculo = ahora
        };
    }
}

public class Gasto : EntidadBase
{
    public string Descripcion { get; set; } = string.Empty;
    public long Monto { get; set; }
    public DateTime Fecha { get; set; }
    public int RegistradoPorEmpleadoId { get; set; }
}

// PuntosBasicos es entero, igual que el dinero: 1900 equivale a 19 %.
public abstract class Impuesto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public int PuntosBasicos { get; set; }
    public bool Activo { get; set; } = true;

    public decimal Porcentaje
        => PuntosBasicos * 100m / Dinero.PuntosBasicosCien;

    public abstract long Calcular(long baseGravable);
}

// El precio de venta ya incluye IVA: se extrae, no se suma.
public sealed class Iva : Impuesto
{
    public override long Calcular(long baseGravable)
        => Dinero.DividirRedondeando(
            baseGravable * PuntosBasicos,
            Dinero.PuntosBasicosCien + PuntosBasicos);
}

public record ParametrosNomina(
    long SalarioMinimo,
    long AuxilioTransporte,
    int TopeAuxilioEnSmmlv,
    int SaludPuntosBasicos,
    int PensionPuntosBasicos);

public sealed class Retencion : Impuesto
{
    public long BaseMinima { get; set; }

    public override long Calcular(long baseGravable)
        => baseGravable < BaseMinima
            ? 0
            : Dinero.Porcentaje(baseGravable, PuntosBasicos);
}
}
