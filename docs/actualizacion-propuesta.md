# Actualización de la propuesta

Este documento contiene el texto de reemplazo para los apartados de la propuesta que cambiaron durante la implementación. **No se editó el documento original**; copie cada bloque en la sección correspondiente cuando corresponda.

Criterio: reflejar lo que el repositorio realmente hace hoy, marcar con claridad lo que está pendiente y separar la arquitectura objetivo de la implementada.

---

## §1. Resumen (reemplazo)

Aplicación de escritorio en C# (.NET 10) que integra el núcleo operativo de un supermercado mediano: punto de venta con turnos de caja, inventario dividido entre estante y bodega con control de lotes y vencimientos, gestión de personal con roles y permisos, cálculo parametrizable de impuestos y nómina, dashboards modulares y consumo tolerante a fallos de APIs externas. La aplicación no emite facturación electrónica ni se conecta a la DIAN.

La entrega actual incluye el dominio, la persistencia, la lógica de negocio y **86 pruebas unitarias en verde**; la interfaz Avalonia es un shell inicial y las pantallas están en construcción. La base de datos es SQLite en esta versión. La arquitectura objetivo para producción —PostgreSQL local como fuente de verdad y Supabase como réplica de una sola vía— está documentada, pero no implementada. La concurrencia entre varias cajas se reconoce como deuda técnica con una mitigación planificada (`xmin`, transacciones con bloqueo y reintentos acotados).

---

## §3. Justificación (reemplazo)

- **Necesidad real.** Las soluciones de grandes cadenas exigen licencias por terminal, servidores dedicados y contratos de mantenimiento que un negocio mediano no justifica. Las alternativas gratuitas suelen ser POS básicos con ventas e inventario simple.
- **Diferencial.** El proyecto se concentra en lo que casi ninguno resuelve: inventario dual estante/bodega con alertas, lotes con vencimiento y salida FEFO, turnos de caja con arqueo que descuenta anulaciones, permisos verificados en la lógica y cálculo de impuestos adaptado a Colombia.
- **Valor académico.** Aplica de forma integrada programación orientada a objetos, arquitectura en capas, principios SOLID, persistencia con ORM, consumo asíncrono de servicios web, seguridad de aplicaciones, pruebas automatizadas y trabajo colaborativo con control de versiones.
- **Viabilidad.** El núcleo funciona de forma local con una base embebida, sin servidores ni licencias por puesto. Para varias cajas se documenta una evolución a PostgreSQL local en la red del supermercado; no se depende de la nube para vender.
- **Estado verificable.** Las reglas críticas —dinero entero, IVA incluido, arqueo, FEFO, bloqueo de cuenta, permisos y auditoría— están cubiertas por pruebas y por un pipeline de integración continua.

---

## §5. Alcance (reemplazo)

### Incluye (implementado o con lógica completa)

- POS y turnos de caja: apertura, venta, pagos mixtos, anulación, cierre y arqueo.
- Inventario dual estante/bodega con lotes, vencimientos, FEFO, traslados, mermas, alertas y reabastecimiento.
- Gestión de empleados, cargos, roles, autenticación con clave o PIN, bloqueo y auditoría.
- IVA y retenciones parametrizables; gastos, resumen financiero y nómina básica.
- Dashboards modulares con widgets filtrados por permiso y disposición por empleado.
- Consumo asíncrono de APIs externas con timeout, reintentos y respaldo local.
- Persistencia SQLite, semilla idempotente y 86 pruebas unitarias.

### Estado de la interfaz y los reportes

- La **GUI Avalonia** es un shell: la lógica está lista, pero las pantallas de login, POS, inventario y caja están pendientes.
- El **ticket PDF** está tras `IGeneradorTicket`; la implementación actual es nula. QuestPDF se integrará en la siguiente iteración.
- La **consulta real** de TRM, código de barras y verificación de correo no está incluida: existen contratos y clientes nulos para no bloquear la operación.

### Se simula

