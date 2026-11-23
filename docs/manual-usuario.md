# Manual de usuario y operación

> **Estado de la interfaz.** La GUI Avalonia incluye login, POS, turnos, clientes, inventario, compras, contabilidad, personal, dashboard y configuración. Los parámetros de negocio y los respaldos se gestionan desde los servicios, no desde una pantalla dedicada. Hay hallazgos de seguridad e integridad abiertos (ver [`pendientes-ui.md`](pendientes-ui.md)); no distribuya ni opere con dinero real hasta cerrarlos. Este manual describe la operación de los módulos disponibles y, donde no hay pantalla, las reglas del servicio.

## 1. Primer arranque

1. Instale el .NET SDK 10.
2. Clone el repositorio y ejecute `dotnet run --project Supermercado.GUI`. La persistencia es SQLite local (*zero-config*): no se instala ni se configura ningún motor externo. La base se crea sola en:
   - Windows: `%LOCALAPPDATA%\SIGS\sigs.db`
   - Linux: `~/.local/share/SIGS/sigs.db`
3. La inicialización crea las tablas, los índices únicos de idempotencia y siembra los 4 roles, cargos, impuestos, una caja y los parámetros iniciales en SQLite.
4. Para cargar el catálogo de prueba, inventario, un turno abierto y las cuentas demo, abra el panel de cuentas de prueba en la pantalla de inicio de sesión y presione **«Sembrar Base Demo (4 Roles + Catálogo + Turno)»**.

### Cuentas y credenciales demo (4 roles)

La pantalla de login cuenta con accesos directos (*chips*) para autocompletar las credenciales de cada rol con un solo clic:

| Rol | Usuario | Contraseña | PIN Caja / Autorización | Nivel / Pantalla principal |
| :-- | :-- | :-- | :-- | :-- |
| **Admin (Administrador)** | `admin` | `Admin123*` | `1111` | Administración general y parámetros |
| **Cajero** | `cajero` | `Cajero123*` | `1234` | POS, Turno/Arqueo y Clientes desde la barra lateral |
| **Gerente** | `gerente` | `Gerente123*` | `3344` | Panel Gerencial, Personal, Finanzas y Métricas |
| **Bodeguero** | `bodega` | `Bodega123*` | `2233` | Inventario FEFO, Mermas y Recepción de Lotes |

> **Objetivo futuro fuera del alcance de `main`.** La arquitectura extendida (PostgreSQL central, Docker, Supabase/Outbox y facturación electrónica) se conserva en la rama `completo`; no son funcionalidades de esta entrega. Los roles adicionales (Supervisor, Compras, Contador, Soporte, Atención al Cliente y Analista de Datos) no existen: «Supervisor» es un mecanismo de autorización (`ValidarSupervisorAsync`), no un rol; «Compras» es un permiso (`GestionarCompras`) y un módulo de la GUI.

### Cambio obligatorio de clave

Mientras `DebeCambiarClave` esté activo, cualquier operación protegida lanza `DebeCambiarClaveException`. El primer paso después del login es `CambiarClaveAsync`. La clave nueva debe tener al menos 8 caracteres.

## 2. Inicio de sesión

| Modo | Quién | Regla |
| :-- | :-- | :-- |
| Usuario y clave | Todos | Clave mínimo 8 caracteres; hash BCrypt. |
| Usuario y PIN | Cajero | PIN mínimo 4 dígitos; mismas protecciones. |

- Tras `seguridad.max_intentos` fallos consecutivos la cuenta se bloquea. Un intento fallido con empleado inactivo también cuenta.
- Un login exitoso reinicia el contador.
- Si el empleado fue desactivado, la sesión se cierra en la siguiente verificación y se deniega el acceso.
- Cada intento, exitoso o no, queda en `Intentos`.

### Mensajes esperados

| Situación | Mensaje |
| :-- | :-- |
| Usuario o clave incorrectos | «Credenciales inválidas». |
| Cuenta bloqueada | «La cuenta está bloqueada». |
| Cambio de clave pendiente | «Debe cambiar la clave antes de continuar». |
| Sin sesión | «No hay una sesión iniciada». |
| Sin permiso | «No tiene permiso para esta operación». |

