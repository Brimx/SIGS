namespace Supermercado.Logica.Seguridad;

public interface IHasheadorClave
{
    string Hashear(string secreto);
    bool Verificar(string secreto, string hash);
}

public class HasheadorBCrypt : IHasheadorClave
{
    public string Hashear(string secreto)
        => BCrypt.Net.BCrypt.HashPassword(secreto);

    public bool Verificar(string secreto, string hash)
        => BCrypt.Net.BCrypt.Verify(secreto, hash);
}