- **Impuestos:** IVA y retenciones parametrizables, sin conexión con la DIAN y sin facturación electrónica.
- **Nómina:** cálculo básico, no liquidación completa.
- **Impresión y periféricos:** el ticket se genera como PDF; el lector de códigos de barras se trata como un teclado (los lectores USB funcionan así).

### Queda fuera de esta entrega

- ERP completo, contabilidad general, pagos electrónicos reales y aplicaciones móviles o web.
- PostgreSQL/Supabase en ejecución y sincronización bidireccional. La arquitectura objetivo es PostgreSQL local con réplica de una sola vía hacia Supabase; se documenta, no se implementa.
- Concurrencia multi-terminal real (`xmin`, bloqueos y reintentos). Es deuda técnica documentada.
- Pantallas completas de la GUI y reportes PDF.

---

## §8. Arquitectura (reemplazo)

### Capas y dependencias

| Proyecto | Responsabilidad | Referencia a |
| :-- | :-- | :-- |
| `Supermercado.Entidades` (Domain) | Entidades, enumeraciones, excepciones y contratos de repositorio | Ninguno |
| `Supermercado.Datos` (Data) | `SigsDbContext`, configuraciones EF Core, repositorios, unidad de trabajo y semilla | Entidades |
| `Supermercado.Logica` (Business) | Casos de uso, permisos, auditoría, cálculo, dashboards y clientes de APIs | Datos, Entidades |
| `Supermercado.GUI` (UI) | Avalonia con MVVM | Lógica, Entidades |
| `Supermercado.Pruebas` | Pruebas unitarias con falsos en memoria | Lógica, Entidades |

**Reglas de dependencia.**

- Nadie referencia la GUI.
- La lógica trabaja contra interfaces de repositorio declaradas en Entidades; **no usa EF Core**. Referencia Datos solo para la composición (`AgregarLogica` llama a `AgregarDatos`), no para las reglas de negocio.
- La GUI no referencia Datos: obtiene todo el grafo por `AgregarLogica()`.
- Cada capa es una biblioteca de clases (`.dll`). La GUI es el único ejecutable.

### Organización por módulos

| Módulo | Entidades principales | Servicios |
| :-- | :-- | :-- |
| Productos | Producto, Categoría, Proveedor, Lote | `ServicioProductos` |
| Inventario | StockEstante, StockBodega, MovimientoInventario, Reabastecimiento, Merma | `ServicioInventario`, `ServicioMermas`, `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| Ventas | Venta, DetalleVenta, Pago, Caja, TurnoCaja | `ServicioVentas`, `ServicioCaja` |
| Personal | Empleado, Cargo, Usuario, Rol | `ServicioEmpleados` |
| Finanzas | Impuesto, Gasto, Nómina | `ServicioImpuestos`, `ServicioFinanzas`, `ServicioNomina` |
| Seguridad | RegistroAuditoria, IntentoAcceso, Parámetro | `ServicioAutenticacion`, `ServicioParametros`, `ServicioAuditoria`, `ServicioInicializacion` |
| Dashboards | ConfiguracionWidget | `ServicioDashboard` + widgets |

### Persistencia

- SQLite con EF Core 10. El proveedor se elige en un solo punto (`UseSqlite` en `ExtensionesDatos`); cambiarlo a PostgreSQL no obliga a tocar la lógica.
- **25 tablas.** Tres jerarquías usan TPH con `Discriminator`: `Productos`, `Impuestos` y `Pago`. `StockEstante` y `StockBodega` son tablas separadas.
- Todas las claves foráneas son `Restrict`: las ventas se anulan, no se eliminan.
- `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.
- Un caso de uso hace **una sola** llamada a `IUnidadDeTrabajo.GuardarAsync`; venta y movimientos de stock se confirman juntos o no se confirman.

### Concurrencia

La unidad de trabajo garantiza atomicidad dentro de un proceso, pero no serializa lecturas concurrentes entre cajas. La mitigación objetivo —transacciones con bloqueo, `UseXminAsConcurrencyToken` y reintentos acotados— está documentada en `docs/arquitectura.md` y `docs/decisiones.md`; no está implementada.

