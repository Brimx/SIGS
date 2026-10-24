# Matriz de permisos

Los permisos son banderas (`[Flags]`) dentro de `Rol`. `ISesionActual.Exigir(permiso)` los verifica al inicio de cada caso de uso. La matriz inicial es editable desde la aplicación; el código solo impone qué permiso exige cada operación.

## Roles × permisos

| Permiso | Admin | Gerente | Cajero | Bodeguero |
| :-- | :--: | :--: | :--: | :--: |
| `Vender` | ✅ | | ✅ | |
| `AnularVenta` | ✅ | ✅ | | |
| `AbrirCerrarTurno` | ✅ | | ✅ | |
| `VerInventario` | ✅ | ✅ | | ✅ |
| `AjustarInventario` | ✅ | | | ✅ |
| `Reabastecer` | ✅ | | | ✅ |
| `GestionarProductos` | ✅ | | | ✅ |
| `VerReportes` | ✅ | ✅ | | |
| `VerFinanzas` | ✅ | ✅ | | |
| `GestionarPersonal` | ✅ | ✅ | | |
| `GestionarRoles` | ✅ | | | |
| `GestionarTarifas` | ✅ | | | |
| `VerAuditoria` | ✅ | | | |

Notas:

- **Admin** recibe todos los permisos por composición de `Enum.GetValues<Permiso>()`.
- **Gerente** puede crear y editar empleados, pero **no** cambiar roles ni tarifas: eso queda en Admin.
- **Cajero** no ve inventario ni finanzas; solo vende y maneja su turno.
- **Bodeguero** gestiona productos e inventario, pero no vende ni ve finanzas.

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
