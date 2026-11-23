# Actualización de la propuesta

Este documento contiene el texto de reemplazo para los apartados de la propuesta que cambiaron durante la implementación. **No se editó el documento original**; copie cada bloque en la sección correspondiente cuando corresponda.

Criterio: reflejar lo que el repositorio realmente hace hoy, marcar con claridad lo que está pendiente y separar la arquitectura objetivo de la implementada.

---

## §1. Resumen (reemplazo)

Aplicación de escritorio en C# (.NET 10) que integra el núcleo operativo de un supermercado mediano: punto de venta con turnos y retiros de caja, inventario dividido entre estante y bodega con control de lotes y vencimientos, clientes con autorización de tratamiento de datos, gestión de personal con cuatro roles (`Admin`, `Gerente`, `Cajero`, `Bodeguero`) y permisos, cálculo parametrizable de impuestos y nómina, dashboards modulares, reportes PDF, consolidado fiscal local y consumo tolerante a fallos de APIs externas. La persistencia es SQLite local. No hay emisión de Documento Equivalente, firma ni transmisión a la DIAN.

La entrega actual incluye el dominio, la persistencia, la lógica de negocio y la suite de pruebas en verde: 443 casos (356 `[Fact]` y 87 `[InlineData]`, detalle en [`plan-pruebas.md`](plan-pruebas.md)); la interfaz Avalonia ya tiene módulos POS, turnos, inventario, clientes y dashboard, aunque quedan hallazgos de seguridad e integridad documentados en [`pendientes-ui.md`](pendientes-ui.md). La base de datos es SQLite en esta versión. PostgreSQL/Supabase, el patrón Outbox y el programa de puntos son objetivo futuro fuera del alcance de `main`, conservado en la rama `completo`. La concurrencia entre varias cajas se reconoce como deuda técnica.

---

## §3. Justificación (reemplazo)

- **Necesidad real.** Las soluciones de grandes cadenas exigen licencias por terminal, servidores dedicados y contratos de mantenimiento que un negocio mediano no justifica. Las alternativas gratuitas suelen ser POS básicos con ventas e inventario simple.
- **Diferencial.** El proyecto se concentra en lo que casi ninguno resuelve: inventario dual estante/bodega con alertas, lotes con vencimiento y salida FEFO, turnos de caja con arqueo que descuenta anulaciones, permisos verificados en la lógica y cálculo de impuestos adaptado a Colombia.
- **Valor académico.** Aplica de forma integrada programación orientada a objetos, arquitectura en capas, principios SOLID, persistencia con ORM, consumo asíncrono de servicios web, seguridad de aplicaciones, pruebas automatizadas y trabajo colaborativo con control de versiones.
- **Viabilidad.** El núcleo funciona de forma local con una base embebida, sin servidores ni licencias por puesto. No se depende de la nube para vender; la evolución a PostgreSQL queda fuera del alcance de `main`, conservada en la rama `completo`.
- **Estado verificable.** Las reglas críticas —dinero entero, IVA incluido, arqueo, FEFO, bloqueo de cuenta, permisos y auditoría— están cubiertas por pruebas y por un pipeline de integración continua.

---

## §5. Alcance (reemplazo)

### Incluye (implementado o con lógica completa)

- POS y turnos de caja: apertura, venta, pagos mixtos, ventas en espera, retiros, anulación, cierre y arqueo.
- Clientes con documento único y autorización de tratamiento de datos.
- Inventario dual estante/bodega con lotes, vencimientos, FEFO, traslados, mermas, alertas y reabastecimiento; conversión de unidades de compra y códigos de balanza GS1.
- Gestión de empleados, cargos, cuatro roles, autenticación con clave o PIN, bloqueo y auditoría.
- IVA y retenciones parametrizables; gastos, resumen financiero y nómina básica.
- Dashboards modulares con diez widgets filtrados por permiso y disposición por empleado (`DashboardWidgets.cs`).
- Reportes PDF: ticket POS, desprendible de nómina y reporte de ventas.
- Consumo asíncrono de APIs externas con timeout, reintentos y respaldo local.
- Consolidado fiscal local: desglose de IVA por tarifas C/M/J, exportación CSV y PDF de ventas, cálculo del dígito de verificación del NIT y códigos de tarifa en la tirilla POS.
- Persistencia SQLite, semilla idempotente y suite de pruebas en verde (443 casos).