---

## §9. Tecnologías y justificación (reemplazo)

| Componente | Tecnología | Versión | Justificación y estado |
| :-- | :-- | :-- | :-- |
| Plataforma | .NET 10 y C# 14 | 10.0 | LTS, rendimiento y ejecución en Windows/Linux. **Implementado.** |
| Interfaz | Avalonia UI con MVVM | 12.1.4 | XAML multiplataforma, separación presentación/lógica. **Shell inicial.** |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 | Comandos y propiedades observables sin código repetitivo. **Implementado en el shell.** |
| Base de datos | SQLite | EF Core 10.0.13 | Embebida, sin servidor ni licencias; suficiente para una terminal. **Implementado.** |
| Base objetivo | PostgreSQL local + Supabase (réplica de una vía) | — | Varias cajas en LAN sin depender de internet. **Documentado, no implementado.** |
| ORM | Entity Framework Core | 10.0.13 | Mapeo, LINQ, TPH, índices y `SaveChanges` transaccional. **Implementado.** |
| Gráficas | LiveCharts2 (Avalonia) | — | Dashboards interactivos. **Pendiente de integración.** |
| PDF | QuestPDF | — | Tickets, desprendibles y reportes. **Contrato listo; implementación nula.** |
| Seguridad | BCrypt.Net-Next | 4.2.0 | Hash con salt para claves y PIN. **Implementado.** |
| APIs | `HttpClient` + `TimeProvider` | — | Llamadas asíncronas, timeout y reintentos. **Contrato implementado; clientes reales pendientes.** |
| Datos de prueba | Bogus | — | Generación de datos para demos. **No incorporado.** |
| Pruebas | xUnit + coverlet | 2.9.3 / 10.1.0 | Pruebas de reglas de negocio. **86 pruebas en verde.** |
| CI | GitHub Actions | — | Build y test en cada push a `main`. **Implementado.** |
| Versionamiento | Git y GitHub | — | Ramas por módulo y revisión de cambios. **Implementado.** |

---

## §10. Especificaciones técnicas esperadas (reemplazo)

- **Entorno de desarrollo:** .NET SDK 10, IDE compatible (Rider o Visual Studio) y Git.
- **Entorno de ejecución:** Windows 10+ o Linux x64, con opción de publicación autocontenida (sin instalar el runtime aparte).
- **Base de datos:** SQLite en la carpeta de datos del usuario.
  - Windows: `%LOCALAPPDATA%\SIGS\sigs.db`
  - Linux: `~/.local/share/SIGS/sigs.db`
  - `EnsureCreated` si no hay migraciones; `MigrateAsync` cuando existan. La semilla es idempotente y solo agrega lo faltante.
- **Usuario inicial:** `admin`, con clave temporal y cambio obligatorio en el primer inicio.
- **Dinero:** enteros `long` en pesos colombianos; porcentajes en puntos básicos enteros (`1900 = 19 %`). Nunca `double` ni `decimal` en la base.
- **Contraseñas y PIN:** BCrypt con salt; bloqueo tras un número configurable de intentos; el PIN exige mínimo 4 dígitos.
- **Autorización:** permisos verificados en la capa de lógica; la interfaz solo oculta lo que el usuario no puede usar, pero no es la defensa.
- **Concurrencia:** una transacción por caso de uso. La concurrencia multi-terminal queda como deuda técnica documentada, no resuelta.
- **Integraciones:** timeout, reintentos con espera progresiva (200 ms → 2 s) y respaldo local; una falla de API no bloquea la venta.
- **Pruebas:** cobertura de impuestos, arqueo, nómina, stock, FEFO, seguridad, parámetros y TRM; 86 pruebas en verde.
- **Calidad:** nombres descriptivos, métodos cortos, una responsabilidad por clase, sin lógica de negocio en la GUI y build sin advertencias.

---

## §11. Seguridad (reemplazo)

