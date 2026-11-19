using Supermercado.Entidades.Personal;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Pruebas;

public sealed class SesionContextoTelegramTests
{
    [Fact]
    public async Task UsuarioContextual_NoAlteraLaSesionGlobalYSeRestauraTrasAwait()
    {
        var global = CrearUsuario(1);
        var vinculado = CrearUsuario(2);
        var sesion = new SesionActual();
        sesion.Iniciar(global);

        using (sesion.UsarUsuario(vinculado))
        {
            Assert.Equal(2, sesion.EmpleadoId);
            await Task.Yield();
            Assert.Equal(2, sesion.EmpleadoId);
        }

        Assert.Equal(1, sesion.EmpleadoId);
    }

    private static Usuario CrearUsuario(int id)
        => new()
        {
            EmpleadoId = id,
            Empleado = new Empleado
            {
                Id = id,
                Activo = true,
                Rol = new Rol { Nombre = "Gerente", Permisos = Permiso.VerAuditoria }
            }
        };
}
