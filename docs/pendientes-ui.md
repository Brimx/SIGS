# Pendientes de interfaz (handoff backend → UI)

Auditoría backend→GUI del **2026-11-02**, revisada el **2026-11-22** tras la simplificación de datos y de servicios. La GUI ya tiene POS, turnos, inventario, dashboard, personal, compras, contabilidad y login; esta nota conserva los riesgos vigentes y marca los cerrados. La matriz cubre cuatro roles (`Admin`, `Gerente`, `Cajero`, `Bodeguero`; `Supermercado.Datos/Semillas.cs:18-43`); «Supervisor» es autorización POS (`ValidarSupervisorAsync`, `Supermercado.Logica/Seguridad.cs:61-106`), no un rol. La suite está en verde (443 pruebas; el conteo vigente y su fecha están en [`plan-pruebas.md`](plan-pruebas.md)). Esta nota no certifica preparación para producción.

> **Cambios del refactor que afectan este documento:** los servicios se consolidaron por módulo (ver tabla siguiente). PostgreSQL dual, Supabase, el Outbox y el programa de puntos (`PagoPuntos`) quedan fuera del alcance de `main`: son objetivo futuro conservado en rama `completo` (`2c38285`). La GUI pasó a filtrar el menú por rol.

| Antes | Hoy |
| :-- | :-- |
| `ServicioVentas`, `PosViewModel` | `Supermercado.Logica/Ventas.cs`, `Supermercado.GUI/ViewModels/PosViewModel*.cs` |
| `ServicioEmpleados`, `ServicioUsuarios`, `ServicioEstructura` | `Supermercado.Logica/Personal.cs` |
| `ServicioAutenticacion`, `SesionActual`, `ServicioParametros` | `Supermercado.Logica/Seguridad.cs` |
| `ServicioProductos`, `ServicioBusquedaProductos`, `ServicioMantenimientoProductos`, `ServicioCatalogos`, `ServicioImpuestos` | `Supermercado.Logica/Productos.cs` |
| `ServicioInventario`, `ServicioConsultaInventario` | `Supermercado.Logica/Inventario.cs` |
| `ServicioFinanzas`, `ServicioCaja`, `ServicioClientes` | `Supermercado.Logica/Finanzas.cs`, `Supermercado.Logica/Caja.cs`, `Supermercado.Logica/Clientes.cs` |
| `ServicioDashboard`, `MargenPorCategoriaWidget` | `Supermercado.Logica/Dashboard.cs`, `Supermercado.Logica/DashboardWidgets.cs` |
| `ServicioRespaldo`, `GeneradorTicketQuestPdf`, `Validador` | `Supermercado.Logica/Infraestructura.cs`, `Supermercado.Logica/Reportes.cs`, `Supermercado.Logica/Comun.cs` |
| `ConfiguracionModelo`, `RutasDatos`, `ExtensionesDatos` | `Supermercado.Datos/Contexto.cs` |
| `ValoresIniciales`, `InicializadorBaseDatos` | `Supermercado.Datos/Semillas.cs` |
| `PagoPuntos`, `ValidadorDian`, esquema de Supabase, `ImpresionTicket` | Fuera del alcance de `main`: objetivo futuro conservado en rama `completo`; `IImpresorTicket` sí existe en la GUI |

## 1. Validación de formularios

El backend rechaza datos inválidos con `ValidacionException` (`Supermercado.Entidades/Excepciones.cs:31`, mensaje listo para mostrar). Para avisar mientras se escribe, la UI reutiliza **las mismas reglas** de `Supermercado.Logica/Comun.cs` (`Validador.Es*` devuelven `bool` y `Exigir*` lanzan y normalizan; líneas 26-43).

| Campo | Regla | Método | Formularios |
| :-- | :-- | :-- | :-- |
| Nombres / apellidos | Solo letras, espacios, `'`, `.`, `-`; sin números; máx. 80 | `EsNombreValido` | Empleado, Cliente |
| Documento | Solo dígitos, 5–15 | `EsDocumentoValido` | Empleado, Cliente |
| NIT | 6–12 dígitos, `-DV` opcional | `EsNitValido` | Proveedor |
| Teléfono | Opcional; 7–10 dígitos (admite `+57`, espacios y guiones al escribir) | `EsTelefonoValido` | Empleado\*, Cliente, Proveedor |
| Correo | Opcional; formato `usuario@dominio.tld` | `EsCorreoValido` | Empleado, Cliente, Proveedor |