- **Roles iniciales:** Admin (todo), Gerente (reportes, finanzas, personal, inventario y anulación), Cajero (venta y su turno), Bodeguero (inventario, ajustes, reabastecimiento y productos).
- **Permisos:** `Permiso` es un enum de banderas guardado en `Rol`. `ISesionActual.Exigir` verifica el permiso al inicio de cada caso de uso y aplica este orden: empleado activo, cambio de clave pendiente, permiso.
- **Inicio de sesión:** clave para todos; PIN para el cajero. Ambos usan BCrypt y comparten el bloqueo por intentos. Un intento fallido con empleado inactivo también cuenta.
- **Auditoría:** inicios de sesión y fallos, anulaciones, ajustes de inventario, mermas, cambios de rol y de tarifa, apertura/cierre de turno, alta/desactivación de empleados, gastos y cambios de parámetros.
- **Excepciones del catálogo:** `CredencialesInvalidasException`, `CuentaBloqueadaException`, `DebeCambiarClaveException`, `SesionNoIniciadaException`, `AccesoDenegadoException`.
- **Secretos:** las credenciales de servicios externos se leen de configuración local; no se incluyen en el repositorio.
- **Integridad:** la auditoría se escribe en la misma unidad de trabajo que la acción; si la acción falla, no queda un registro huérfano.

La matriz completa rol × permiso y permiso exigido por método está en `docs/matriz-permisos.md`.

---

## §13. Integraciones externas (reemplazo)

- **TRM (implementada la lógica):** consulta asíncrona con timeout configurable y reintentos. La espera entre intentos crece 200 ms, 400 ms, … hasta 2 s. Si la API responde, el valor se guarda como respaldo en una sola operación atómica; si falla, se usa el último valor. El cliente HTTP real no está incluido: por defecto `ClienteTrmNoConfigurado` lanza `ServicioNoConfiguradoException`.
- **Consulta por código de barras (contrato listo):** `IConsultaCodigoBarras` permite reemplazar la implementación nula por un servicio abierto. Si el producto no aparece, se registra manualmente. `ServicioProductos` solo captura errores de red, timeout o E/S; un error de programación no se silencia.
- **Verificación de correo (contrato listo):** `IVerificadorCorreo`; por defecto `VerificadorCorreoNulo` devuelve «no verificado» y el proceso continúa. Se usa solo al registrar empleados o recuperar contraseña, nunca para iniciar sesión. Reacher requiere salida por el puerto 25, que muchos proveedores bloquean; su licencia AGPL-3.0 o comercial debe evaluarse.
- **Reemplazo de clientes:** registre la implementación real **después** de `AgregarLogica()`; el contenedor toma la última inscripción. No es necesario tocar la lógica.

---

## §15. Riesgos y mitigación (reemplazo)

| Riesgo | Impacto | Mitigación |
| :-- | :-- | :-- |
| Alcance demasiado amplio | Entrega incompleta | Priorizar núcleo (POS, inventario, roles), segundo nivel (reportes, impuestos, TRM) y extras (nómina, widgets). |
| Complejidad legal de la DIAN | Bloqueo del proyecto | Simular los cálculos; sin facturación electrónica ni conexión real. Tarifas parametrizables y revisadas cada año. |
| Curva de aprendizaje de Avalonia y MVVM | Retraso de la GUI | Prototipos tempranos del POS y revisión semanal. La lógica ya está desacoplada para no depender de la vista. |
| Fallas de APIs externas | Operación detenida | Timeout, reintentos y respaldo local; clientes nulos por defecto. La venta nunca depende de internet. |
| Concurrencia entre varias cajas | Sobrevender stock, duplicar anulaciones o turnos | No habilitar varias cajas sobre SQLite. Antes de producción multi-terminal: PostgreSQL, transacciones con bloqueo y `xmin`. Deuda documentada. |
| Pérdida del archivo SQLite | Pérdida de historial | Respaldo programado y restauración probada; no sincronizar el archivo con la aplicación abierta. |
| Conflictos en el equipo | Retrabajo | Ramas por módulo, commits convencionales y revisión de cambios. |
| Errores de redondeo en dinero | Descuadres | Enteros en pesos, puntos básicos y pruebas unitarias de cálculo. |
| Dependencia de una sola persona | Bloqueo | Documentación de arquitectura, catálogo de código, convenciones y ADR. |

