# Pendientes de interfaz (handoff backend → UI)

Contrato backend→GUI y auditoría posterior a la implementación de la interfaz. La GUI ya tiene POS, turnos, inventario, dashboard, clientes y login; la revisión del **2026-11-02** encontró riesgos nuevos y controles de backend aún abiertos. La suite tiene **329 pruebas en verde**. Esta nota no certifica preparación para producción.

## 1. Validación de formularios

El backend rechaza datos inválidos con `ValidacionException` (mensaje listo para mostrar). Para avisar mientras se escribe, la UI reutiliza **las mismas reglas** de `Supermercado.Logica/Comunes/Validador.cs` (`Es*` devuelven `bool`, `Exigir*` lanzan y normalizan).

| Campo | Regla | Método | Formularios |
| :-- | :-- | :-- | :-- |
| Nombres / apellidos | Solo letras, espacios, `'`, `.`, `-`; sin números; máx. 80 | `EsNombreValido` | Empleado, Cliente |
| Documento | Solo dígitos, 5–15 | `EsDocumentoValido` | Empleado, Cliente |
| NIT | 6–12 dígitos, `-DV` opcional | `EsNitValido` | Proveedor |
| Teléfono | Opcional; 7–10 dígitos (admite `+57`, espacios y guiones al escribir) | `EsTelefonoValido` | Empleado\*, Cliente, Proveedor |
| Correo | Opcional; formato `usuario@dominio.tld` | `EsCorreoValido` | Empleado, Cliente, Proveedor |

\* `Empleado` no tiene teléfono en el modelo actual.

Pendiente en GUI: restringir la entrada (no aceptar dígitos en nombres), mostrar el error junto al campo, deshabilitar «Guardar» mientras sea inválido y mostrar `ValidacionException.Message` (vía `ITraductorExcepciones`) cuando el backend rechace. La validación de la UI es **solo defensa en profundidad**: el backend siempre vuelve a validar.

Pendiente en backend: validar `Producto` (nombre/código de barras con formato) si se decide una regla de código (EAN-8/13/UPC); hoy solo exige no vacío y precio positivo (`ServicioProductos.cs:92-104`).

## 2. Navegación por teclado y atajos

El backend no impone atajos; solo garantiza que cada acción sea **una llamada atómica** (sin estados intermedios). A cargo de la GUI:

- Orden de tabulación lógico en todos los formularios; `Enter` avanza/confirma, `Esc` cancela.
- Caja: campo de código siempre enfocado; el lector USB (keyboard wedge) envía dígitos + `Enter` → `IServicioProductos.BuscarPorCodigoBarrasAsync`.
- Atajos propuestos: cobrar, buscar producto, asociar cliente, anular línea, cancelar venta. Definirlos en un único lugar y mostrarlos en la UI.
- **Doble disparo:** deshabilitar el comando mientras `RegistrarVentaAsync` está en curso. El backend ya acepta `SolicitudVenta.ClaveIdempotencia` (`ServicioVentas.cs:33-39`), pero el índice de `Venta.ClaveIdempotencia` **no es único** (`ConfiguracionModelo.cs:51`): dos solicitudes concurrentes aún pueden duplicar la venta. La UI debe generar un **GUID por intento de cobro**, reutilizarlo solo al reintentar la misma operación y mantener el botón deshabilitado. La unicidad real es un pendiente de backend (sección 6).
- **Una operación, un scope de DI:** no compartir `SigsDbContext` ni servicios `Scoped` entre tareas. La GUI debe resolver los servicios dentro de un `IServiceScope` por operación (ver sección 7).

## 3. Clientes y puntos (caja)

Backend: `IServicioClientes` (`Supermercado.Logica/Clientes`), `SolicitudVenta(Lineas, Pagos, ClienteId, PuntosARedimir, ClaveIdempotencia)`. Ver ADR-018.

