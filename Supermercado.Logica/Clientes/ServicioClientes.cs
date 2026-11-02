using Supermercado.Entidades.Clientes;
using Supermercado.Entidades.Contratos;
using Supermercado.Entidades.Excepciones;
using Supermercado.Entidades.Seguridad;
using Supermercado.Logica.Comunes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.Logica.Clientes;

public class ServicioClientes(
    IClienteRepositorio clientes,
    IServicioAuditoria auditoria,
    ISesionActual sesion,
    IUnidadDeTrabajo trabajo) : IServicioClientes
{
    public async Task<Cliente> CrearAsync(
        string documento,
        string nombres,
        string apellidos,
        string telefono,
        string correo,
        bool autorizaTratamientoDatos = true)
    {
        sesion.Exigir(Permiso.GestionarClientes);

        var cliente = Armar(new Cliente(), documento, nombres, apellidos,
            telefono, correo);
        cliente.AutorizaTratamientoDatos = autorizaTratamientoDatos;
        if (autorizaTratamientoDatos)
            cliente.FechaAutorizacion = DateTime.UtcNow;
        if (await clientes.ObtenerPorDocumentoAsync(cliente.Documento) is not null)
            throw new ValidacionException("Ya existe un cliente con ese documento.");

        await clientes.AgregarAsync(cliente);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ClienteCreado,
            nameof(Cliente),
            detalle: cliente.Documento);
        await trabajo.GuardarAsync();
        return cliente;
    }

    public async Task EditarAsync(
        int id,
        string documento,
        string nombres,
        string apellidos,
        string telefono,
        string correo,
        bool autorizaTratamientoDatos = true)
    {
        sesion.Exigir(Permiso.GestionarClientes);

        var cliente = await Buscar(id);
        var datos = Armar(new Cliente(), documento, nombres, apellidos,
            telefono, correo);
        cliente.AutorizaTratamientoDatos = autorizaTratamientoDatos;
        var otro = await clientes.ObtenerPorDocumentoAsync(datos.Documento);
        if (otro is not null && otro.Id != id)
            throw new ValidacionException("Ya existe un cliente con ese documento.");

        cliente.Documento = datos.Documento;
        cliente.Nombres = datos.Nombres;
        cliente.Apellidos = datos.Apellidos;
        cliente.Telefono = datos.Telefono;
        cliente.Correo = datos.Correo;
        clientes.Actualizar(cliente);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ClienteModificado,
            nameof(Cliente),
            id,
            cliente.Documento);
        await trabajo.GuardarAsync();
    }

    public async Task DesactivarAsync(int id)
    {
        sesion.Exigir(Permiso.GestionarClientes);

        var cliente = await Buscar(id);
        cliente.Activo = false;
        clientes.Actualizar(cliente);
        await auditoria.RegistrarAsync(
            AccionAuditoria.ClienteModificado,
            nameof(Cliente),
            id,
            "Desactivado");
        await trabajo.GuardarAsync();
    }

    public async Task<Cliente?> BuscarPorDocumentoAsync(string documento)
    {
        ExigirCajaOGestion();
        return await clientes.ObtenerPorDocumentoAsync(documento?.Trim() ?? "");
    }

    public async Task<IReadOnlyList<Cliente>> ObtenerTodosAsync()
    {
        sesion.Exigir(Permiso.GestionarClientes);
        return await clientes.ObtenerTodosAsync();
    }

    // Cajero busca al cliente en caja; no necesita gestionarlos.
    private void ExigirCajaOGestion()
    {
        if (sesion.Tiene(Permiso.Vender))
        {
            sesion.Exigir(Permiso.Vender);
            return;
        }

        sesion.Exigir(Permiso.GestionarClientes);
    }

    private async Task<Cliente> Buscar(int id)
        => await clientes.ObtenerPorIdAsync(id)
            ?? throw new ValidacionException("El cliente no existe.");

    private static Cliente Armar(
        Cliente cliente,
        string documento,
        string nombres,
        string apellidos,
        string telefono,
        string correo)
    {
        cliente.Documento = Validador.ExigirDocumento(documento);
        cliente.Nombres = Validador.ExigirNombre(nombres, "el nombre");
        cliente.Apellidos = Validador.ExigirNombreOpcional(
            apellidos, "el apellido");
        cliente.Telefono = Validador.ExigirTelefonoOpcional(telefono);
        cliente.Correo = Validador.ExigirCorreoOpcional(correo);
        return cliente;
    }
}