---

## §17. Decisiones por confirmar (reemplazo)

| Tema | Estado | Observación |
| :-- | :-- | :-- |
| .NET Framework 4.8 vs. .NET 10 | **Resuelto en el código** | El repositorio usa .NET 10 y compila sin advertencias. Falta confirmar que la rúbrica lo acepte. |
| Framework de interfaz | **Resuelto en el código** | Avalonia 12.1.4 con MVVM. Falta confirmar que no haya restricción docente. |
| Verificación de correo con Reacher | **Pendiente** | Se deja `VerificadorCorreoNulo`; el alta continúa sin verificar. |
| PostgreSQL local + Supabase | **Decidido, no implementado** | Réplica de una sola vía; nunca bidireccional. |
| Concurrencia multi-terminal | **Deuda documentada** | `xmin`, bloqueos y reintentos planificados; no se implementan en esta entrega. |
| Alcance de la GUI para el primer demo | **Pendiente** | Definir si el demo se apoya en pruebas/servicios o si se priorizan las pantallas de login y POS. |
| Integración de QuestPDF y LiveCharts2 | **Pendiente** | Los contratos existen; faltan las implementaciones y su prueba. |

---

## Slide 5 — Arquitectura (reemplazo)

**Título:** Sistema en 4 capas

**UI — Avalonia + MVVM**
Consume Lógica y usa Entidades para los modelos. No referencia Datos.

**BUSINESS — Lógica**
Casos de uso, permisos, arqueo, IVA, nómina, inventario y dashboards. Trabaja contra interfaces de repositorio; no usa EF Core.

**DATA — EF Core + SQLite**
`SigsDbContext`, configuraciones, repositorios, unidad de trabajo y semilla. Es el único punto que elige el proveedor.

**DOMAIN — Entidades**
Producto, Lote, Venta, Empleado, Turno, contratos y excepciones. Sin dependencias externas.

**Separación y SOLID**
Una responsabilidad por capa (SRP). Entidades no depende de nada. La lógica depende de contratos, no de EF Core (DIP). La GUI no conoce la base de datos. Cada capa es una `.dll`; la GUI es el único ejecutable.

**Nota honesta:** Lógica referencia Datos solo para la composición (`AgregarDatos`); las reglas no llaman a `DbContext`. El modelo tiene 25 tablas y una transacción por caso de uso.

---

## Slide 6 — Stack tecnológico (reemplazo)

**Título:** Por qué estas herramientas

**.NET 10 y C# 14**
LTS, soporte a largo plazo y buen rendimiento. **Implementado.**

**Avalonia UI 12.1 + MVVM**
XAML multiplataforma y separación presentación/lógica. **Shell inicial; pantallas pendientes.**

**SQLite + EF Core 10**
Base embebida sin servidor para una terminal. TPH, índices únicos y `SaveChanges` transaccional. **Implementado.**

**Bibliotecas complementarias**

| Biblioteca | Uso | Estado |
| :-- | :-- | :-- |
| BCrypt.Net-Next 4.2 | Hash de claves y PIN | Implementado |
| xUnit 2.9 + coverlet | 86 pruebas de reglas | Implementado |
| CommunityToolkit.Mvvm 8.4 | MVVM en Avalonia | Implementado en el shell |
| QuestPDF | Tickets y reportes PDF | Pendiente |
| LiveCharts2 | Gráficas de dashboards | Pendiente |
| Bogus | Datos de demostración | No incorporado |

**Evolución de producción:** PostgreSQL local en la LAN como fuente de verdad y Supabase como réplica de una sola vía. **Documentado, no implementado.**