### Estado de la interfaz y los reportes

- La **GUI Avalonia** tiene vistas implementadas para login, POS, caja, inventario, compras/proveedores, personal/roles, contabilidad, dashboard y configuración (`Supermercado.GUI/Views/`); no se limita a un shell.
- El **ticket PDF** ya se genera con QuestPDF (`GeneradorTicketQuestPdf`), igual que el desprendible de nómina y el reporte de ventas.
- La **TRM** y la **consulta de código de barras** se registran con URLs por defecto y pueden apagarse mediante variables de entorno (ADR-019). La **verificación de correo** usa `VerificadorCorreoNulo` salvo que se configure un proveedor HTTP.
- La **DIAN** se reduce en `main` al consolidado fiscal local; no hay emisión, firma ni transmisión de documentos electrónicos.

### Se simula

- **DIAN:** consolidado fiscal y cálculos locales (IVA por tarifa, exportación CSV/PDF, DV de NIT y tirilla POS); no hay emisión de Documento Equivalente, firma digital, habilitación ni contingencia. La generación de CUDE, XML UBL y QR es objetivo futuro fuera del alcance de `main`, conservada en la rama `completo`.
- **Nómina:** cálculo básico, no liquidación completa.
- **Impresión y periféricos:** el ticket se genera como PDF; el lector de códigos de barras se trata como un teclado (los lectores USB funcionan así).

### Queda fuera de esta entrega

- ERP completo, contabilidad general, pagos electrónicos reales y aplicaciones móviles o web.
- Emisión de Documento Equivalente, firma, transmisión y certificación DIAN; en `main` solo se calcula el consolidado fiscal. La generación de CUDE/XML UBL/QR es objetivo futuro en la rama `completo`.
- PostgreSQL/Supabase y sincronización por Outbox: objetivo futuro fuera del alcance de `main`, conservado en la rama `completo`; no hay código de producción para ellos.
- Concurrencia multi-terminal real (bloqueos, tokens de concurrencia y reintentos). Es deuda técnica documentada.

---

## §8. Arquitectura (reemplazo)

### Capas y dependencias

| Proyecto | Responsabilidad | Referencia a |
| :-- | :-- | :-- |
| `Supermercado.Entidades` (Domain) | Entidades, enumeraciones, excepciones y contratos de repositorio | Ninguno |
| `Supermercado.Datos` (Data) | `SigsDbContext`, configuraciones EF Core, repositorios y semilla | Entidades |
| `Supermercado.Logica` (Business) | Casos de uso, permisos, auditoría, cálculo, dashboards y clientes de APIs | Datos, Entidades |
| `Supermercado.GUI` (UI) | Avalonia con MVVM | Lógica, Entidades |
| `Supermercado.Pruebas` | Pruebas unitarias con falsos en memoria | Lógica, Entidades |

**Reglas de dependencia.**

- Nadie referencia la GUI.
- La lógica trabaja contra interfaces de repositorio declaradas en Entidades; **no usa EF Core**. Referencia Datos solo para la composición (`AgregarLogica` llama a `AgregarDatos`), no para las reglas de negocio.
- La GUI no referencia Datos: obtiene todo el grafo por `AgregarLogica()`.
- Cada capa es una biblioteca de clases (`.dll`). La GUI es el único ejecutable.

### Organización por módulos