\* `Empleado` no tiene teléfono en el modelo actual.

Pendiente en GUI: restringir la entrada (no aceptar dígitos en nombres), mostrar el error junto al campo, deshabilitar «Guardar» mientras sea inválido y mostrar `ValidacionException.Message` vía `TraductorExcepciones.Traducir` (`Comun.cs:173-193`) cuando el backend rechace. La validación de la UI es **solo defensa en profundidad**: el backend siempre vuelve a validar.

Pendiente en backend: validar `Producto` (nombre/código de barras con formato) si se decide una regla de código (EAN-8/13/UPC); hoy `ServicioProductos.CrearProductoAsync` (`Supermercado.Logica/Productos.cs:51`) solo exige no vacío y precio positivo.

## 2. Navegación por teclado y atajos

El backend no impone atajos; solo garantiza que cada acción sea **una llamada atómica** (sin estados intermedios). A cargo de la GUI:

- Orden de tabulación lógico en todos los formularios; `Enter` avanza/confirma, `Esc` cancela.
- Caja: campo de código siempre enfocado; el lector USB (keyboard wedge) envía dígitos + `Enter` → `ServicioProductos.BuscarPorCodigoBarrasAsync` (`Supermercado.Logica/Productos.cs:117`).
- Atajos propuestos: cobrar, buscar producto, asociar cliente, anular línea, cancelar venta. Definirlos en un único lugar y mostrarlos en la UI.
- **Doble disparo:** deshabilitar el comando mientras `RegistrarVentaAsync` está en curso. `SolicitudVenta.ClaveIdempotencia` (`Supermercado.Logica/Ventas.cs:311-317`) se apoya en un **índice único** sobre `Venta.ClaveIdempotencia` (`Contexto.cs:115`): una segunda inserción con la misma clave falla en la base en vez de duplicar la venta. La UI debe generar un **GUID por intento de cobro**, reutilizarlo solo al reintentar la misma operación y mantener el botón deshabilitado. Lo que el índice no cubre es el stock: sin transacción, dos cobros concurrentes pueden leer el mismo saldo y sobrevender (ver sección 6).
- **Una operación, un scope de DI:** no compartir `SigsDbContext` ni servicios `Scoped` entre tareas. La GUI debe resolver los servicios dentro de un `IServiceScope` por operación (ver sección 7).

## 3. Clientes y habeas data (caja)

Backend: `ServicioClientes` (`Supermercado.Logica/Clientes.cs:11`) con `CrearAsync` (`:16`), `EditarAsync` (`:43`), `DesactivarAsync` (`:76`), `BuscarPorDocumentoAsync` (`:91`) y `ObtenerTodosAsync` (`:97`). `SolicitudVenta` quedó en `Lineas`, `Pagos`, `ClienteId`, `ClaveIdempotencia` y `CajaId` (`Supermercado.Logica/Ventas.cs:311-317`).

- **Ya implementado en POS:** búsqueda de cliente por documento (`PosView.axaml:82-102`, `Enter`) y modal de registro con habeas data (`PosView.axaml:754`, `F5`). El modelo exige autorización explícita (`Cliente.AutorizarHabeasData`, `Supermercado.Entidades/Clientes.cs:16-25`) y, si no la hay, la venta queda como Consumidor Final (`Supermercado.Logica/Ventas.cs:73-77`).
- **Pendiente:** pantalla de gestión (editar, desactivar y listar) para el permiso `GestionarClientes`. Hoy solo existe el alta desde caja.
- **Pagos:** la UI solo debe construir `PagoEfectivo`, `PagoTarjeta` y `PagoTransferencia` (`Supermercado.Entidades/Ventas.cs:154`, `:159`, `:176`). **No hay `PagoPuntos` ni `PuntosARedimir` en `main`**: el programa de puntos es objetivo futuro fuera del alcance de `main`, conservado en rama `completo`. ADR-018 queda como decisión histórica.
- El ticket usa `NombreAdquirente` y `DocumentoAdquirente` de la venta (`Supermercado.Entidades/Ventas.cs:22-23`); no imprime puntos porque no están en `main`.

## 4. APIs opcionales y Gestión de Credenciales en UI (Bring Your Own Keys)

Backend: ADR-019. Las integraciones de pago o con planes escalables son **soportadas por el presupuesto del supermercado**. El software incluye los conectores, pero el supermercado ingresa sus propias claves desde la pantalla de configuración y asume el costo de suscripción si supera los planes gratuitos.

