# Matriz de permisos

Los permisos son banderas (`[Flags]`) dentro de `Rol`. `ISesionActual.Exigir(permiso)` los verifica al inicio de cada caso de uso. La matriz inicial es editable desde la aplicación; el código solo impone qué permiso exige cada operación.

## Roles × permisos

| Permiso | Admin | Gerente | Supervisor | Cajero | Bodeguero | Compras | Contador | Soporte |
| :-- | :--: | :--: | :--: | :--: | :--: | :--: | :--: | :--: |
| `Vender` | ✅ | | ✅ | ✅ | | | | |
| `AnularVenta` | ✅ | ✅ | ✅ | | | | | |
| `AbrirCerrarTurno` | ✅ | | ✅ | ✅ | | | | |
| `VerInventario` | ✅ | ✅ | ✅ | | ✅ | ✅ | | |
| `AjustarInventario` | ✅ | | | | ✅ | | | |
| `Reabastecer` | ✅ | | | | ✅ | ✅ | | |
| `GestionarProductos` | ✅ | | | | ✅ | ✅ | | |
| `VerReportes` | ✅ | ✅ | | | | | ✅ | |
| `VerFinanzas` | ✅ | ✅ | | | | | ✅ | |
| `GestionarPersonal` | ✅ | ✅ | | | | | | |
| `GestionarRoles` | ✅ | | | | | | | |
| `GestionarTarifas` | ✅ | | | | | | | |
| `VerAuditoria` | ✅ | | | | | | ✅ | ✅ |
| `GestionarClientes` | ✅ | ✅ | ✅ | ✅ | | | | |
| `AutorizarOperacionPos` | ✅ | ✅ | ✅ | | | | | |
| `GestionarCompras` | ✅ | | | | | ✅ | | |
| `GestionarContabilidad` | ✅ | | | | | | ✅ | |
| `MantenimientoSistema` | ✅ | | | | | | | ✅ |

Notas:

- **Admin** recibe todos los permisos por composición de `Enum.GetValues<Permiso>()`.
- **Gerente** supervisa la operación de la tienda, finanzas y personal, sin alterar configuración de roles o impuestos.
- **Supervisor** apoya la línea de cajas, autoriza cancelaciones/anulaciones y verifica clientes e inventario.
- **Cajero** vende, redime fidelización y realiza apertura/arqueo ciego de su turno.
- **Bodeguero** gestiona existencias, lotes, mermas y reabastecimiento estante ↔ bodega.
- **Compras** gestiona relación con proveedores, órdenes de compra y reabastecimiento.
- **Contador** audita números, reportes fiscales DIAN, arqueos y comprobantes de nómina.
- **Soporte** realiza mantenimiento, respaldos y revisión técnica de logs de auditoría.

> **Hallazgo abierto (P0):** un Gerente con `GestionarPersonal` puede crear un empleado con rol Admin, crearle usuario o restablecer la clave del Admin. La autorización debe impedir asignar un rol con más permisos que el del actor y restablecer claves de rango superior. Ver [`pendientes-ui.md`](pendientes-ui.md).

## Permiso exigido por operación

