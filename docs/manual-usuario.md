# Manual de usuario y operación

> **Estado de la interfaz.** La lógica, la persistencia y las reglas de negocio están completas y probadas. La GUI Avalonia es un shell inicial: todavía no expone las pantallas. Este manual describe el flujo operativo previsto y las reglas que los servicios ya aplican; cuando una pantalla no exista, la operación se ejecuta desde la capa de lógica o desde pruebas.

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
2. Agregue líneas por código de barras. Los productos pesados se ingresan en gramos; los unitarios, en unidades.
3. El sistema valida que el producto exista, esté activo, tenga impuesto y haya stock en estante.
4. El descuento es FEFO: primero el lote que vence antes; los vencidos no se venden.
5. Registre uno o varios pagos: efectivo, tarjeta (últimos 4 dígitos) o transferencia (referencia).
6. Si el pago supera el total, el sistema calcula el cambio. La venta se completa cuando los pagos cubren el total.
7. Venta y movimientos de stock se guardan en una sola transacción.

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

## 5. Personal y roles

| Acción | Permiso |
| :-- | :-- |
| Crear, editar o desactivar empleado | `GestionarPersonal` |
| Crear usuario | `GestionarPersonal` |
| Cambiar rol | `GestionarRoles` |
| Editar empleado cambiando su rol | `GestionarPersonal` + `GestionarRoles` |

- El documento del empleado es único.
- Desactivar un empleado no borra su historial: cierra su sesión y conserva ventas, auditoría y nóminas.
- El correo se marca como verificado solo si el verificador responde; por defecto el cliente nulo devuelve «no verificado» y el alta continúa.

## 6. Finanzas

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

## 7. Dashboards

- Cada widget declara el permiso que necesita; los que el usuario no puede ver no aparecen.
- La disposición se guarda por empleado.
- Al agregar, la posición es `máximo + 1`; al reordenar, quedan posiciones `0..n−1` sin duplicados.
- Widgets actuales: ventas del día, reabastecimientos pendientes y TRM del día.

## 8. Parámetros

Los valores se editan desde la aplicación con permiso `GestionarPersonal`. El guardado administrativo exige permiso y deja auditoría; el respaldo interno (TRM) es atómico y no exige permiso.

| Clave | Uso |
| :-- | :-- |
| `seguridad.max_intentos` | Bloqueo de cuenta |
| `inventario.dias_alerta` | Ventana de vencimiento |
| `caja.tolerancia_arqueo` | Diferencia aceptada en pesos |
| `nomina.*` | Salario mínimo, auxilio y deducciones |
| `integraciones.*` | Timeout, reintentos y respaldo de TRM |

Un parámetro ausente **no** tiene valor por defecto silencioso: lanza `ParametroFaltanteException`.

## 9. TRM

- Se consulta a la API con timeout y reintentos.
- La espera entre intentos crece 200 ms, 400 ms, … hasta 2 s.
- Si la API responde, se guarda el valor como respaldo en una sola operación.
- Si todos los intentos fallan, se usa el último respaldo. Si nunca hubo uno, devuelve `null`.
- El cliente real no está incluido: por defecto `ClienteTrmNoConfigurado` lanza `ServicioNoConfiguradoException`.

## 10. Problemas frecuentes

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

## 11. Buenas prácticas de operación

- Cierre el turno antes de cambiar de cajero; no comparta usuarios.
- Registre la merma en el momento; no la acumule para el cierre.
- No edite la base directamente: use los servicios para conservar auditoría.
- Programe respaldos del archivo SQLite (ver [`despliegue.md`](despliegue.md)).
- Revise los parámetros de nómina al inicio de cada año.