> Supabase queda fuera del alcance de `main` como objetivo futuro conservado en rama `completo` (`2c38285`); no hay proveedor ni esquema activo. Quedan TRM, códigos de barras y Resend, que sí existen en el código.

### Pantalla en GUI: Configuración → «Integraciones y Conectividad» (pendiente)

Todos los valores se guardan en la tabla `Parametros` mediante `ServicioParametros.GuardarAsync(clave, valor)` (`Supermercado.Logica/Seguridad.cs:370`, hoy exige permiso `GestionarPersonal`; ver hallazgo de permisos en la sección 6).

| Servicio | Parámetros a enlazar | Campos visuales | Acciones / Enlaces externos (Abrir navegador) |
| :-- | :-- | :-- | :-- |
| **TRM (Datos Abiertos Colombia)** | `integraciones.trm_habilitada` (`1`/`0`)<br>`integraciones.trm_token` | Switch activar/desactivar<br>TextBox para App Token | Enlace «Obtener App Token gratuito»:<br>`https://www.datos.gov.co/profile/edit/developer_settings` |
| **Códigos de barras (Open Food Facts)** | `integraciones.codigo_barras_habilitado` (`1`/`0`) | Switch activar/desactivar | Enlace «Base de datos abierta ODbL»:<br>`https://world.openfoodfacts.org` |
| **Envío de recibos por correo (Resend)** | `integraciones.resend_token`<br>`integraciones.resend_remitente` | PasswordBox para API Key<br>TextBox para correo remitente | • Enlace «Obtener API Key en Resend»:<br>`https://resend.com/api-keys`<br>• Enlace «Ver Planes y Precios (Pro $20/mes)»:<br>`https://resend.com/pricing` |

Las claves viven en `ClavesParametro` (`Supermercado.Logica/Seguridad.cs:120-147`). La pantalla de Configuración actual solo cubre apariencia y textos legales; no hay controles de integración.

### Reglas para la UI al manejar credenciales y enlaces

1. **Seguridad visual:** los campos de API Key / Token deben ser tipo contraseña (`PasswordChar = '•'`) con icono de ojo para alternar visibilidad.
2. **No volver a mostrar el secreto:** al abrir la pantalla, mostrar un placeholder («•••• configurado») y no leer el valor completo desde `Parametros` para pintarlo. `ServicioParametros.ObtenerTextoAsync` no exige permiso y puede devolver secretos (`Supermercado.Logica/Seguridad.cs:349-353`).
3. **Acción de apertura en navegador:** en Avalonia, invocar el launcher del sistema operativo para abrir las URLs de registro/pago sin bloquear la app:
   ```csharp
   TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(new Uri(url));
   ```
4. **Resiliencia en caja:** si alguna API no está configurada (campo vacío) o no hay internet, la caja no muestra diálogos modales ni bloquea el cobro; la venta continúa normalmente en modo local. Los clientes externos arrancan apagados (`ClienteTrmNoConfigurado`, `ConsultaCodigoBarrasNula` y `VerificadorCorreoNulo` en `ClientesExternos.cs`).
5. **No llamar `GuardarRespaldoAsync` desde la UI:** ese método de `ServicioParametros` no exige permiso ni deja auditoría (`Supermercado.Logica/Seguridad.cs:383-393`); es un mecanismo interno de las integraciones.

## 5. Otros pendientes que tocan UI

- Impresión física: hoy hay `IGeneradorTicket` (`Reportes.cs:287`) e `IImpresorTicket` (`Supermercado.GUI/ServiciosGui.cs:165`). Si se implementa ESC/POS, la UI necesitará elegir impresora y probar impresión.
- Dashboard: no hay widget de clientes; añadirlo si se desea.
- Respaldos: avisar que el archivo `.db` contiene hashes, tokens de integración y datos personales; no guardarlo en carpetas sincronizadas a la nube sin cifrado. El respaldo actual usa `File.Copy` sobre SQLite vivo (`Infraestructura.cs:53`), lo que puede producir copias inconsistentes.
- Datos demo: `GeneradorDatosDemo` está registrado siempre (`Composicion.cs:60`) y crea usuarios con claves conocidas (`DatosDemo.cs:296-320`). La UI no debe exponer «generar datos demo» en producción; el backend debe condicionarlo al entorno.
- Migraciones: `Inicial` genera el esquema desde el modelo actual en tiempo de ejecución (`Supermercado.Datos/Migraciones/Inicial.cs:9`). Una base existente no recibe columnas nuevas; antes de actualizar, respaldar y prever migración/recreación.

