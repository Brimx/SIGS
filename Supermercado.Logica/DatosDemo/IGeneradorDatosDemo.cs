namespace Supermercado.Logica.DatosDemo;

public interface IGeneradorDatosDemo
{
    Task GenerarAsync(int semilla = 42);
}
