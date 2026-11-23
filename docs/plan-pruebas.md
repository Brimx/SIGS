# Plan de pruebas

## 1. Estado actual

| Métrica | Valor |
| :-- | :-- |
| framework | xUnit 2.9 |
| pruebas | **443** |
| desglose | 356 `[Fact]` + 87 `[InlineData]`; 425 en la raíz y 18 en `GUI/` (29 archivos: 22 + 7) |
| resultado | 443 correctas, 0 fallidas, 0 omitidas |
| duración | ~5 s (última ejecución local, 2026-11-22) |
| cobertura | reglas de negocio, servicios, GUI, PDF e integración SQLite |

Este archivo es el **único lugar** donde se registra el conteo de pruebas: el resto de la documentación remite aquí. Actualícelo con la salida real de `dotnet test` y la fecha de la medición.

Comando:

```bash
dotnet test Supermercado.Pruebas/Supermercado.Pruebas.csproj
```

En CI se ejecuta en `Release` y se publica el `.trx` como artefacto.

## 2. Estrategia

- **Unitaria pura:** entidades y cálculos sin dependencias (`Dinero`, `Impuesto`, `Nomina`, `Venta`).
- **Unitaria con falsos:** servicios con repositorios en memoria, `RelojFijo`, auditoría falsa y unidad de trabajo falsa.
- **Integración SQLite:** archivos temporales para verificar el modelo EF, índices, TPH y el flujo punta a punta; nunca tocan la base del usuario.
- **PDF:** se genera el documento en memoria y se verifica que sea un PDF válido.
- **Determinista:** no depende de la hora, del orden ni de internet.
- **Regresión:** cada bug relevante agrega una prueba que lo reproduce.

## 3. Cobertura por archivo

| Archivo | Qué verifica |
| :-- | :-- |
| `DineroTests` | Redondeo al peso y porcentajes en puntos básicos |
| `ImpuestosTests` | IVA incluido, retención con base mínima, tarifa inválida |
| `ProductoTests` | Subtotal unitario y pesado, producto inactivo, impuesto obligatorio |
| `VentasTests` | Totales, IVA congelado, cambio, pagos mixtos, cierre y anulación |
| `CajaTests` | Apertura, efectivo esperado, anulación y cierre único |
| `InventarioTests` | FEFO, desempate por lote, vencidos, reparto y stock no negativo |
| `ServicioInventarioTests` | Descuento, reintegro, traslado, merma, lote de otro producto, ingreso inválido |
| `ValidadorTests` | Nombres sin números, documento, NIT, teléfono y correo |
| `ClientesPuntosTests` | Clientes: permisos, alta y validaciones; no comprueba saldo de puntos (no existe en el modelo) |
| `ApisPorDefectoTests` | APIs activas por defecto, interruptores de entorno y de parámetro |
| `ClientesSqliteTests` | Persistencia de `Cliente` y su índice único |
| `EventoOutboxSqliteTests` | Idempotencia de venta por `ClaveIdempotencia` contra SQLite |
| `ReportesPdfTests` | Ticket, desprendible de nómina y reporte de ventas |
| `FlujoPuntaAPuntaSqliteTests` | Venta, stock y persistencia en SQLite |
| `DianYBalanzasTests` | Dígito de verificación de NIT DIAN, parser de balanzas GS1 (prefijo 20-29), conversión de unidades, Habeas Data (consumidor final) y switch régimen tributario |
| `ProductosDianTests` | Dígito de verificación de NIT y tarifas de productos |
| `ServicioVentasTests` | Venta completa, stock insuficiente, permisos, tarjeta, transferencia, doble anulación |
| `ServicioCajaTests` | Caja inexistente, caja inactiva, tolerancia dentro y fuera |
| `NominaTests` | Cálculo, tope de auxilio, año/mes inválido, parámetros negativos |
| `SeguridadTests` | Bloqueo, desbloqueo, permisos, sesión, empleado inactivo, cambio de clave, BCrypt |
| `ServicioAutenticacionTests` | Login, clave incorrecta, bloqueo, PIN, empleado inactivo, cambio de clave |
| `ServicioParametrosTests` | Permiso, auditoría, respaldo atómico, parámetro faltante |
| `ServicioFinanzasTests` | Permiso y auditoría de gastos |
| `ServicioTrmTests` | Respaldo, reintentos, espera con reloj, ausencia total de datos |