- Pantalla de gestión de clientes (permiso `GestionarClientes`): crear, editar, desactivar, listar.
- En caja: buscar cliente por documento (`BuscarPorDocumentoAsync`, lo puede usar el cajero) y mostrar nombre y saldo de puntos.
- Redención: campo «puntos a redimir» (≤ saldo; el equivalente en pesos = puntos × `puntos.valor_punto_pesos`). El backend recalcula y valida; la UI solo previsualiza.
- **Pagos:** la UI solo debe construir `PagoEfectivo`, `PagoTarjeta` y `PagoTransferencia`. **Nunca** debe enviar `PagoPuntos` dentro de `Pagos`: el backend lo genera a partir de `PuntosARedimir` (`ServicioVentas.cs:196-200`). Hoy `ServicioVentas` copia cualquier `Pago` de la solicitud (`ServicioVentas.cs:59-60`) y `PagoPuntos.Validar` solo exige `Puntos > 0` y `Monto > 0` (`PagoPuntos.cs:13-19`), lo que permite pagar sin cliente y sin descontar puntos. Es un **bloqueante de backend**; la UI no debe exponer un editor de pagos genérico ni permitir instanciar `PagoPuntos`.
- Mostrar puntos ganados tras la venta (`Venta.PuntosGanados`). El `GeneradorTicketQuestPdf` ya imprime cliente, documento, puntos ganados, redimidos y saldo (`GeneradorTicketQuestPdf.cs:49-63`).
- Parámetros editables desde Configuración: `puntos.pesos_por_punto` (0 desactiva la acumulación) y `puntos.valor_punto_pesos` (0 desactiva la redención).

## 4. APIs opcionales y Gestión de Credenciales en UI (Bring Your Own Keys)

Backend: ADR-019 y ADR-020. Las integraciones de pago o con planes escalables son **soportadas por el presupuesto del supermercado**. El software incluye los conectores, pero el supermercado ingresa sus propias claves desde la pantalla de configuración y asume el costo de suscripción si supera los planes gratuitos.

### Pantalla en GUI: Configuración → «Integraciones y Conectividad»

Todos los valores se guardan en la tabla `Parametros` mediante `IServicioParametros.GuardarAsync(clave, valor)` (hoy exige permiso `GestionarPersonal`; ver hallazgo de permisos en la sección 6).

| Servicio | Parámetros a enlazar | Campos visuales | Acciones / Enlaces externos (Abrir navegador) |
| :-- | :-- | :-- | :-- |
| **TRM (Datos Abiertos Colombia)** | `integraciones.trm_habilitada` (`1`/`0`)<br>`integraciones.trm_token` | Switch activar/desactivar<br>TextBox para App Token | Enlace «Obtener App Token gratuito»:<br>`https://www.datos.gov.co/profile/edit/developer_settings` |
| **Códigos de barras (Open Food Facts)** | `integraciones.codigo_barras_habilitado` (`1`/`0`) | Switch activar/desactivar | Enlace «Base de datos abierta ODbL»:<br>`https://world.openfoodfacts.org` |
| **Nube / Réplica (Supabase o Neon)** | `integraciones.supabase_habilitado` (`1`/`0`)<br>`integraciones.supabase_url`<br>`integraciones.supabase_key` | Switch activar/desactivar<br>TextBox para Project URL<br>PasswordBox para API Key | • Enlace «Abrir Dashboard / Crear Proyecto»:<br>`https://supabase.com/dashboard`<br>• Enlace «Ver Planes y Precios (Pro $25/mes)»:<br>`https://supabase.com/pricing` |
| **Envío de recibos por correo (Resend)** | `integraciones.resend_token`<br>`integraciones.resend_remitente` | PasswordBox para API Key<br>TextBox para correo remitente | • Enlace «Obtener API Key en Resend»:<br>`https://resend.com/api-keys`<br>• Enlace «Ver Planes y Precios (Pro $20/mes)»:<br>`https://resend.com/pricing` |

### Reglas para la UI al manejar credenciales y enlaces:

1. **Seguridad visual:** los campos de API Key / Token deben ser tipo contraseña (`PasswordChar = '•'`) con icono de ojo para alternar visibilidad.
2. **No volver a mostrar el secreto:** al abrir la pantalla, mostrar un placeholder («•••• configurado») y no leer el valor completo desde `Parametros` para pintarlo. `IServicioParametros.ObtenerTextoAsync` no exige permiso y puede devolver secretos (`ServicioParametros.cs:15-19`).
3. **Acción de apertura en navegador:** en Avalonia, invocar el launcher del sistema operativo para abrir las URLs de registro/pago sin bloquear la app:
   ```csharp
   TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(new Uri(url));
   ```