## 3. Operación de caja

### Abrir turno

1. Verifique que la caja exista y esté `Activa`.
2. Registre el efectivo inicial (entero, en pesos, ≥ 0).
3. El sistema rechaza abrir un segundo turno si ya hay uno abierto para el empleado o para la caja.
4. Queda auditoría de apertura.

### Vender

1. El cajero debe tener turno abierto.
2. Agregue líneas por código de barras. Los productos pesados se ingresan en gramos; los unitarios, en unidades. Los códigos EAN-13 de balanza con prefijo 20–29 se interpretan como PLU + gramos.
3. El sistema valida que el producto exista, esté activo, tenga impuesto y haya stock en estante.
4. El descuento es FEFO: primero el lote que vence antes; los vencidos no se venden.
5. Si el cliente está registrado, asígnelo por documento para identificar la venta (ver sección 5).
6. Registre uno o varios pagos: efectivo, tarjeta (últimos 4 dígitos) o transferencia (referencia).
7. Si el pago supera el total, el sistema calcula el cambio. La venta se completa cuando los pagos cubren el total.
8. El ticket incluye el código de tarifa del IVA según la línea (`PlantillaTicketPos.ObtenerCodigoTarifa`). **No se emite documento electrónico ni se transmite a la DIAN.**
9. Venta y movimientos de stock se guardan en una sola transacción.

### Anular

1. Requiere permiso `AnularVenta` y un motivo no vacío.
2. Solo se anula una venta completada y una sola vez.
3. El stock vuelve al lote de origen en estante y se registra un movimiento de devolución.
4. Queda auditoría con el motivo.

### Cerrar y arquear

1. El sistema suma el efectivo neto de las ventas completadas del turno y resta el de las anuladas.
2. `Esperado = efectivo inicial + efectivo neto`.
3. Ingrese el efectivo contado.
4. `Diferencia = contado − esperado`. Si el valor absoluto supera `caja.tolerancia_arqueo`, el cierre se marca fuera de tolerancia.
5. El cierre es único y deja auditoría con la diferencia.

## 4. Inventario

### Ingreso de mercancía

1. Registre el producto si no existe.
2. Cree el lote con número, fecha de ingreso, vencimiento y costo unitario.
3. El ingreso entra a **bodega**. Desde allí se traslada al estante.
4. Un lote pertenece a un solo producto; el sistema rechaza mezclar stock de productos distintos.

### Traslado a estante

- Se indica producto y cantidad.
- Se toma primero el lote vigente que vence antes.
- Los lotes vencidos no se trasladan al estante; regístrelos como merma o gestione su disposición.
- Cada traslado deja movimiento.

### Merma

- Requiere permiso `AjustarInventario` y motivo.
- Descuenta del estante o de la bodega y registra `Merma`.
- No se puede sacar más de lo que hay.

### Alertas y reabastecimiento

- **Bajo mínimo:** compara el stock de estante contra `StockMinimoEstante` del producto.
- **Por vencer:** usa `inventario.dias_alerta`.
- **Solicitud:** se genera solo si no existe una pendiente para el producto.
- **Atender:** marca la solicitud como `Atendido` y traslada de bodega a estante.
- **Cancelar:** disponible solo mientras esté `Pendiente`; una solicitud cerrada no se reabre.

## 5. Clientes

- **Alta:** documento único (5–15 dígitos), nombres, apellidos, teléfono y correo; el backend valida el formato.
- **Habeas Data:** el cliente autoriza o revoca el tratamiento de datos. Sin autorización, la venta registra «Consumidor Final» (`Venta.DocumentoAdquirente` / `NombreAdquirente`) y no se imprimen sus datos.
- **Venta:** el cajero puede buscar al cliente por documento con permiso `Vender`.
- **Permisos:** gestionar clientes exige `GestionarClientes`.

> **Objetivo futuro fuera del alcance de `main`.** La fidelización con puntos (saldo, acumulación y redención) y los parámetros `puntos.*` se conservan en la rama `completo`; no están disponibles en esta entrega.

## 6. Personal y roles

