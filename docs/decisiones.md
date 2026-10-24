# Decisiones de arquitectura

## ADR-001: Stock en entidades separadas por ubicación

- **Contexto:** RF-10 pide stock separado por producto y lote, y la
  propuesta lista StockEstante y StockBodega como entidades.
- **Decisión:** `StockLote` es abstracta y no se mapea; `StockEstante` y
  `StockBodega` la heredan y cada una tiene su propia tabla.
- **Consecuencias:** el compilador impide mezclar ubicaciones y todo
  traslado deja un `MovimientoInventario`; a cambio hay más tablas.

## ADR-002: Tarifas de impuestos en puntos básicos enteros

- **Contexto:** la propuesta pide tarifas parametrizables en tabla y
  evitar decimales por las limitaciones de SQLite.
- **Decisión:** `Impuesto.PuntosBasicos` es un entero (1900 = 19 %), y
  el cálculo usa solo aritmética entera con redondeo al peso más cercano.
- **Consecuencias:** no hay errores de redondeo ni decimales en la base;
  la interfaz debe convertir a porcentaje para mostrarlo.

## ADR-003: El precio de venta incluye el IVA

- **Contexto:** en Colombia el precio en góndola es el precio final.
- **Decisión:** `Iva.Calcular` extrae el impuesto del precio; no lo suma.
- **Consecuencias:** el cajero ve un solo precio; si el negocio quisiera
  precios sin IVA, solo cambia la clase `Iva`.

## ADR-004: Los servicios de dominio no guardan

- **Contexto:** la propuesta exige que venta y stock se actualicen juntos
  o no se actualicen.
- **Decisión:** `ServicioInventario` modifica entidades y registra
  movimientos, pero el caso de uso que lo orquesta llama a
  `IUnidadDeTrabajo.GuardarAsync` una sola vez.
- **Consecuencias:** hay una única transacción por operación; ningún
  servicio puede guardar a medias.

## ADR-005: Parámetros editables en una tabla

- **Contexto:** intentos de bloqueo, días de alerta, tolerancia de arqueo
  y valores de nómina cambian por negocio o por año.
- **Decisión:** la entidad `Parametro` (clave y valor) guarda esos
  valores; las claves son constantes en `ClavesParametro`. Si falta uno,
  el servicio lanza `ParametroFaltanteException` en vez de inventar un
  valor.
- **Consecuencias:** nada de esto está fijo en el código; la carga inicial
  solo agrega los parámetros que faltan y nunca pisa los ya editados.
  `Configuracion` es la única carpeta de módulo que no está en la
  propuesta.

## ADR-006: El reloj se inyecta y las fechas se guardan en UTC

- **Contexto:** `DateTime.UtcNow` disperso no se puede simular en pruebas
  y mezcla fuentes de hora.
- **Decisión:** los servicios reciben `TimeProvider`; las entidades no
  leen el reloj. `FechaCreacion` la asigna el `DbContext` al guardar.
- **Consecuencias:** las pruebas controlan la fecha; "hoy" se calcula con
  la zona horaria local del equipo.

## ADR-006b: Permisos como banderas dentro de `Rol`

- **Contexto:** la propuesta define roles con permisos verificados en la
  capa de lógica.
- **Decisión:** `Permiso` es un enum de banderas guardado como entero en
  `Rol`; `ISesionActual.Exigir` lo verifica al inicio de cada caso de uso.
- **Consecuencias:** no hace falta una tabla de permisos; la matriz
  inicial de roles es una carga editable, no una regla del código.

## ADR-007: APIs externas tras interfaces, con respaldo

- **Contexto:** TRM, códigos de barras y verificación de correo no deben
  bloquear una operación si fallan.
- **Decisión:** cada API vive tras una interfaz en `Logica/Servicios`; por
  defecto se registran versiones nulas. `ServicioTrm` aplica tiempo
  máximo, reintentos y último valor guardado.
- **Consecuencias:** el sistema funciona sin internet; falta elegir las
  APIs reales y escribir sus clientes.

## ADR-008: SQLite en esta entrega y PostgreSQL local como objetivo

- **Contexto:** el proyecto necesita funcionar sin servidor para el demo,
  pero varias cajas no pueden compartir un archivo SQLite.
- **Decisión:** se mantiene SQLite como base de la entrega y se documenta
  PostgreSQL local en la red del supermercado como fuente de verdad para
  producción, con Supabase como réplica de una sola vía.
- **Consecuencias:** el demo es simple y portable; la operación
  multi-terminal queda explícitamente fuera hasta migrar el proveedor.
  La réplica nunca es bidireccional: un solo nodo escribe.