4. **Resiliencia en caja:** si alguna API no está configurada (campo vacío) o no hay internet, la caja no muestra diálogos modales ni bloquea el cobro; la venta continúa normalmente en modo local.
5. **Supabase:** usar **solo la clave `anon`** con RLS restrictiva; **nunca** `service_role` en el cliente. El esquema de `docs/supabase-schema.sql` ya define política solo `INSERT` para `anon` y vistas con `security_invoker=true`; falta verificarlo en el proyecto desplegado. Exigir `https://` en la URL.
6. **No llamar `GuardarRespaldoAsync` desde la UI:** ese método de `IServicioParametros` no exige permiso ni deja auditoría (`ServicioParametros.cs:50-57`); es un mecanismo interno.

## 5. Otros pendientes que tocan UI

- Impresión física: hoy solo hay PDF (`IGeneradorTicket`). Si se implementa ESC/POS, la UI necesitará elegir impresora y probar impresión.
- Dashboard: añadir widgets de clientes/puntos si se desea (no existen).
- Respaldos: avisar que el archivo `.db` contiene hashes, tokens de integración y datos personales; no guardarlo en carpetas sincronizadas a la nube sin cifrado. El respaldo actual usa `File.Copy` sobre SQLite vivo (`ServicioRespaldo.cs:42`), lo que puede producir copias inconsistentes.
- Datos demo: `GeneradorDatosDemo` está registrado siempre (`ExtensionesLogica.cs:68`) y crea `admin_demo` / `cajero_demo` con claves conocidas (`GeneradorDatosDemo.cs:262-272`). La UI no debe exponer «generar datos demo» en producción; el backend debe condicionarlo al entorno.
- Migraciones: `Inicial` genera el esquema desde el modelo actual en tiempo de ejecución (`Migraciones/Inicial.cs:11-18`). Una base existente no recibe columnas nuevas; antes de actualizar, respaldar y prever migración/recreación.

## 6. Auditoría backend 2026-11-02: hallazgos que condicionan la UI

Prioridad: **P0** bloquea producción, **P1** corrige antes de operar con varias terminales o datos reales, **P2** deuda que la UI debe mitigar.

