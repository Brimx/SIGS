using Supermercado.Entidades.Excepciones;
using Supermercado.Logica.Comunes;
using Xunit;

namespace Supermercado.Pruebas;

public class ValidadorTests
{
    [Theory]
    [InlineData("Ana", true)]
    [InlineData("María José", true)]
    [InlineData("O'Neil", true)]
    [InlineData("Pérez-Ñúñez", true)]
    [InlineData("Ana3", false)]
    [InlineData("123", false)]
    [InlineData("Ana@", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void Nombre_RechazaNumerosYSimbolos(string? valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsNombreValido(valor));

    [Theory]
    [InlineData("12345", true)]
    [InlineData("1020304050", true)]
    [InlineData("1234", false)]
    [InlineData("12A45", false)]
    [InlineData("12.345.678", false)]
    [InlineData("", false)]
    public void Documento_SoloDigitos(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsDocumentoValido(valor));

    [Theory]
    [InlineData("900123456", true)]
    [InlineData("900123456-1", true)]
    [InlineData("900123456-12", false)]
    [InlineData("123", false)]
    [InlineData("ABC123456", false)]
    public void Nit_AdmiteDigitoDeVerificacion(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsNitValido(valor));

    [Theory]
    [InlineData("", true)]
    [InlineData("3001234567", true)]
    [InlineData("300 123 4567", true)]
    [InlineData("+573001234567", true)]
    [InlineData("12345", false)]
    [InlineData("abc1234567", false)]
    public void Telefono_EsOpcionalPeroConFormato(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsTelefonoValido(valor));

    [Theory]
    [InlineData("", true)]
    [InlineData("a@b.com", true)]
    [InlineData("sin-arroba", false)]
    [InlineData("a@b", false)]
    [InlineData("Ana <a@b.com>", false)]
    public void Correo_EsOpcionalPeroConFormato(string valor, bool esperado)
        => Assert.Equal(esperado, Validador.EsCorreoValido(valor));

    [Fact]
    public void ExigirNombre_ConNumeros_LanzaValidacion()
    {
        var ex = Assert.Throws<ValidacionException>(
            () => Validador.ExigirNombre("Ana3", "el nombre"));

        Assert.Contains("sin números", ex.Message);
    }

    [Fact]
    public void ExigirNombre_RecortaEspacios()
        => Assert.Equal("Ana", Validador.ExigirNombre("  Ana ", "el nombre"));
}