## ADR-009: La lógica no usa EF Core

- **Contexto:** la propuesta exige inversión de dependencias y pruebas sin
  base de datos.
- **Decisión:** los casos de uso trabajan contra interfaces declaradas en
  `Entidades`; `Lógica` referencia `Datos` solo para la composición
  (`AgregarLogica` llama a `AgregarDatos`). Ningún archivo de `Lógica`
  importa `Microsoft.EntityFrameworkCore`.
- **Consecuencias:** las pruebas usan repositorios en memoria y el
  proveedor se cambia en un solo archivo; a cambio, la composición obliga
  a que `Lógica` conozca el proyecto `Datos`.

## ADR-010: Concurrencia multi-terminal como deuda técnica

- **Contexto:** con varias cajas, el patrón `leer → validar → escribir`
  permite vender dos veces la última unidad, anular dos veces o abrir dos
  turnos sobre la misma caja.
- **Decisión:** no se implementa concurrencia en esta entrega. Se
  documenta la mitigación: transacciones con bloqueo, tokens `xmin`,
  índice único parcial de turno abierto y reintentos acotados ante
  `40001`/`DbUpdateConcurrencyException`.
- **Consecuencias:** una terminal es segura; varias terminales sobre
  SQLite no. Antes de producción multi-terminal se debe migrar a
  PostgreSQL y completar esta deuda.

## ADR-011: Guardado administrativo y respaldo interno de parámetros

- **Contexto:** los parámetros se editan desde la aplicación, pero la TRM
  necesita guardar su último valor aunque no haya sesión ni permiso.
- **Decisión:** `IServicioParametros` separa `GuardarAsync` (exige
  `GestionarPersonal`, audita y guarda) de `GuardarRespaldoAsync`
  (interno, sin permiso ni auditoría, atómico para varias claves).
- **Consecuencias:** un proceso de infraestructura no puede saltarse la
  autorización, y el respaldo de la TRM no ensucia la auditoría de
  cambios administrativos.

## ADR-012: Reintentos de TRM con espera progresiva y reloj inyectado

- **Contexto:** la API de TRM puede fallar por red o timeout; reintentar
  sin espera satura al proveedor y bloquea la operación.
- **Decisión:** `ServicioTrm` reintenta hasta `integraciones.reintentos`
  con espera exponencial de 200 ms a 2 s, usando `TimeProvider`, y guarda
  el valor en una sola operación atómica. Si todo falla, devuelve el
  último respaldo o `null`.
- **Consecuencias:** las pruebas controlan la espera con `RelojFijo`; la
  venta nunca depende de la TRM. El cliente HTTP real queda pendiente.

## ADR-013: Excepciones de dominio en lugar de errores genéricos

- **Contexto:** `InvalidOperationException` no distingue una regla de
  negocio de un bug y la interfaz no puede reaccionar bien.
- **Decisión:** todo error esperado usa el catálogo de
  `Entidades/Excepciones` (`ValidacionException`,
  `StockInsuficienteException`, `TurnoInvalidoException`,
  `ParametroFaltanteException`, etc.). Las integraciones solo capturan
  errores de red, timeout o E/S.
- **Consecuencias:** la GUI puede mapear excepciones a mensajes y las
  pruebas afirman el tipo exacto; un `catch (Exception)` genérico queda
  reservado a la frontera de una integración.

## ADR-014: Abrir turno exige caja existente y activa

- **Contexto:** un turno sobre una caja inexistente o inactiva deja
  ventas sin punto de atención y arqueos imposibles.
- **Decisión:** `ServicioCaja.AbrirTurnoAsync` valida que la caja exista
  y que `Activa` sea verdadero antes de crear el turno; además rechaza un
  segundo turno abierto para el empleado o la caja.
- **Consecuencias:** el error se detecta al abrir, no al cerrar; la regla
  vive en la lógica y no depende de que la GUI oculte el botón.

## ADR-015: Documentación viva y catálogo de código curado

- **Contexto:** el código cambia más rápido que un documento escrito a
  mano y un listado exhaustivo se vuelve obsoleto.
- **Decisión:** `docs/catalogo-codigo.md` se mantiene curado por módulo
  (responsabilidades, constructores, métodos y DI) y
  `scripts/inventario-codigo.sh` genera un anexo exhaustivo bajo demanda.
- **Consecuencias:** el catálogo se revisa en cada PR que cambie una API
  pública; el anexo no se versiona y sirve para detectar cambios de
  superficie.
