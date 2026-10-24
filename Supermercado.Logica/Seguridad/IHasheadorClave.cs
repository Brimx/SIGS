namespace Supermercado.Logica.Seguridad;

public interface IHasheadorClave
{
    string Hashear(string secreto);
    bool Verificar(string secreto, string hash);
}