| Acción | Permiso |
| :-- | :-- |
| Crear, editar o desactivar empleado | `GestionarPersonal` |
| Crear usuario | `GestionarPersonal` |
| Cambiar rol | `GestionarRoles` |
| Editar empleado cambiando su rol | `GestionarPersonal` + `GestionarRoles` |

- El documento del empleado es único.
- Desactivar un empleado no borra su historial: cierra su sesión y conserva ventas, auditoría y nóminas.
- El correo se marca como verificado solo si el verificador responde; por defecto el cliente nulo devuelve «no verificado» y el alta continúa.

## 7. Finanzas

### Gastos y resumen

- Registrar un gasto exige `VerFinanzas` y deja auditoría.
- El resumen del periodo muestra ingresos, egresos, IVA generado y margen.

### Impuestos

- Las tarifas se editan con `GestionarTarifas`.
- Se expresan en puntos básicos: `1900 = 19 %`, `400 = 4 %`.
- El IVA está incluido en el precio de venta y se extrae, no se suma.
- Una retención solo aplica sobre su base mínima.

### Nómina

- Requiere `GestionarPersonal`.
- No se puede calcular dos veces el mismo empleado, año y mes.
- Si falta `nomina.salario_minimo`, el cálculo se detiene con `ParametroFaltanteException`; no se inventan valores.
- El cálculo es básico: salario, auxilio de transporte, salud, pensión y neto.

## 8. Dashboards

- Cada widget declara el permiso que necesita; los que el usuario no puede ver no aparecen.
- La disposición se guarda por empleado.
- Al agregar, la posición es `máximo + 1`; al reordenar, quedan posiciones `0..n−1` sin duplicados.
- Widgets actuales: ventas del día, reabastecimientos pendientes, TRM del día, IVA generado, margen por categoría, lotes por vencer, stock bajo mínimo, ventas por categoría y arqueo del turno.

## 9. Parámetros

Los valores se gestionan desde los servicios con permiso `GestionarPersonal`. El guardado administrativo exige permiso y deja auditoría; el respaldo interno (TRM) es atómico y no exige permiso.

| Clave | Uso |
| :-- | :-- |
| `seguridad.max_intentos` | Bloqueo de cuenta |
| `inventario.dias_alerta` | Ventana de vencimiento |
| `caja.tolerancia_arqueo` | Diferencia aceptada en pesos |
| `nomina.*` | Salario mínimo, auxilio y deducciones |
| `integraciones.timeout_seg` / `integraciones.reintentos` | Consulta de TRM |
| `integraciones.trm_centavos` / `integraciones.trm_fecha` | Último valor de TRM guardado como respaldo |
| `integraciones.trm_habilitada` / `integraciones.codigo_barras_habilitado` | Interruptores (`1` activo, `0` apagado) |
| `integraciones.trm_token` / `integraciones.resend_token` / `integraciones.resend_remitente` | Integraciones vigentes |
| `tributario.responsable_iva` | `1` extrae IVA; `0` lo desactiva (régimen no responsable) |
| `negocio.nombre` / `nit` / `direccion` / `telefono` | Datos del emisor en el ticket |
| `sistema.carpeta_respaldos` | Destino de los respaldos |

Estas son las claves definidas en `ClavesParametro` (`Supermercado.Entidades/Seguridad.cs`). No existen claves `puntos.*`, `dian.*` ni `integraciones.supabase_*` en `main`.

Un parámetro ausente **no** tiene valor por defecto silencioso: lanza `ParametroFaltanteException`. Excepción: los interruptores `integraciones.*_habilitad*` se consideran activos si faltan.

## 10. TRM

- Se consulta a datos.gov.co con timeout y reintentos; el cliente está activo por defecto.
- La espera entre intentos crece 200 ms, 400 ms, … hasta 2 s.
- Si la API responde, se guarda el valor como respaldo en una sola operación.
- Si todos los intentos fallan, se usa el último respaldo. Si nunca hubo uno, devuelve `null`.
- Se puede apagar con `SIGS_TRM_HABILITADA=0` o con `integraciones.trm_habilitada=0`; en ese caso solo se usa el respaldo.

## 11. Contabilidad fiscal (sin emisión DIAN)