| Módulo | Entidades principales | Servicios o componentes |
| :-- | :-- | :-- |
| Productos | Producto, Categoría, Proveedor, Lote | `ServicioProductos` |
| Inventario | StockEstante, StockBodega, MovimientoInventario, Reabastecimiento, Merma | `ServicioInventario`, `ServicioMermas`, `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| Ventas | Venta, DetalleVenta, VentaEnEspera, Pago, Caja, TurnoCaja, RetiroCaja | `ServicioVentas`, `ServicioCaja` |
| Clientes | Cliente | `ServicioClientes` |
| DIAN (consolidado) | Venta (adquirente, tarifas de IVA) | `ContabilidadDianViewModel`, `PlantillaTicketPos`, `Validador.CalcularDigitoVerificacionNit` |
| Personal | Empleado, Cargo, Usuario, Rol | `ServicioEmpleados` |
| Finanzas | Impuesto, Gasto, Nómina | `ServicioImpuestos`, `ServicioFinanzas`, `ServicioNomina` |
| Seguridad | RegistroAuditoria, IntentoAcceso, Parámetro | `ServicioAutenticacion`, `ServicioParametros`, `ServicioAuditoria`, `ServicioInicializacion` |
| Dashboards | ConfiguracionWidget | `ServicioDashboard` + widgets |

La sincronización por Outbox y Supabase es objetivo futuro fuera del alcance de `main`, conservada en la rama `completo`.

### Persistencia

- SQLite con EF Core 10. `UseSqlite` se invoca únicamente en `SigsDbContext` (`Supermercado.Datos/Contexto.cs`); no hay otro proveedor configurado en `main`.
- **28 DbSet** declarados en `SigsDbContext` (`Supermercado.Datos/Contexto.cs`), entre ellos `VentaEnEspera` y `RetiroCaja`. Tres jerarquías usan TPH con `Discriminator`: `Productos`, `Impuestos` y `Pago` (`PagoEfectivo`, `PagoTarjeta` y `PagoTransferencia`). `StockEstante` y `StockBodega` son tablas separadas.
- Todas las claves foráneas son `Restrict`: las ventas se anulan, no se eliminan.
- `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.
- Los cambios se guardan a través del contexto; `IUnidadDeTrabajo` no existe en `main` y es objetivo futuro conservado en la rama `completo`.

### Concurrencia

La operación actual no serializa lecturas concurrentes entre cajas. La mitigación objetivo —transacciones con bloqueo, tokens de concurrencia y reintentos acotados— queda fuera del alcance de `main`, conservada en la rama `completo`; no está implementada.

---

## §9. Tecnologías y justificación (reemplazo)

| Componente | Tecnología | Versión | Justificación y estado |
| :-- | :-- | :-- | :-- |
| Plataforma | .NET 10 y C# 14 | 10.0 | LTS, rendimiento y ejecución en Windows/Linux. **Implementado.** |
| Interfaz | Avalonia UI con MVVM | 12.1.4 | XAML multiplataforma, separación presentación/lógica. **Implementada con vistas y ViewModels.** |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 | Comandos y propiedades observables sin código repetitivo. **Implementado.** |
| Base de datos | SQLite | EF Core 10.0.13 | Embebida, sin servidor ni licencias; suficiente para una terminal. **Implementado.** |
| Base objetivo | PostgreSQL local + Supabase (réplica de una vía) | — | Objetivo futuro fuera del alcance de `main`, conservado en la rama `completo`. **No implementado.** |
| ORM | Entity Framework Core | 10.0.13 | Mapeo, LINQ, TPH, índices y `SaveChanges` transaccional. **Implementado.** |
| Gráficas | LiveCharts2 (Avalonia) | — | Dashboards interactivos. **Pendiente de integración.** |
| PDF | QuestPDF | — | Tickets, desprendibles y reportes. **Implementado** (licencia comunitaria, ADR-017). |
| Seguridad | BCrypt.Net-Next | 4.2.0 | Hash con salt para claves y PIN. **Implementado.** |
| APIs | `HttpClient` + `TimeProvider` | — | Llamadas asíncronas, timeout y reintentos. **TRM y código de barras con URLs por defecto y apagables; verificación de correo nula por defecto.** |
| DIAN | Consolidado fiscal local | — | IVA por tarifas C/M/J, exportación CSV (`Declaracion_Fiscal_DIAN_*.csv`) y PDF de ventas, DV de NIT y tirilla POS. **Sin emisión, firma ni transmisión.** |
| Réplica | Outbox + REST | — | Objetivo futuro fuera del alcance de `main`, conservado en la rama `completo`. **No implementado.** |
| Datos de prueba | Bogus | — | Generación de datos para demos. **No incorporado.** |
| Pruebas | xUnit + coverlet | 2.9.3 / 10.1.0 | 443 casos (356 `[Fact]` + 87 `[InlineData]`) de reglas de negocio, GUI e integración SQLite. Suite en verde; detalle en [`plan-pruebas.md`](plan-pruebas.md). |
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
- **Pruebas:** cobertura de impuestos, arqueo, nómina, stock, FEFO, seguridad, parámetros, TRM, clientes, consolidado fiscal, GUI, PDF e integración SQLite; 443 casos (356 `[Fact]` y 87 `[InlineData]`), detalle en [`plan-pruebas.md`](plan-pruebas.md).
- **Calidad:** nombres descriptivos, métodos cortos, una responsabilidad por clase, sin lógica de negocio en la GUI y build sin advertencias.