## 6. Auditoría backend 2026-11-02: hallazgos que condicionan la UI

Prioridad: **P0** bloquea producción, **P1** corrige antes de operar con varias terminales o datos reales, **P2** deuda que la UI debe mitigar. El estado actualizado por hallazgo está en la sección 8; aquí se conserva la evidencia.

| Pri. | Hallazgo | Evidencia | Acción UI | Acción backend |
| :-- | :-- | :-- | :-- | :-- |
| **P0** | Escalada de privilegios: un Gerente puede crear un empleado Admin, crearle usuario o restablecer la clave del Admin | `ServicioPersonal.CrearAsync` (`Supermercado.Logica/Personal.cs:36-56`), `CrearUsuarioAsync` (`:158-186`), `ExigirAlcanceDelRolAsync`; `ServicioAutenticacion` (`Supermercado.Logica/Seguridad.cs:13`) | Ocultar asignación de rol, creación de usuario y restablecimiento de clave para no-admin; confirmar identidad | Exigir `GestionarRoles` para asignar/cambiar roles y para crear usuarios o restablecer claves de cuentas con rol superior; prohibir que un no-admin otorgue Admin |
| **P1** | La sesión cachea rol/estado y no se revoca al cambiarlos en otro contexto | `SesionActual.Tiene`/`Exigir` (`Supermercado.Logica/Seguridad.cs:417-440`) | Tras cambiar rol/desactivar empleado, forzar re-login; no confiar en permisos cacheados | Revalidar permisos contra la BD o versionar/invalidar la sesión |
| **P1** | Secretos y PII en SQLite sin cifrar; respaldos y logs sin permisos restrictivos | `ServicioParametros.ObtenerTextoAsync` (`Supermercado.Logica/Seguridad.cs:349`), `GuardarAsync` (`:370`); `ServicioRespaldo` (`Infraestructura.cs:21`); `RutasDatos` (`Contexto.cs:153`) | PasswordBox, no loggear ni mostrar secretos, avisar del contenido de respaldos | Cifrar/OS keyring, permisos `0600`, redactar logs, separar configuración pública de secretos |
| **P1** | Respaldo en caliente con `File.Copy` y `rutaOrigen` arbitraria | `IServicioRespaldo`/`ServicioRespaldo` (`Infraestructura.cs:16-56`), `File.Copy` en `:53` | No exponer ruta libre; avisar que el respaldo puede ser inconsistente | Usar `VACUUM INTO` o `SqliteConnection.BackupDatabase`; validar ruta; verificar restauración |
| **P1** | Concurrencia multi-terminal sin tokens ni bloqueos; SQLite sin WAL/busy_timeout | `ExtensionesDatos` (`Contexto.cs:182-233`); `ServicioCaja` (`Supermercado.Logica/Caja.cs:12`); ADR-010 (`docs/decisiones.md`) | Serializar operaciones, deshabilitar botones, reintentar ante «database is locked»; no prometer multi-terminal | Tokens de concurrencia, índice único parcial de turno abierto y WAL/busy_timeout; PostgreSQL para varias cajas es objetivo futuro fuera del alcance de `main`, conservado en rama `completo` |
| **P1** | Inventario: merma no valida producto/lote; traslado permite vencidos; ingreso duplicado; kardex sin saldo inicial | `ServicioInventario` (`Supermercado.Logica/Inventario.cs:63-160`), `ServicioProductos.RegistrarIngresoLoteAsync` (`Supermercado.Logica/Productos.cs:142`) | Validar producto/lote, advertir vencidos, impedir lote duplicado, mostrar saldo inicial correcto | Corregir validaciones, bloquear traslado de vencidos, traducir duplicados, calcular saldo previo |
| **P1** | Productos pesados: precio/costo por kg y cantidades en gramos sin conversión en reportes | `ProductoPesado` (`Supermercado.Entidades/Productos.cs:49`); `DatosDemo.cs:120`; `MargenPorCategoriaWidget` (`DashboardWidgets.cs:280`) | Etiquetar «gramos» y «precio por kg»; convertir a kg al mostrar; no reutilizar el margen actual | Convertir costo `CostoUnitario × gramos / 1000`; evitar doble conteo de ingreso cuando una línea sale de varios lotes |
| **P2** | Permisos de lectura usados para escritura y permisos demasiado amplios | `ServicioFinanzas.RegistrarGastoAsync` (`Supermercado.Logica/Finanzas.cs:32`, con `VerFinanzas`); `ServicioProductos.CambiarPrecioAsync` (`Supermercado.Logica/Productos.cs:271`, con `GestionarProductos`); `ServicioParametros.GuardarAsync` (`Supermercado.Logica/Seguridad.cs:370`, con `GestionarPersonal`) | Ocultar acciones sensibles según rol; no asumir que «Ver» es solo lectura | Separar permisos de escritura (`RegistrarGastos`, `CambiarPrecios`, `GestionarIntegraciones`) |
| **P2** | Lecturas sin `Exigir`: productos, categorías, proveedores e impuestos | `ServicioProductos.ObtenerProductosAsync` (`Supermercado.Logica/Productos.cs:101`), `ObtenerImpuestosAsync` (`:112`), `BuscarPorCodigoBarrasAsync` (`:117`), `BuscarPorNombreAsync` (`:213`), `ObtenerCategoriasAsync` (`:338`), `ObtenerProveedoresAsync` (`:395`); `ServicioPersonal.ObtenerEmpleadosAsync` (`Supermercado.Logica/Personal.cs:31`) | No confiar en el servicio para autorización; aplicar filtro de rol en la pantalla | Añadir `Exigir` coherente a cada lectura |
| **P2** | APIs con entidades completas permiten overposting (`Id`, `Activo`, `RolId`, `Pago`) | `SolicitudVenta` (`Supermercado.Logica/Ventas.cs:311`); `ServicioPersonal.CrearAsync` (`Supermercado.Logica/Personal.cs:36`); `ServicioProductos.RegistrarAsync` (`Supermercado.Logica/Productos.cs:30`) | Bindear solo campos permitidos; no reutilizar entidades entre formularios | Usar comandos/DTOs y construir entidades en el backend |
| **P2** | Aritmética de dinero sin `checked` ni topes; posible overflow con precios/cantidades extremas | `Dinero` (`Supermercado.Entidades/Base.cs:10-18`); `ProductoPesado.CalcularSubtotal` (`Supermercado.Logica/Productos.cs:55`); `Iva` (`Supermercado.Entidades/Finanzas.cs:89`) | Limitar cantidad/precio en la UI y mostrar error | Aritmética comprobada y límites de dominio |
| **P2** | Autenticación: enumeración por timing; clave solo longitud 8; PIN 4 dígitos sin complejidad | `ServicioAutenticacion` (`Supermercado.Logica/Seguridad.cs:13-180`), `LongitudMinimaClave`/`LongitudMinimaPin` | Mensajes genéricos, no revelar si el usuario existe; exigir clave fuerte | Hash dummy para usuario inexistente; política de complejidad; límites de PIN |
| **P2** | Datos demo con credenciales conocidas siempre registrados | `Composicion.cs:60`; `DatosDemo.cs:296-320` | No exponer en producción | Registrar solo en Development y exigir confirmación |
| **P2** | Migración `Inicial` no determinista; bases existentes no reciben columnas nuevas | `Migraciones/Inicial.cs:9`; `InicializadorBaseDatos` (`Semillas.cs:120`) | Avisar y respaldar antes de actualizar | Migraciones incrementales reales con snapshot |
| **P3** | Documentación desactualizada | Historial de esta nota y `docs/arquitectura.md` | Usar esta nota como fuente | Actualizar docs al cerrar cada hallazgo |