Las pruebas de GUI están en `Supermercado.Pruebas/GUI/` (18 casos en 7 archivos). El consolidado fiscal DIAN se cubre desde la GUI de contabilidad, además de `DianYBalanzasTests` y `ProductosDianTests`.

## 4. Casos críticos y resultado esperado

| Caso | Esperado |
| :-- | :-- |
| Venta con stock suficiente | Venta `Completada`, stock descontado, movimiento `Venta` |
| Venta sin stock | `StockInsuficienteException`, sin venta ni movimiento |
| Venta con producto inactivo | `VentaInvalidaException` |
| Pago insuficiente | La venta no se completa |
| Pago en efectivo con vuelto | `Cambio` correcto y `EfectivoNeto` descontado del arqueo |
| Anulación | Stock reintegrado una vez, movimiento `Devolucion`, auditoría |
| Segunda anulación | `VentaInvalidaException`, sin segundo reintegro |
| Turno duplicado | `TurnoInvalidoException` |
| Caja inactiva | `ValidacionException` |
| Arqueo fuera de tolerancia | `DentroDeTolerancia = false` |
| Login con empleado inactivo | `CredencialesInvalidasException` y un intento fallido |
| Cambio de clave pendiente | `DebeCambiarClaveException` |
| Guardar parámetro sin permiso | `AccesoDenegadoException`, sin escritura |
| Respaldo de TRM | Se guarda sin permiso y en una sola unidad de trabajo |
| TRM con API caída | Reintenta y devuelve el último respaldo |
| Reabastecimiento cerrado | `ValidacionException` al atender o cancelar de nuevo |

## 5. Falsos de prueba

| Falso | Reemplaza |
| :-- | :-- |
| `RepositorioEnMemoria<T>` | `IRepositorio<T>`; `Actualizar` valida existencia |
| `StockEnMemoria<T>` | `IStockRepositorio<T>`; rechaza lote duplicado |
| `ProductosEnMemoria`, `VentasEnMemoria`, `TurnosEnMemoria`, etc. | Repositorios específicos |
| `ParametrosFalsos` | `IServicioParametros` con `ParametroFaltanteException` |
| `ParametrosEnMemoria`, `GastosEnMemoria` | Repositorios de configuración y finanzas |
| `UnidadDeTrabajoFalsa` | Cuenta guardados sin persistir |
| `AuditoriaFalsa` | Acumula acciones para asserciones |
| `HasheadorFalso` | Hash determinista `h:secreto` |
| `RelojFijo` | `TimeProvider` con hora y zona UTC |
| `ClienteTrmFalso` | Respuesta o falla controlada |
| `EscenarioInventario` | Arma estante, bodega, movimientos y servicio |

## 6. Pendientes

| Área | Qué falta | Prioridad |
| :-- | :-- | :-- |
| Idempotencia | Prueba de dos solicitudes concurrentes con la misma `ClaveIdempotencia` (el índice único ya existe) | Alta |
| Seguridad | Escalada de privilegios por rol (ver `pendientes-ui.md`) | Alta |
| GUI | Ampliar las pruebas de `GUI/` a más ViewModels y flujos de pantalla | Media |
| Concurrencia | Pruebas con dos sesiones sobre SQLite y bloqueo de turno (ADR-010) | Media |
| Carga | Venta y descuento de stock < 1 s | Baja |
| APIs reales | Clientes de TRM, código de barras y correo con servidor simulado | Baja |

## 7. Cómo agregar una prueba

1. Elija el archivo del área; si es un servicio nuevo, cree `<Servicio>Tests.cs`.
2. Use un `Escenario` local con falsos; no comparta estado entre pruebas.
3. Nombre en español con el patrón `Acción_Condición_Resultado`.
4. Use `Assert.ThrowsAsync<T>` para excepciones del catálogo.
5. Ejecute `dotnet test` y confirme que sigue en verde.
6. Si la prueba descubre un bug, corríjalo en el mismo cambio.

## 8. Criterio de salida

- `dotnet build` sin errores ni advertencias.
- `dotnet test` con 0 fallos.
- CI en verde.
- Toda regla nueva con al menos una prueba.
- Ninguna prueba depende de red, hora real ni base del usuario.
