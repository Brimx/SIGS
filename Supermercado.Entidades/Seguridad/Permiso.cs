namespace Supermercado.Entidades.Seguridad;

[Flags]
public enum Permiso : long
{
    Ninguno = 0,
    Vender = 1 << 0,
    AnularVenta = 1 << 1,
    AbrirCerrarTurno = 1 << 2,
    VerInventario = 1 << 3,
    AjustarInventario = 1 << 4,
    Reabastecer = 1 << 5,
    GestionarProductos = 1 << 6,
    VerReportes = 1 << 7,
    VerFinanzas = 1 << 8,
    GestionarPersonal = 1 << 9,
    GestionarRoles = 1 << 10,
    GestionarTarifas = 1 << 11,
    VerAuditoria = 1 << 12,
}