- La pantalla de contabilidad calcula el consolidado fiscal y el desglose de IVA por tarifa (`C` / `M` / `J`, según `PlantillaTicketPos.ObtenerCodigoTarifa`).
- Exporta el consolidado a CSV (`Declaracion_Fiscal_DIAN_*.csv`) y genera un PDF de ventas (`ContabilidadDianViewModel`).
- El ticket usa los datos de `negocio.*`, el código de tarifa y `Venta.DocumentoAdquirente` / `NombreAdquirente`, con constantes de consumidor final; la validación del NIT dispone de `Validador.CalcularDigitoVerificacionNit`.
- Los textos legales de la aplicación citan la Resolución 000165 como referencia, sin certificar emisión.
- **No emite documento electrónico ni transmite a la DIAN.** No hay CUDE, XML UBL, QR ni consecutivos en `main`.

> **Objetivo futuro fuera del alcance de `main`.** La emisión, firma y transmisión de documentos electrónicos, los parámetros `dian.*` y la gestión de resolución/vigencia/folios se conservan en la rama `completo`.

## 12. Reportes PDF

- Ticket POS de 80 mm con totales, pagos y datos del emisor y del adquirente; incluye el código de tarifa del IVA.
- Desprendible de nómina (permiso `GestionarPersonal`).
- Reporte de ventas por rango (permiso `VerReportes`).
- Se generan con QuestPDF; no requieren conexión.

## 13. Problemas frecuentes

| Síntoma | Causa probable | Solución |
| :-- | :-- | :-- |
| «No hay una sesión iniciada» | Se llamó un caso de uso sin login | Iniciar sesión |
| «Debe cambiar la clave» | `DebeCambiarClave` activo | Cambiar la clave |
| «No tiene permiso» | Rol sin el permiso | Revisar la matriz de permisos |
| «Stock insuficiente» | No hay unidades en estante o el lote no existe | Reabastecer o revisar el lote |
| «La caja está inactiva» | `Caja.Activa = false` | Activar la caja |
| «La solicitud ya se cerró» | Reabastecimiento atendido o cancelado | Crear una solicitud nueva |
| «Falta el parámetro …» | Clave ausente en `Parametro` | Cargar el parámetro |
| La base no aparece | Ruta de datos distinta | Revisar `RutasDatos.BaseDatosPorDefecto()` |

## 14. Buenas prácticas de operación

- Cierre el turno antes de cambiar de cajero; no comparta usuarios.
- Registre la merma en el momento; no la acumule para el cierre.
- No edite la base directamente: use los servicios para conservar auditoría.
- Use `ServicioRespaldo` (copia con `File.Copy` y deja auditoría): copie con la aplicación cerrada o use la herramienta `sqlite3` con `VACUUM INTO` como recomendación operativa de SQLite, no como una función del sistema (ver [`despliegue.md`](despliegue.md)).
- Revise los parámetros de nómina al inicio de cada año y los datos de `negocio.*` antes de imprimir tickets.
- Trate los datos del cliente como información personal: no los exporte ni los comparta sin autorización (Ley 1581).
- No comparta los tokens de integración (`integraciones.trm_token`, `integraciones.resend_token`).

## 15. Configuración: apariencia, textos legales y Acerca de

Se abre desde el pie de la barra lateral («Configuración»), desde el icono de engranaje de la barra compacta de
cajero y desde el pie del inicio de sesión. No exige permiso: cualquier rol puede cambiar el tema y leer los
textos legales. No expone parámetros de negocio ni credenciales.

- **Apariencia:** tres tarjetas — Claro cálido, Oscuro Slate y Alto contraste. Al elegir una se aplica de
  inmediato y se guarda en la máquina; el próximo arranque abre con ese tema. El botón de sol del shell cicla
  entre los tres.
- **Legal:** política de privacidad y términos de servicio empaquetados con la aplicación. Se leen sin conexión.
  Ambos son **borradores operativos sujetos a revisión jurídica**; no sustituyen la asesoría legal del negocio.
- **Acerca de:** versión de la aplicación y aviso de que SIGS no es una certificación DIAN.

La preferencia vive en `~/.local/share/SIGS/preferencias.json` (en Windows, la carpeta de datos locales del
usuario). Borrar ese archivo devuelve la aplicación al tema claro. Si está corrupto, también.
