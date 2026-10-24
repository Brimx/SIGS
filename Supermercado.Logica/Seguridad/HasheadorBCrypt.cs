namespace Supermercado.Logica.Seguridad;

public class HasheadorBCrypt : IHasheadorClave
{
    public string Hashear(string secreto)
        => BCrypt.Net.BCrypt.HashPassword(secreto);

    public bool Verificar(string secreto, string hash)
        => BCrypt.Net.BCrypt.Verify(secreto, hash);
}
