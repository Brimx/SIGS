# Manual de usuario y operación

> **Estado de la interfaz.** La lógica, la persistencia y las reglas de negocio están implementadas y cubiertas por pruebas, con hallazgos de seguridad y concurrencia abiertos (ver [`pendientes-ui.md`](pendientes-ui.md)). La GUI Avalonia es un shell inicial: todavía no expone las pantallas. Este manual describe el flujo operativo previsto y las reglas que los servicios ya aplican; cuando una pantalla no exista, la operación se ejecuta desde la capa de lógica o desde pruebas.

## 1. Primer arranque

1. Instale el .NET SDK 10.
2. Clone el repositorio y ejecute `dotnet run --project Supermercado.GUI`.
3. La base se crea sola en:
   - Windows: `%LOCALAPPDATA%\SIGS\sigs.db`
   - Linux: `~/.local/share/SIGS/sigs.db`
4. La inicialización siembra roles, cargos, impuestos, una caja y los parámetros. Es idempotente: si ya hay datos, no los duplica.
5. El usuario inicial es `admin`. La clave se pasa a `ServicioInicializacion.InicializarAsync` desde la capa que arranque la aplicación; el usuario queda marcado con cambio de clave obligatorio.

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
5. Si el cliente está registrado, asígnelo por documento para acumular o redimir puntos (ver sección 5).
6. Registre uno o varios pagos: efectivo, tarjeta (últimos 4 dígitos), transferencia (referencia) o puntos.
7. Si el pago supera el total, el sistema calcula el cambio. La venta se completa cuando los pagos cubren el total.
8. Si la DIAN está habilitada, se genera el Documento Equivalente POS local (CUDE, XML y QR). **No se transmite a la DIAN.**
9. Venta, puntos, documento y movimientos de stock se guardan en una sola transacción.

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
- Se toma primero el lote que vence antes, incluso si está vencido: el traslado no es venta y sirve para retirar o reorganizar.
- **Hallazgo abierto:** el traslado debería rechazar lotes vencidos; hoy no lo hace (ver [`pendientes-ui.md`](pendientes-ui.md)).
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

## 5. Clientes y puntos

- **Alta:** documento único (5–15 dígitos), nombres, apellidos, teléfono y correo; el backend valida el formato.
- **Habeas Data:** el cliente autoriza o revoca el tratamiento de datos. Sin autorización, el documento DIAN se emite como «Consumidor Final» y no se imprimen sus datos.
- **Acumulación:** se gana `floor(base / puntos.pesos_por_punto)`, donde `base = total − pagos con puntos`. Un valor `0` desactiva la acumulación.
- **Redención:** cada punto vale `puntos.valor_punto_pesos`; no se puede redimir más que el saldo ni más que el total. Un valor `0` desactiva la redención.
- **Anulación:** revierte los puntos ganados y devuelve los redimidos; el saldo nunca queda negativo.
- **Permisos:** gestionar clientes exige `GestionarClientes`; el cajero puede buscar por documento con `Vender`.

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

Los valores se editan desde la aplicación con permiso `GestionarPersonal`. El guardado administrativo exige permiso y deja auditoría; el respaldo interno (TRM) es atómico y no exige permiso.

| Clave | Uso |
| :-- | :-- |
| `seguridad.max_intentos` | Bloqueo de cuenta |
| `inventario.dias_alerta` | Ventana de vencimiento |
| `caja.tolerancia_arqueo` | Diferencia aceptada en pesos |
| `nomina.*` | Salario mínimo, auxilio y deducciones |
| `integraciones.*` | Timeout, reintentos, respaldo de TRM e interruptores `trm_habilitada` / `codigo_barras_habilitado` (`1` activo, `0` apagado) |
| `puntos.pesos_por_punto` | Pesos de compra por punto ganado (`0` desactiva la acumulación) |
| `puntos.valor_punto_pesos` | Valor en pesos de un punto al redimir (`0` desactiva la redención) |
| `tributario.responsable_iva` | `1` extrae IVA; `0` lo desactiva (régimen no responsable) |
| `dian.habilitado` | `1` genera el Documento Equivalente POS; `0` lo apaga |
| `dian.resolucion_*`, `dian.prefijo`, `dian.rango_*`, `dian.clave_tecnica`, `dian.siguiente_consecutivo`, `dian.tipo_ambiente` | Datos de la resolución y numeración DIAN |
| `integraciones.supabase_url` / `_key` / `_habilitado` | Réplica Outbox hacia Supabase/Neon; use solo la clave `anon` |
| `negocio.nombre` / `nit` / `direccion` / `telefono` | Datos del emisor en ticket y XML |

Un parámetro ausente **no** tiene valor por defecto silencioso: lanza `ParametroFaltanteException`. Excepción: los interruptores `integraciones.*_habilitad*` se consideran activos si faltan.

## 10. TRM

- Se consulta a datos.gov.co con timeout y reintentos; el cliente está activo por defecto.
- La espera entre intentos crece 200 ms, 400 ms, … hasta 2 s.
- Si la API responde, se guarda el valor como respaldo en una sola operación.
- Si todos los intentos fallan, se usa el último respaldo. Si nunca hubo uno, devuelve `null`.
- Se puede apagar con `SIGS_TRM_HABILITADA=0` o con `integraciones.trm_habilitada=0`; en ese caso solo se usa el respaldo.

## 11. Documento Equivalente POS DIAN

- Se genera localmente al cerrar la venta si `dian.habilitado = 1`.
- Incluye CUDE (SHA-384), XML UBL 2.1, prefijo, consecutivo y cadena QR.
- El adquirente es el cliente solo si autorizó el tratamiento de datos; si no, «Consumidor Final».
- **No transmite, no firma y no maneja contingencia.** El documento no tiene validez fiscal hasta completar ese circuito.
- La resolución vencida o el rango agotado bloquean la venta con `ValidacionException`; revise `dian.*`.

## 12. Reportes PDF

- Ticket POS de 80 mm con totales, pagos, puntos y datos DIAN si existen.
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
| «La resolución DIAN venció» | `dian.resolucion_vigencia_hasta` superada | Actualizar la resolución o apagar `dian.habilitado` |
| «Se agotó el rango autorizado» | `dian.siguiente_consecutivo` supera `dian.rango_hasta` | Solicitar nueva numeración a la DIAN |
| La base no aparece | Ruta de datos distinta | Revisar `RutasDatos.BaseDatosPorDefecto()` |

## 14. Buenas prácticas de operación

- Cierre el turno antes de cambiar de cajero; no comparta usuarios.
- Registre la merma en el momento; no la acumule para el cierre.
- No edite la base directamente: use los servicios para conservar auditoría.
- Programe respaldos con `VACUUM INTO` o la API de respaldo de SQLite, no con una copia en caliente (ver [`despliegue.md`](despliegue.md)).
- Revise los parámetros de nómina al inicio de cada año y los de la DIAN antes de que venza la resolución.
- Trate los datos del cliente como información personal: no los exporte ni los comparta sin autorización (Ley 1581).
- Use solo la clave `anon` de Supabase en el cliente; nunca `service_role`.
