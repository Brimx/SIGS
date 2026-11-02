using Supermercado.Entidades.Excepciones;

namespace Supermercado.Logica.Productos;

public record FactorConversionUnidad(
    string UnidadOrigen,
    string UnidadDestino,
    decimal Factor);

// Conversor de unidades de compra a unidades de venta para inventario y Kardex.
// Permite ingresar bultos, arrobas o cajas y transformarlos a kilos, gramos o unidades.
public static class ConversorUnidades
{
    public const string Bulto50Kg = "Bulto50Kg";
    public const string Arroba = "Arroba";
    public const string Libra = "Libra";
    public const string Kilo = "Kilo";
    public const string Gramo = "Gramo";
    public const string Caja = "Caja";
    public const string Unidad = "Unidad";

    private static readonly Dictionary<string, decimal> FactoresAGramos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Bulto50Kg] = 50_000m,
            [Arroba] = 12_500m,
            [Kilo] = 1_000m,
            [Libra] = 500m,
            [Gramo] = 1m
        };

    public static int ConvertirAGramos(decimal cantidad, string unidadOrigen)
    {
        if (cantidad <= 0)
            throw new ValidacionException("La cantidad debe ser mayor a cero.");

        if (!FactoresAGramos.TryGetValue(unidadOrigen.Trim(), out var factor))
            throw new ValidacionException(
                $"La unidad '{unidadOrigen}' no tiene factor de conversión a gramos registrado.");

        var gramos = (int)Math.Round(cantidad * factor, MidpointRounding.AwayFromZero);
        return Math.Max(1, gramos);
    }

    public static int ConvertirCajaAUnidades(int cantidadCajas, int unidadesPorCaja)
    {
        if (cantidadCajas <= 0 || unidadesPorCaja <= 0)
            throw new ValidacionException("La cantidad de cajas y unidades por caja deben ser positivas.");

        return cantidadCajas * unidadesPorCaja;
    }

    public static int Bultos50KgAGramos(decimal bultos) => ConvertirAGramos(bultos, Bulto50Kg);

    public static int ArrobasAGramos(decimal arrobas) => ConvertirAGramos(arrobas, Arroba);

    public static int KilosAGramos(decimal kilos) => ConvertirAGramos(kilos, Kilo);

    public static int LibrasAGramos(decimal libras) => ConvertirAGramos(libras, Libra);

    public static int CajasAUnidades(int cajas, int unidadesPorCaja) => ConvertirCajaAUnidades(cajas, unidadesPorCaja);

    public static decimal CalcularCostoPorGramo(long costoTotalCompra, decimal cantidadComprada, string unidadOrigen)
    {
        var gramosTotales = ConvertirAGramos(cantidadComprada, unidadOrigen);
        if (gramosTotales <= 0) return 0m;
        return (decimal)costoTotalCompra / gramosTotales;
    }

    public static long CalcularCostoUnitarioDeCaja(long costoTotalCompra, int cantidadCajas, int unidadesPorCaja)
    {
        var unidadesTotales = ConvertirCajaAUnidades(cantidadCajas, unidadesPorCaja);
        if (unidadesTotales <= 0) return 0;
        return (long)Math.Round((decimal)costoTotalCompra / unidadesTotales, MidpointRounding.AwayFromZero);
    }
}