### Reglas de oro para la UI

1. **La UI nunca es la autoridad.** `SesionActual.Tiene` solo sirve para mostrar/ocultar; la autorización real es `Exigir` en el servicio.
2. **No construir entidades de pago.** La UI arma comandos de pago simples; el backend valida los montos.
3. **Un scope por operación.** No mantener un `DbContext` vivo entre pantallas ni compartirlo entre tareas.
4. **Botón único.** Toda operación que escribe se deshabilita mientras está en curso y se rehabilita al terminar; usar `CancellationToken` cuando aplique.
5. **No mostrar secretos.** PasswordBox, placeholder, sin logs, sin portapapeles, sin capturas.
6. **No prometer multi-terminal.** Mientras ADR-010 siga abierto, una sola terminal; si hay varias, esperar errores de bloqueo y reintentar.
7. **Unidades explícitas.** Gramos para pesados; pesos enteros para dinero.
8. **Errores traducidos.** Mostrar `TraductorExcepciones.Traducir(ex)`; nunca el stack trace. Los `DbUpdateException` de duplicados hoy caen en el mensaje genérico: la UI debe decir «dato duplicado» solo si el backend lo traduce.

## 7. Contrato de integración para la GUI

- **Composición:** llamar `services.AgregarLogica()` una vez en el arranque (`Composicion.cs:21`). La GUI no referencia `Supermercado.Datos`.
- **Ciclo de vida:** `SesionActual` es singleton; los servicios y repositorios son `Scoped`; `SigsDbContext` es `Scoped`. Resolver cada operación con `IServiceScopeFactory.CreateScope()` y disponer el scope al terminar.
- **Ventas:** generar `ClaveIdempotencia = Guid.NewGuid().ToString("N")` al abrir el cobro; conservarla si se reintenta la misma venta; descartarla si el cajero cancela y empieza otra. Enviar `Lineas`, `Pagos`, `ClienteId` y `CajaId`. Validar en la UI que `TotalPagado >= Total` y que el cambio solo se da en efectivo.
- **Pagos:** `PagoEfectivo { Monto }`, `PagoTarjeta { Monto, UltimosDigitos }` (4 dígitos), `PagoTransferencia { Monto, Referencia }`. Crear objetos nuevos por venta; no reutilizar instancias.
- **Clientes:** registrar el consentimiento de habeas data antes de asociar el documento a la venta; sin consentimiento, vender como Consumidor Final.
- **Inventario:** para `ProductoPesado`, `Cantidad` va en gramos y `PrecioVenta`/`CostoUnitario` por kg; `StockMinimoEstante` en gramos. Mostrar kg y precio/kg; validar que la merma use el mismo producto y lote.
- **Permisos:** cargar la matriz desde `Rol.Permisos` solo para visibilidad; tras un cambio de rol, cerrar sesión y volver a autenticar.
- **Integraciones:** guardar claves con `GuardarAsync`; no leerlas de vuelta para mostrarlas; probar conexión con un botón explícito; si falla una API, la caja sigue en modo local sin diálogos modales.
- **Respaldos:** no expone `rutaOrigen`; usar la carpeta configurada; avisar que el archivo contiene PII/secretos; verificar restauración antes de confiar.
- **Errores:** `ValidacionException`, `VentaInvalidaException`, `StockInsuficienteException`, `TurnoInvalidoException`, `AccesoDenegadoException`, `DebeCambiarClaveException` y las demás del catálogo (`Supermercado.Entidades/Excepciones.cs`) tienen mensaje mostrable; el resto se registra y se muestra un mensaje genérico.
- **Pruebas de aceptación:** doble clic en cobrar no duplica venta; usuario sin permiso no ve la acción ni puede invocarla; cambio de rol exige re-login; respaldo se puede restaurar; producto pesado muestra kg/gramos correctamente.