| Pri. | Hallazgo | Evidencia | Acción UI | Acción backend |
| :-- | :-- | :-- | :-- | :-- |
| **P0** | `PagoPuntos` fabricado paga sin cliente y sin descontar puntos | `ServicioVentas.cs:59-60`, `PagoPuntos.cs:13-19`, `Venta.cs:39-55` | No enviar `PagoPuntos`; enviar solo `ClienteId` + `PuntosARedimir`; no exponer editor de pagos genérico | Rechazar `PagoPuntos` en `SolicitudVenta.Pagos`; construir el pago internamente y validar `Monto = Puntos × valorPunto` contra el cliente |
| **P0** | Escalada de privilegios: un Gerente puede crear un empleado Admin, crearle usuario o restablecer la clave del Admin | `ServicioEmpleados.cs:22-41`, `:102-135`; `ServicioUsuarios.cs:43-64`; `ValoresIniciales.cs:15-24` | Ocultar asignación de rol, creación de usuario y restablecimiento de clave para no-admin; confirmar identidad | Exigir `GestionarRoles` para asignar/cambiar roles y para crear usuarios o restablecer claves de cuentas con rol superior; prohibir que un no-admin otorgue Admin |
| **P1** | Idempotencia no única: dos cobros concurrentes pueden duplicar la venta | `ConfiguracionModelo.cs:51`, `ServicioVentas.cs:33-39`; ADR-021 dice «clave única» | GUID por intento, botón deshabilitado, reintento solo con la misma clave | Índice único filtrado, capturar `DbUpdateException` y devolver la venta existente; acotar la clave al empleado |
| **P1** | La sesión cachea rol/estado y no se revoca al cambiarlos en otro contexto | `SesionActual.cs:19-43`, `ServicioAutenticacion.cs:96` | Tras cambiar rol/desactivar empleado, forzar re-login; no confiar en permisos cacheados | Revalidar permisos contra la BD o versionar/invalidar la sesión |
| **P1** | RLS permisiva en Supabase y vistas sin `security_invoker` | `docs/supabase-schema.sql`; `ValoresIniciales.cs:85-86` | Usar solo `anon` con RLS; nunca `service_role`; exigir HTTPS | **Mitigado en el esquema**: política `FOR INSERT` para `anon` y vistas con `security_invoker=true`; verificar en el proyecto desplegado |
| **P1** | Secretos y PII en SQLite sin cifrar; respaldos y logs sin permisos restrictivos | `ValoresIniciales.cs:83-92`; `ServicioParametros.cs:15-19`, `:36-57`; `ServicioRespaldo.cs:42`; `RutasDatos.cs:9-29` | PasswordBox, no loggear ni mostrar secretos, avisar del contenido de respaldos | Cifrar/OS keyring, permisos `0600`, redactar logs, separar configuración pública de secretos |
| **P1** | Respaldo en caliente con `File.Copy` y `rutaOrigen` arbitraria | `IServicioRespaldo.cs:5`, `ServicioRespaldo.cs:21-23`, `:42` | No exponer ruta libre; avisar que el respaldo puede ser inconsistente | Usar `VACUUM INTO` o `SqliteConnection.BackupDatabase`; validar ruta; verificar restauración |
| **P1** | Concurrencia multi-terminal sin tokens ni bloqueos; SQLite sin WAL/busy_timeout | `ExtensionesDatos.cs:19-20`; `ServicioCaja.cs:33-37`; ADR-010 (`docs/decisiones.md:102-113`) | Serializar operaciones, deshabilitar botones, reintentar ante «database is locked»; no prometer multi-terminal | Tokens de concurrencia, índice único parcial de turno abierto, WAL/busy_timeout, migrar a PostgreSQL para varias cajas |
| **P1** | Inventario: merma no valida producto/lote; traslado permite vencidos; ingreso duplicado; kardex sin saldo inicial | `ServicioInventario.cs:55-68`, `:87-107`, `:129-137`; `ServicioProductos.cs:58-90`; `ServicioConsultaInventario.cs:85-86` | Validar producto/lote, advertir vencidos, impedir lote duplicado, mostrar saldo inicial correcto | Corregir validaciones, bloquear traslado de vencidos, traducir duplicados, calcular saldo previo |
| **P1** | Productos pesados: precio/costo por kg y cantidades en gramos sin conversión en reportes | `ProductoPesado.cs:5-13`; `GeneradorDatosDemo.cs:120`; `MargenPorCategoriaWidget.cs:52-64` | Etiquetar «gramos» y «precio por kg»; convertir a kg al mostrar; no reutilizar el margen actual | Convertir costo `CostoUnitario × gramos / 1000`; evitar doble conteo de ingreso cuando una línea sale de varios lotes |
| **P2** | Permisos de lectura usados para escritura y permisos demasiado amplios | `ServicioFinanzas.cs:19-21` (gasto con `VerFinanzas`); `ServicioMantenimientoProductos.cs:50-55` (precio con `GestionarProductos`, que tiene Bodeguero); `ServicioParametros.cs:36-38` (secretos con `GestionarPersonal`) | Ocultar acciones sensibles según rol; no asumir que «Ver» es solo lectura | Separar permisos de escritura (`RegistrarGastos`, `CambiarPrecios`, `GestionarIntegraciones`) |
| **P2** | Lecturas sin `Exigir`: productos, categorías, cajas, roles e impuestos | `ServicioProductos.cs:36-37`; `ServicioBusquedaProductos.cs:9-28`; `ServicioCatalogos.cs:17-18`; `ServicioEstructura.cs:18-19`, `:100-101`; `ServicioImpuestos.cs:16-17` | No confiar en el servicio para autorización; aplicar filtro de rol en la pantalla | Añadir `Exigir` coherente a cada lectura |
| **P2** | APIs con entidades completas permiten overposting (`Id`, `Activo`, `RolId`, `Pago`) | `ServicioEmpleados.cs:22`, `:43`; `ServicioProductos.cs:21`; `SolicitudVenta.cs:7-12` | Bindear solo campos permitidos; no reutilizar entidades entre formularios | Usar comandos/DTOs y construir entidades en el backend |
| **P2** | Aritmética de dinero sin `checked` ni topes; posible overflow con precios/cantidades extremas | `ProductoPesado.cs:12-13`; `Iva.cs:8-11`; `Dinero.cs:8-12` | Limitar cantidad/precio en la UI y mostrar error | Aritmética comprobada y límites de dominio |
| **P2** | Autenticación: enumeración por timing; clave solo longitud 8; PIN 4 dígitos sin complejidad | `ServicioAutenticacion.cs:35-36`, `:49`, `:64-70` | Mensajes genéricos, no revelar si el usuario existe; exigir clave fuerte | Hash dummy para usuario inexistente; política de complejidad; límites de PIN |
| **P2** | Datos demo con credenciales conocidas siempre registrados | `ExtensionesLogica.cs:68`; `GeneradorDatosDemo.cs:262-272` | No exponer en producción | Registrar solo en Development y exigir confirmación |
| **P2** | Migración `Inicial` no determinista; bases existentes no reciben columnas nuevas | `Migraciones/Inicial.cs:11-18`; `InicializadorBaseDatos.cs:12-15` | Avisar y respaldar antes de actualizar | Migraciones incrementales reales con snapshot |
| **P3** | Documentación desactualizada | `docs/pendientes-ui.md:30` (decía que no había idempotencia), `:39` (decía que el ticket no imprimía puntos); `docs/arquitectura.md:178` (decía ticket nulo) | Usar esta nota como fuente | Actualizar docs al cerrar cada hallazgo |