| Servicio | Método | Permiso | Excepción si falta |
| :-- | :-- | :-- | :-- |
| `ServicioAutenticacion` | `IniciarSesionAsync` / `IniciarSesionConPinAsync` | Ninguno (público) | `CredencialesInvalidas`, `CuentaBloqueada` |
| `ServicioAutenticacion` | `CambiarClaveAsync` / `AsignarPinAsync` | Sesión iniciada | `SesionNoIniciada` |
| `ServicioParametros` | `GuardarAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioParametros` | `GuardarRespaldoAsync` | Interno (sin sesión) | — |
| `ServicioEmpleados` | `CrearAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioEmpleados` | `EditarAsync` | `GestionarPersonal`; si cambia el rol, además `GestionarRoles` | `AccesoDenegado` |
| `ServicioEmpleados` | `CambiarRolAsync` | `GestionarRoles` | `AccesoDenegado` |
| `ServicioEmpleados` | `DesactivarAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioEmpleados` | `CrearUsuarioAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioProductos` | `RegistrarAsync` | `GestionarProductos` | `AccesoDenegado` |
| `ServicioProductos` | `RegistrarIngresoLoteAsync` | `GestionarProductos` | `AccesoDenegado` |
| `ServicioProductos` | `BuscarPorCodigoBarrasAsync` / `ConsultarExternoAsync` | Ninguno | — |
| `ServicioMantenimientoProductos` | `EditarAsync` / `CambiarPrecioAsync` / `ActivarAsync` / `DesactivarAsync` | `GestionarProductos` | `AccesoDenegado` |
| `ServicioCatalogos` | `CrearCategoriaAsync` / `EditarCategoriaAsync` / `CrearProveedorAsync` / `EditarProveedorAsync` | `GestionarProductos` | `AccesoDenegado` |
| `ServicioBusquedaProductos` | `BuscarPorNombreAsync` | Ninguno | — |
| `ServicioConsultaInventario` | `ObtenerStockAsync` / `ObtenerKardexAsync` | `VerInventario` | `AccesoDenegado` |
| `ServicioInventario` | `DescontarVentaAsync` / `ReintegrarVentaAsync` | Invocado por ventas; no exige permiso propio | — |
| `ServicioInventario` | `TrasladarAEstanteAsync` / `RegistrarIngresoAsync` | Invocado por reabastecimiento/productos | — |
| `ServicioMermas` | `RegistrarAsync` | `AjustarInventario` | `AccesoDenegado` |
| `ServicioAlertasInventario` | `ObtenerBajoMinimoAsync` / `ObtenerLotesPorVencerAsync` | `VerInventario` | `AccesoDenegado` |
| `ServicioReabastecimiento` | `GenerarSolicitudesAsync` / `ObtenerPendientesAsync` / `AtenderAsync` | `Reabastecer` | `AccesoDenegado` |
| `ServicioVentas` | `RegistrarVentaAsync` | `Vender` | `AccesoDenegado` |
| `ServicioVentas` | `AnularVentaAsync` | `AnularVenta` | `AccesoDenegado` |
| `ServicioCaja` | `AbrirTurnoAsync` / `CerrarTurnoAsync` | `AbrirCerrarTurno` | `AccesoDenegado` |
| `ServicioCaja` | `ObtenerTurnoActualAsync` | Sesión iniciada | `SesionNoIniciada` |
| `ServicioFinanzas` | `RegistrarGastoAsync` / `ObtenerResumenAsync` | `VerFinanzas` | `AccesoDenegado` |
| `ServicioImpuestos` | `ObtenerActivosAsync` | Ninguno | — |
| `ServicioImpuestos` | `CambiarTarifaAsync` | `GestionarTarifas` | `AccesoDenegado` |
| `ServicioNomina` | `CalcularAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioUsuarios` | `ObtenerTodosAsync` / `DesbloquearAsync` | `GestionarPersonal` | `AccesoDenegado` |
| `ServicioUsuarios` | `RestablecerClaveTemporalAsync` | `GestionarRoles` | `AccesoDenegado` |
| `ServicioEstructura` | `CrearCargoAsync` / `EditarCargoAsync` / `CrearRolAsync` / `EditarRolAsync` | `GestionarPersonal`; roles además `GestionarRoles` | `AccesoDenegado` |
| `ServicioConsultaAuditoria` | `ObtenerAsync` | `VerAuditoria` | `AccesoDenegado` |
| `ServicioRespaldo` | `CrearRespaldoAsync` | `GestionarRoles` | `AccesoDenegado` |
| `ServicioReportesQuestPdf` | `GenerarDesprendibleNominaAsync` / `GenerarReporteVentasAsync` / `GenerarReporteInventarioAsync` / `GenerarReporteArqueoAsync` | `GestionarPersonal` / `VerReportes` / `VerInventario` / `AbrirCerrarTurno` | `AccesoDenegado` |
| `ServicioDian` | `EmitirDocumentoPosAsync` / `ConsultarEstadoResolucionAsync` | Invocado por ventas; no exige permiso propio | — |
| `ServicioDashboard` | `ObtenerDisponibles` / `ObtenerDisposicionAsync` / `AgregarAsync` / `QuitarAsync` / `ReordenarAsync` | Filtra por `PermisoRequerido` de cada widget | — |
| `VentasDelDiaWidget` | `ObtenerDatosAsync` | `VerReportes` | No se muestra |
| `ReabastecimientosPendientesWidget` | `ObtenerDatosAsync` | `VerInventario` | No se muestra |
| `TrmDelDiaWidget` | `ObtenerDatosAsync` | `Ninguno` | — |

## Verificación en sesión

`SesionActual.Exigir` aplica este orden:

1. Si el empleado no está activo, cierra la sesión y lanza `AccesoDenegadoException`.
2. Si `DebeCambiarClave` está activo, lanza `DebeCambiarClaveException`.
3. Si no tiene el permiso, lanza `AccesoDenegadoException`.

`SesionActual.EmpleadoId` lanza `SesionNoIniciadaException` si no hay usuario.

## Agregar un permiso

1. Añada el valor a `Permiso` con la siguiente potencia de dos.
2. Asígnelo en `ValoresIniciales` al rol que corresponda.
3. Llame `sesion.Exigir(Permiso.Nuevo)` al inicio del caso de uso.
4. Actualice esta matriz y `docs/diccionario-datos.md`.

### Clientes y puntos

| Servicio | Método | Permiso | Excepción si falta |
| :-- | :-- | :-- | :-- |
| `ServicioClientes` | `CrearAsync` / `EditarAsync` / `DesactivarAsync` / `ObtenerTodosAsync` | `GestionarClientes` | `AccesoDenegado` |
| `ServicioClientes` | `BuscarPorDocumentoAsync` | `Vender` o `GestionarClientes` | `AccesoDenegado` |
| `ServicioVentas` | `RegistrarVentaAsync` con `ClienteId` / `PuntosARedimir` | `Vender` | `AccesoDenegado` |

La semilla solo inserta roles faltantes: una base creada antes de `GestionarClientes` debe añadirlo a Gerente y Cajero desde la gestión de roles.