---

## §11. Seguridad (reemplazo)

- **Roles:** exactamente cuatro — `Admin` (todo), `Gerente` (reportes, finanzas, personal, inventario y anulación), `Cajero` (venta y su turno) y `Bodeguero` (inventario, ajustes, reabastecimiento y productos).
- **Permisos:** `Permiso` es un enum de banderas guardado en `Rol`. `SesionActual.Exigir` verifica el permiso al inicio de cada caso de uso y aplica este orden: empleado activo, cambio de clave pendiente, permiso. `Supervisor` no es un rol: es el mecanismo de autorización `ValidarSupervisorAsync`; `Compras` es el permiso `GestionarCompras`, no un rol.
- **Inicio de sesión:** clave para todos; PIN para el cajero. Ambos usan BCrypt y comparten el bloqueo por intentos. Un intento fallido con empleado inactivo también cuenta.
- **Auditoría:** inicios de sesión y fallos, anulaciones, ajustes de inventario, mermas, cambios de rol y de tarifa, apertura/cierre de turno, alta/desactivación de empleados, gastos, clientes y cambios de parámetros.
- **Datos personales:** `Venta` guarda `DocumentoAdquirente` y `NombreAdquirente`, con constantes de consumidor final para cuando no hay cliente o este no autorizó el tratamiento de datos (Ley 1581).
- **Excepciones del catálogo:** `CredencialesInvalidasException`, `CuentaBloqueadaException`, `DebeCambiarClaveException`, `SesionNoIniciadaException`, `AccesoDenegadoException`.
- **Secretos:** las credenciales reales se leen de configuración local o parámetros. No se versionan claves de TRM ni correo; en `main` no hay parámetros de facturación electrónica ni de fidelización.
- **Integridad:** la auditoría se registra junto con la acción que la origina; `IUnidadDeTrabajo` no existe en `main` (objetivo futuro en la rama `completo`).
- **Límites conocidos:** hay hallazgos de seguridad y concurrencia abiertos (escalada de privilegios, revocación de sesión e idempotencia); se detallan en [`pendientes-ui.md`](pendientes-ui.md) y en la auditoría del proyecto.

La matriz completa rol × permiso y permiso exigido por método está en `docs/matriz-permisos.md`.

---

## §13. Integraciones externas (reemplazo)

- **TRM (implementada):** consulta asíncrona con timeout configurable y reintentos. La espera entre intentos crece 200 ms, 400 ms, … hasta 2 s. Si la API responde, el valor se guarda como respaldo en una sola operación atómica; si falla, se usa el último valor. `ClienteTrmDatosGov` está activo por defecto contra datos.gov.co; se apaga con `SIGS_TRM_HABILITADA=0` o `integraciones.trm_habilitada=0` (ADR-019).
- **Consulta por código de barras (implementada):** `ClienteCodigoBarrasOff` consulta Open Food Facts, activo por defecto y apagable. Si el producto no aparece, se registra manualmente. `ServicioProductos` solo captura errores de red, timeout o E/S; un error de programación no se silencia.
- **Verificación de correo (contrato listo):** `IVerificadorCorreo`; por defecto `VerificadorCorreoNulo` devuelve «no verificado» y el proceso continúa. Se usa solo al registrar empleados o recuperar contraseña, nunca para iniciar sesión. Reacher requiere salida por el puerto 25, que muchos proveedores bloquean; su licencia AGPL-3.0 o comercial debe evaluarse.
- **DIAN (consolidado fiscal local):** `ContabilidadDianViewModel` calcula el consolidado y el desglose de IVA por tarifas C/M/J, exporta CSV (`Declaracion_Fiscal_DIAN_*.csv`) y PDF de ventas; `PlantillaTicketPos.ObtenerCodigoTarifa` mapea tarifas en la tirilla y `Validador.CalcularDigitoVerificacionNit` valida el NIT. No hay emisión de Documento Equivalente, firma ni transmisión.
- **Réplica:** objetivo futuro fuera del alcance de `main`, conservado en la rama `completo`; no hay Outbox ni cliente de sincronización en producción.
- **Reemplazo de clientes:** registre la implementación real **después** de `AgregarLogica()`; el contenedor toma la última inscripción. No es necesario tocar la lógica.