## 8. Revisión tras completar la GUI (2026-11-02, actualizada 2026-11-22)

Esta revisión se actualizó después de aplicar correcciones de producción y del refactor. «No expuesto por la GUI» no equivale a corregido en backend; los estados por hallazgo distinguen ambos casos.

| Pri. | Hallazgo | Estado tras revisar la GUI | Evidencia / riesgo actual |
| :-- | :-- | :-- | :-- |
| **P0** | Acceso a credenciales demo conocidas | **Corregido:** se retiraron botones, credenciales visibles, acceso rápido y generación demo del flujo de onboarding de la GUI. | `Supermercado.GUI/Views/LoginView.axaml`; `ViewModels/LoginViewModel.cs`; `ViewModels/RegistroUsuarioViewModel.cs`. |
| **P0** | Pago `PagoPuntos` fabricado | **Fuera del alcance de `main`:** no hay tipo `PagoPuntos`, programa de puntos ni `PuntosARedimir`; es objetivo futuro conservado en rama `completo`. `SolicitudVenta` solo transporta pagos monetarios (`Supermercado.Logica/Ventas.cs:311-317`). | `ServicioVentas.RegistrarVentaAsync` (`Supermercado.Logica/Ventas.cs:31-70`); `Supermercado.Entidades/Ventas.cs:154-190`. |
| **P0 backend** | Escalada de privilegios vía `GestionarPersonal` | **Mitigado:** crear empleado o usuario exige que el rol no supere el alcance del actor (`ExigirAlcanceDelRolAsync`), y cambiar rol exige `GestionarRoles`. La GUI de personal ya existe y respeta esa separación. | `ServicioPersonal` (`Supermercado.Logica/Personal.cs:36-56`, `:120`, `:158-186`, `:240-330`); `PersonalRolesView.axaml`. |
| **P1** | Reintento de cobro / idempotencia | **Mitigado en POS y esquema:** se reutiliza la clave durante el reintento; tras persistir, carrito y clave se limpian antes de imprimir, por lo que un fallo de impresión no crea otra venta. Índice único (`Contexto.cs:115`) y chequeo de duplicados al arranque (`Semillas.cs:143-144`). Residuo menor: si dos solicitudes con la misma clave llegan a la vez, la segunda lanza `DbUpdateException` en vez de devolver la venta existente; no duplica, pero muestra un error. | `PosViewModel.Operaciones.cs`; `Supermercado.Logica/Ventas.cs:33-51`; `Contexto.cs:115`; `Semillas.cs:143-144`. |
| **P1** | Scope de DI y `DbContext` | **Corregido:** ViewModels resuelven servicios de negocio dentro de un scope por operación; la sesión singleton permanece entre pantallas. | `Supermercado.GUI/App.axaml.cs`; ViewModels de POS, inventario, turnos, dashboard, personal, compras, contabilidad, login y registro. |
| **P1** | Sesión cacheada y permisos visuales | **Parcial:** el menú ya filtra módulos por rol (`AplicarPermisosDeNavegacion`) y las operaciones protegidas dependen de `Exigir`; pero `SesionActual` solo revoca si el objeto en memoria aparece inactivo y no consulta cambios de rol/estado en BD. | `SesionActual` (`Supermercado.Logica/Seguridad.cs:407-440`); `ShellViewModel.cs:205-219`; `ShellView.axaml:87-135`. |
| **P1** | Secretos y PII en SQLite | **Sigue abierto en persistencia; no expuesto en pantalla de integraciones.** La base y respaldos mantienen valores en claro; el usuario con acceso al archivo puede leer PII y tokens configurados. | `ServicioParametros` (`Supermercado.Logica/Seguridad.cs:343-393`); `Infraestructura.cs:21-56`; `RutasDatos` (`Contexto.cs:153`). |
| **P1** | Respaldo en caliente/ruta de origen libre | **Backend abierto, función no expuesta en GUI actual.** `File.Copy` de la BD activa y `rutaOrigen` arbitraria. | `ServicioRespaldo` (`Infraestructura.cs:21-56`); no existe navegación de respaldos en `ShellViewModel`. |
| **P1** | Concurrencia multi-terminal | **No habilitada ni soportada.** SQLite usa configuración básica, sin WAL ni `busy_timeout`; el check-then-insert de turno no está protegido por índice único parcial. Un solo proceso y operación serial no sustituyen esas garantías. | `ExtensionesDatos` (`Contexto.cs:182-233`); `ServicioCaja` (`Supermercado.Logica/Caja.cs:12`); ADR-010 en `docs/decisiones.md`. |
| **P1** | Validaciones de inventario | **Parcialmente mitigado:** merma comprueba que el producto corresponda al lote; traslado excluye lotes vencidos; el kardex calcula saldo inicial. El índice del lote evita duplicados en persistencia, pero el error de duplicado aún necesita traducción amigable; la GUI ya expone creación de productos e ingreso a bodega. | `ServicioInventario` (`Supermercado.Logica/Inventario.cs:63-330`); `ServicioProductos.RegistrarIngresoLoteAsync` (`Supermercado.Logica/Productos.cs:142`); pruebas de inventario. |
| **P1** | Margen de productos pesados | **Corregido y probado:** convierte costo por kg desde gramos y suma ingreso una sola vez por producto, incluso con salidas de varios lotes. | `MargenPorCategoriaWidget` (`DashboardWidgets.cs:280`); `Supermercado.Pruebas/DashboardTests.cs`. |
| **P2** | Permisos de escritura demasiado amplios | **Parcial:** el menú ya filtra por rol, pero `RegistrarGastoAsync` sigue usando `VerFinanzas` y los parámetros siguen usando `GestionarPersonal`. No confundir ocultar botones con autorización. | `Supermercado.Logica/Finanzas.cs:32`; `Supermercado.Logica/Seguridad.cs:370`; `ShellViewModel.cs:205-219`. |
| **P2** | Lecturas sin `Exigir` | **Abierto.** Productos, impuestos, categorías y proveedores se listan sin permiso; el dashboard sí filtra widgets por permiso. La GUI muestra solo los módulos permitidos, pero eso no autoriza la lectura. | `Supermercado.Logica/Productos.cs:101`, `:112`, `:117`, `:213`, `:338`, `:395`; `Supermercado.Logica/Personal.cs:31`. |
| **P2** | Overposting/entidades en contratos | **Parcialmente mitigado por GUI.** Los ViewModels crean sus pagos internamente y no enlazan directamente el editor a una entidad arbitraria; `SolicitudVenta.Pagos` y los servicios de personal/productos siguen aceptando entidades completas. | `SolicitudVenta` (`Supermercado.Logica/Ventas.cs:311`); `Supermercado.Logica/Personal.cs:36`; `Supermercado.Logica/Productos.cs:30`. |
| **P2** | Overflow y límites numéricos | **Sigue abierto en dominio.** Controles numéricos de la GUI no sustituyen `checked` ni límites en las reglas; operaciones de pesos/gramos pueden desbordarse con entradas extremas. | `Dinero` (`Supermercado.Entidades/Base.cs:10-18`); `Supermercado.Logica/Productos.cs:55`; `Iva` (`Supermercado.Entidades/Finanzas.cs:89`). |
| **P2** | Timing y política de autenticación | **Mensajes no corrigen el timing.** El usuario inexistente no verifica hash dummy; clave mínima 8 y PIN solo longitud/dígitos. | `ServicioAutenticacion` (`Supermercado.Logica/Seguridad.cs:13-180`). |
| **P2** | Semilla demo habilitada por defecto en onboarding | **Corregido en la GUI:** ya no genera datos demo desde login ni onboarding. El generador permanece registrado siempre para pruebas internas. | `LoginViewModel.cs`; `RegistroUsuarioViewModel.cs`; `Composicion.cs:60`. |
| **P2** | Migración inicial no incremental | **Sigue abierto.** La migración genera el DDL desde el modelo actual al ejecutarse; bases instaladas con esquema anterior no reciben alteraciones incrementales. | `Migraciones/Inicial.cs:9`; `InicializadorBaseDatos` (`Semillas.cs:120`). |
| **P2** | Validación de formularios | **Parcial.** Hay verificación de DV NIT y validación final en backend; el alta de cliente en POS solo valida documento/nombre no vacíos, no cada campo con las mismas reglas ni normaliza antes de enviar. | `PosViewModel`; `Validador` (`Comun.cs:10-45`). |