### Reglas de oro para la UI

1. **La UI nunca es la autoridad.** `ISesionActual.Tiene` solo sirve para mostrar/ocultar; la autorización real es `Exigir` en el servicio.
2. **No construir entidades de pago.** La UI arma comandos de pago simples; el backend decide el `PagoPuntos`.
3. **Un scope por operación.** No mantener un `DbContext` vivo entre pantallas ni compartirlo entre tareas.
4. **Botón único.** Toda operación que escribe se deshabilita mientras está en curso y se rehabilita al terminar; usar `CancellationToken` cuando aplique.
5. **No mostrar secretos.** PasswordBox, placeholder, sin logs, sin portapapeles, sin capturas.
6. **No prometer multi-terminal.** Mientras ADR-010 siga abierto, una sola terminal; si hay varias, esperar errores de bloqueo y reintentar.
7. **Unidades explícitas.** Gramos para pesados; pesos enteros para dinero; puntos como enteros.
8. **Errores traducidos.** Mostrar `ITraductorExcepciones.Traducir(ex)`; nunca el stack trace. Los `DbUpdateException` de duplicados hoy caen en el mensaje genérico: la UI debe decir «dato duplicado» solo si el backend lo traduce.

## 7. Contrato de integración para la GUI

- **Composición:** llamar `services.AgregarLogica()` una vez en el arranque. La GUI no referencia `Supermercado.Datos`.
- **Ciclo de vida:** `ISesionActual` es singleton; los servicios y repositorios son `Scoped`; `SigsDbContext` es `Scoped`. Resolver cada operación con `IServiceScopeFactory.CreateScope()` y disponer el scope al terminar.
- **Ventas:** generar `ClaveIdempotencia = Guid.NewGuid().ToString("N")` al abrir el cobro; conservarla si se reintenta la misma venta; descartarla si el cajero cancela y empieza otra. Enviar `Lineas`, `Pagos` (sin `PagoPuntos`), `ClienteId` y `PuntosARedimir`. Validar en la UI que `TotalPagado >= Total` y que el cambio solo se da en efectivo.
- **Pagos:** `PagoEfectivo { Monto }`, `PagoTarjeta { Monto, UltimosDigitos }` (4 dígitos), `PagoTransferencia { Monto, Referencia }`. Crear objetos nuevos por venta; no reutilizar instancias.
- **Puntos:** previsualizar `Puntos × valor_punto_pesos`; no descontar en la UI; el backend descuenta y acumula en la misma unidad de trabajo.
- **Inventario:** para `ProductoPesado`, `Cantidad` va en gramos y `PrecioVenta`/`CostoUnitario` por kg; `StockMinimoEstante` en gramos. Mostrar kg y precio/kg; validar que la merma use el mismo producto y lote.
- **Permisos:** cargar la matriz desde `Rol.Permisos` solo para visibilidad; tras un cambio de rol, cerrar sesión y volver a autenticar.
- **Integraciones:** guardar claves con `GuardarAsync`; no leerlas de vuelta para mostrarlas; probar conexión con un botón explícito; ejecutar la sincronización en segundo plano y no bloquear la caja.
- **Respaldos:** no expone `rutaOrigen`; usar la carpeta configurada; avisar que el archivo contiene PII/secretos; verificar restauración antes de confiar.
- **Errores:** `ValidacionException`, `VentaInvalidaException`, `StockInsuficienteException`, `TurnoInvalidoException`, `AccesoDenegadoException`, `DebeCambiarClaveException` tienen mensaje mostrable; el resto se registra y se muestra un mensaje genérico.
- **Pruebas de aceptación:** doble clic en cobrar no duplica venta; venta con puntos descuenta saldo; intento de `PagoPuntos` fabricado se rechaza; usuario sin permiso no ve la acción ni puede invocarla; cambio de rol exige re-login; respaldo se puede restaurar; producto pesado muestra kg/gramos correctamente.