---

## §15. Riesgos y mitigación (reemplazo)

| Riesgo | Impacto | Mitigación |
| :-- | :-- | :-- |
| Alcance demasiado amplio | Entrega incompleta | Priorizar núcleo (POS, inventario, roles), segundo nivel (reportes, impuestos, TRM) y extras (nómina, widgets). |
| Complejidad legal de la DIAN | Bloqueo del proyecto | Mantener el consolidado fiscal local (IVA por tarifa, CSV/PDF, DV de NIT y tirilla POS) y dejar la emisión, firma y habilitación para la rama `completo`. |
| Curva de aprendizaje de Avalonia y MVVM | Retraso de la GUI | Prototipos tempranos del POS y revisión semanal. La lógica ya está desacoplada para no depender de la vista. |
| Fallas de APIs externas | Operación detenida | Timeout, reintentos y respaldo local; las APIs abiertas se pueden apagar y la venta nunca depende de internet. |
| Concurrencia entre varias cajas | Sobrevender stock, duplicar anulaciones o turnos | No habilitar varias cajas sobre SQLite. La evolución multi-terminal (transacciones con bloqueo y tokens de concurrencia) es objetivo futuro conservado en la rama `completo`. Deuda documentada. |
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
| PostgreSQL local + Supabase | **Fuera del alcance de `main`** | Objetivo futuro conservado en la rama `completo`; no hay Outbox ni sincronización en producción. |
| Concurrencia multi-terminal | **Deuda documentada** | Bloqueos, tokens de concurrencia y reintentos: objetivo futuro en la rama `completo`; no implementados en `main`. |
| Alcance de la GUI para el primer demo | **Resuelto en el código** | Las vistas de login, POS y las demás pantallas están implementadas; el demo puede apoyarse en la GUI y la semilla. |
| Integración de QuestPDF | **Resuelto** | Ticket, nómina y reporte de ventas implementados y probados. |
| LiveCharts2 | **Pendiente** | Los widgets se muestran como tarjetas en `DashboardView`; falta integrar la librería de gráficas. |
| Emisión, firma y transmisión DIAN | **Fuera del alcance de `main`** | Solo consolidado fiscal local; la generación de CUDE/XML UBL/QR se conserva en la rama `completo`. |

---

## Slide 5 — Arquitectura (reemplazo)

**Título:** Sistema en 4 capas

**UI — Avalonia + MVVM**
Consume Lógica y usa Entidades para los modelos. No referencia Datos.

**BUSINESS — Lógica**
Casos de uso, permisos, arqueo, IVA, nómina, inventario y dashboards. Trabaja contra interfaces de repositorio; no usa EF Core.

**DATA — EF Core + SQLite**
`SigsDbContext`, configuraciones, repositorios y semilla. Es el único punto que elige el proveedor.

**DOMAIN — Entidades**
Producto, Lote, Venta, Empleado, Turno, contratos y excepciones. Sin dependencias externas.

**Separación y SOLID**
Una responsabilidad por capa (SRP). Entidades no depende de nada. La lógica depende de contratos, no de EF Core (DIP). La GUI no conoce la base de datos. Cada capa es una `.dll`; la GUI es el único ejecutable.

**Nota honesta:** Lógica referencia Datos solo para la composición (`AgregarDatos`); las reglas no llaman a `DbContext`. El modelo declara 28 DbSet en `SigsDbContext` y usa SQLite como único proveedor; no hay `IUnidadDeTrabajo` ni Outbox en `main` (objetivo futuro en la rama `completo`).

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
| xUnit 2.9 + coverlet | Pruebas de reglas, GUI e integración | Implementado |
| CommunityToolkit.Mvvm 8.4 | MVVM en Avalonia | Implementado |
| QuestPDF | Tickets, nómina y reportes PDF | Implementado |
| LiveCharts2 | Gráficas de dashboards | Pendiente |
| Bogus | Datos de demostración | No incorporado |

**Evolución fuera del alcance de `main`:** PostgreSQL local en la LAN como fuente de verdad, Supabase/Neon como réplica de una sola vía y Outbox para la replicación; conservado en la rama `completo`, sin código en producción.