**Conclusión de riesgo:** el acceso demo, la escalada de privilegios, el reintento, el scope DI, los traslados vencidos y el cálculo del margen quedaron corregidos y probados; el pago de puntos queda fuera del alcance de `main` (objetivo futuro conservado en rama `completo`). Aún no recomiendo producción: continúan cifrado/gestión de secretos, concurrencia entre terminales, sesión que no se revalida ante cambios externos, migración incremental y otros P2 listados en esta sección. La suite actual está en verde; su conteo por sí solo no certifica todos los controles.

## 9. Pendientes de la pantalla de Configuración (2026-11-02)

- **Preferencia por máquina, no por usuario:** el tema se guarda en `~/.local/share/SIGS/preferencias.json`
  junto a la base; en un equipo compartido, el último cambio gana. Si se requiere por usuario, mover la
  preferencia a `Parametros` con clave por `UsuarioId` y auditarla (`ServicioParametros`, `Supermercado.Logica/Seguridad.cs:343`).
- **Sin «seguir al sistema»:** solo hay tres temas explícitos. Añadir una cuarta opción requiere escuchar
  `ActualThemeVariantChanged` y decidir qué hacer con el alto contraste.
- **Textos legales sin revisión jurídica:** los archivos de `Assets/Legal/` son borradores. Antes de distribuir,
  un abogado debe validar finalidad, retención, derechos del titular y el aviso DIAN.
- **Secciones futuras:** Integraciones/credenciales (BYOK), respaldos y puerto de impresora siguen fuera de la
  pantalla; ver secciones 4 y 5.
- **Alto contraste no cubre controles de terceros no mapeados:** `Resources/SemiBridge.axaml` solo cubre lo que SIGS usa.
  Al adoptar un control nuevo de Semi, añadir sus claves finales o quedará con la paleta de Semi.