## 8. Revisión tras completar la GUI (2026-11-02)

Esta revisión se actualizó después de aplicar correcciones de producción. «No expuesto por la GUI» no equivale a corregido en backend; los estados por hallazgo distinguen ambos casos.

| Pri. | Hallazgo | Estado tras revisar la GUI | Evidencia / riesgo actual |
| :-- | :-- | :-- | :-- |
| **P0** | Acceso a credenciales demo conocidas | **Corregido:** se retiraron botones, credenciales visibles, acceso rápido y generación demo del flujo de onboarding de la GUI. El generador queda fuera del arranque normal. | `Supermercado.GUI/Views/LoginView.axaml`; `ViewModels/LoginViewModel.cs`; `ViewModels/RegistroUsuarioViewModel.cs`. |
| **P0 backend** | Pago `PagoPuntos` fabricado | **Mitigado en servicio:** se rechaza cualquier `PagoPuntos` enviado en `SolicitudVenta.Pagos`; la GUI solo arma métodos monetarios. La UI aún no ofrece redención de puntos. | `ServicioVentas.cs`; `ServicioVentasTests.cs`. |
| **P0 backend** | Escalada de privilegios vía `GestionarPersonal` | **Mitigado:** la creación de empleado/usuario no puede asignar un rol con permisos superiores al actor; el restablecimiento de claves exige `GestionarRoles`. No hay módulo de personal en la GUI todavía. | `ServicioEmpleados.cs`; `ServicioUsuarios.cs`; pruebas homónimas. |
| **P1** | Reintento de cobro / idempotencia | **Mitigado en POS y esquema:** se reutiliza la clave durante el reintento; tras persistir, carrito y clave se limpian antes de imprimir, por lo que un fallo de impresión no crea otra venta. Índice único y chequeo de duplicados al arranque. | `PosViewModel.cs`; `ConfiguracionModelo.cs`; `InicializadorBaseDatos.cs`; `ServicioVentas.cs`. |
| **P1** | Scope de DI y `DbContext` | **Corregido:** ViewModels resuelven servicios de negocio dentro de un scope por operación; la sesión singleton permanece entre pantallas. | `Supermercado.GUI/App.axaml.cs`; ViewModels operativos de POS, inventario, turnos, dashboard, login y registro. |
| **P1** | Sesión cacheada y permisos visuales | **Sigue abierto.** `SesionActual` solo revoca si el objeto en memoria aparece inactivo; no consulta cambios de rol/estado en BD. La navegación muestra todos los módulos a cualquier rol. Las operaciones protegidas dependen de `Exigir`; el dashboard sí filtra widgets por permiso. | `SesionActual.cs:19-42`; `ShellView.axaml:52-107`; `ServicioDashboard.cs:14-15`. |
| **P1** | RLS Supabase | **Mitigado solo en el esquema**, no verificado en un proyecto desplegado. La GUI no contiene pantalla de configuración/sincronización ni usa una clave Supabase. | `docs/supabase-schema.sql`; sin conexión/configuración Supabase en `Supermercado.GUI`. |
| **P1** | Secretos y PII en SQLite | **Sigue abierto en persistencia; no expuesto en pantalla de integraciones.** La base y respaldos mantienen valores en claro; el usuario con acceso al archivo puede leer PII y tokens configurados. | `ServicioParametros.cs:15-18`; `ServicioRespaldo.cs:42`; `RutasDatos.cs`. |
| **P1** | Respaldo en caliente/ruta de origen libre | **Backend abierto, función no expuesta en GUI actual.** `File.Copy` de la BD activa y `rutaOrigen` arbitraria. | `ServicioRespaldo.cs:17-23,42`; no existe navegación de respaldos en `ShellViewModel`. |
| **P1** | Concurrencia multi-terminal | **No habilitada ni soportada.** SQLite usa configuración básica, sin WAL ni `busy_timeout`; el check-then-insert de turno no está protegido por índice único parcial. Un solo proceso y operación serial no sustituyen esas garantías. | `ExtensionesDatos.cs:18-20`; `ServicioCaja.cs` (apertura); ADR-010 en `docs/decisiones.md`. |
| **P1** | Validaciones de inventario | **Parcialmente mitigado:** merma comprueba que el producto corresponda al lote; traslado excluye lotes vencidos; el kardex calcula saldo inicial. El índice del lote evita duplicados en persistencia, pero el error de duplicado aún necesita traducción amigable; la GUI no expone ingreso/traslado. | `ServicioInventario.cs`; `ServicioProductos.cs:58-90`; `ServicioConsultaInventario.cs`; pruebas de inventario. |
| **P1** | Margen de productos pesados | **Corregido y probado:** convierte costo por kg desde gramos y suma ingreso una sola vez por producto, incluso con salidas de varios lotes. | `MargenPorCategoriaWidget.cs`; `WidgetsTests.cs`. |
| **P2** | Permisos escritura demasiado amplios | **Backend abierto; algunas acciones aún no tienen pantalla.** `RegistrarGastoAsync` usa `VerFinanzas`; parámetros usan `GestionarPersonal`; el menú no filtra módulos por rol. No confundir ocultar botones con autorización. | `ServicioFinanzas.cs:19-21`; `ServicioParametros.cs:36-38`; `ShellView.axaml:52-107`. |
| **P2** | Lecturas sin `Exigir` | **Abierto.** La búsqueda de productos y consultas de catálogos/cajas/roles/impuestos no siempre exigen permiso; el dashboard sí filtra widgets. La GUI permite navegar módulos sin filtrar por rol. | `ServicioBusquedaProductos.cs:9-28`; `ServicioCatalogos.cs:17-18,57-58`; `ServicioEstructura.cs:18-19,60-61,100-101`; `ServicioImpuestos.cs:16-17`. |
| **P2** | Overposting/entidades en contratos | **Parcialmente mitigado por GUI.** Los ViewModels crean sus pagos internamente y no enlazan directamente el editor a una entidad arbitraria; el contrato `SolicitudVenta.Pagos` y servicios de empleados/productos siguen aceptando entidades completas. | `SolicitudVenta.cs:7-12`; `ServicioEmpleados.cs:22,43`; `ServicioProductos.cs:21`. |
| **P2** | Overflow y límites numéricos | **Sigue abierto en dominio.** Controles numéricos de la GUI no sustituyen `checked` ni límites en las reglas; operaciones de pesos/gramos pueden desbordarse con entradas extremas. | `ProductoPesado.cs:12-13`; `Iva.cs:8-11`; `Dinero.cs:8-12`. |
| **P2** | Timing y política de autenticación | **Mensajes no corrigen el timing.** El usuario inexistente no verifica hash dummy; clave mínima 8 y PIN solo longitud/dígitos. | `ServicioAutenticacion.cs:64-88,29-36,45-50`. |
| **P2** | Semilla demo habilitada por defecto en onboarding | **Corregido:** la GUI ya no genera datos demo desde login ni onboarding. El generador permanece disponible para pruebas internas. | `LoginViewModel.cs`; `RegistroUsuarioViewModel.cs`. |
| **P2** | Migración inicial no incremental | **Sigue abierto.** La migración genera el DDL desde el modelo actual al ejecutarse; bases instaladas con esquema anterior no reciben alteraciones incrementales. | `Migraciones/Inicial.cs:11-18`; `InicializadorBaseDatos.cs:10-15`. |
| **P2** | Validación de formularios | **Parcial.** Hay verificación de DV NIT y validación final en backend, pero en el formulario de cliente la GUI solo valida documento/nombre no vacíos; no valida por campo con las mismas reglas ni normaliza antes de enviar. | `RegistroUsuarioViewModel.cs:230-260`; `Validador.cs`; `ValidadorDian.cs`. |

**Conclusión de riesgo:** el acceso demo, los dos P0 de pagos/privilegios, el reintento, el scope DI, los traslados vencidos y el cálculo del margen quedaron corregidos y probados. Aún no recomiendo producción: continúan cifrado/gestión de secretos, concurrencia entre terminales, sesión que no se revalida ante cambios externos, migración incremental y otros P2 listados en esta sección. La suite actual tiene 329 pruebas; su conteo por sí solo no certifica todos los controles.
