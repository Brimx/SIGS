# Arquitectura

## Vista de capas

```text
┌─────────────────────────────────────────────┐
│ Supermercado.GUI (Avalonia + MVVM)          │
│  Pantallas, ViewModels, ServiciosGui.cs     │
└───────────────────┬─────────────────────────┘
                    │ clases concretas + interfaces externas
┌───────────────────▼─────────────────────────┐
│ Supermercado.Logica                         │
│  Casos de uso, permisos, auditoría,         │
│  cálculo, widgets, integraciones            │
└───────────────────┬─────────────────────────┘
                    │ repositorios por módulo + GuardarAsync
┌───────────────────▼─────────────────────────┐
│ Supermercado.Datos                          │
│  SigsDbContext, repositorios, EF, SQLite,   │
│  semillas y migración inicial               │
└───────────────────┬─────────────────────────┘
                    │ entidades y enums
┌───────────────────▼─────────────────────────┐
│ Supermercado.Entidades                      │
│  Dominio, excepciones y contratos           │
└─────────────────────────────────────────────┘
```

Reglas de dependencia:

1. **Entidades no referencia ningún proyecto.**
2. **Datos referencia Entidades.** Es el único proyecto que conoce EF Core y SQLite; expone repositorios concretos por módulo y solo conserva las interfaces `IVentaRepositorio` e `IUsuarioRepositorio`.
3. **Lógica referencia Datos y Entidades.** Consume repositorios por módulo y clases concretas de dominio; las interfaces se reservan para límites externos y esos dos contratos de persistencia.
4. **GUI referencia Lógica y Entidades.** No referencia Datos: obtiene todo por `AgregarLogica()`.
5. **Pruebas referencia Lógica y Entidades.** Usa falsos sobre SQLite en memoria para los repositorios migrados; no necesita base de datos externa.

## Composición

`Supermercado.Logica/Composicion.cs`:

```csharp
servicios.AgregarDatos(rutaBaseDatos);

servicios.AddSingleton<SesionActual>();
servicios.AddSingleton<IHasheadorClave, HasheadorBCrypt>();
servicios.AddScoped<EstrategiaSalida, SalidaFefo>();

servicios.AddScoped<ServicioAuditoria>();
servicios.AddScoped<ServicioParametros>();
servicios.AddScoped<ServicioAutenticacion>();
servicios.AddScoped<ServicioInicializacion>();
servicios.AddScoped<ServicioPersonal>();
servicios.AddScoped<ServicioProductos>();
servicios.AddScoped<ServicioInventario>();
servicios.AddScoped<ServicioCaja>();
servicios.AddScoped<ServicioVentas>();
servicios.AddScoped<ServicioFinanzas>();
servicios.AddScoped<ServicioClientes>();
servicios.AddScoped<ServicioDashboard>();
```

Después se registran los límites externos (`ITrmClient`, `IConsultaCodigoBarras`, `IVerificadorCorreo`, `IGeneradorTicket`, `IServicioReportes`, `IServicioRespaldo`, Telegram) y las 10 implementaciones de `IWidget`.

`Supermercado.Datos/Contexto.cs`:

```csharp
servicios.AddSingleton(TimeProvider.System);
servicios.AddDbContext<SigsDbContext>(opciones =>
    opciones.UseSqlite($"Data Source={rutaBaseDatos}"));

servicios.AddScoped<InicializadorBaseDatos>();
servicios.AddScoped<ProductoRepositorio>();
servicios.AddScoped<IVentaRepositorio, VentaRepositorio>();
servicios.AddScoped<TurnoCajaRepositorio>();
servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
// ... el resto de repositorios concretos por archivo de dominio en Datos/*.cs
```

`AgregarDatos` utiliza **SQLite local** en la carpeta de datos del usuario (`%LOCALAPPDATA%/SIGS/sigs.db` en Windows o `~/.local/share/SIGS/sigs.db` en Linux) sin configuración previa. La lógica y los ViewModels permanecen desacoplados del motor de persistencia; cada repositorio hereda el mismo `SigsDbContext` y expone `GuardarAsync`.

## Flujos principales

### Venta

```text
GUI
 └─ ServicioVentas.RegistrarVentaAsync(SolicitudVenta)
     ├─ sesion.Exigir(Vender)
     ├─ idempotencia: ventas.ObtenerPorClaveIdempotenciaAsync
     ├─ turnos.ObtenerAbiertoPorEmpleadoAsync
     ├─ por línea: productos.ObtenerPorCodigoBarrasAsync o ParserCodigoBalanza
     ├─ cliente / autorización Habeas Data
     ├─ Venta.AgregarProducto / AgregarPago / Cerrar
     ├─ ventas.AgregarAsync
     ├─ por detalle: ServicioInventario.DescontarVentaAsync
     │   ├─ SalidaFefo (excluye vencidos, ordena por vencimiento y LoteId)
     │   ├─ PlanDeSalida.Repartir
     │   ├─ StockLote.Disminuir
     │   └─ MovimientoInventario(Tipo = Venta, VentaId)
     └─ ventas.GuardarAsync   ← una sola confirmación del contexto
```

### Anulación con reembolso

```text
ServicioVentas.AnularVentaAsync
 ├─ Exigir(AnularVenta)
 ├─ Venta.Anular(motivo)
 ├─ ServicioInventario.ReintegrarVentaAsync
 │   ├─ movimientos.ObtenerSalidasDeVentaAsync
 │   ├─ devuelve cada cantidad a su lote en estante
 │   └─ registra MovimientoInventario(Tipo = Devolucion)
 ├─ ventas.Actualizar
 ├─ auditoría(VentaAnulada)
 └─ ventas.GuardarAsync
```

El arqueo (`TurnoCaja.CalcularEfectivoEsperado`) suma el efectivo neto de las ventas completadas y **resta** el de las anuladas, para que una venta anulada después del cierre no infle el esperado.

### Arqueo y cierre

```text
ServicioCaja.CerrarTurnoAsync(efectivoContado)
 ├─ Exigir(AbrirCerrarTurno)
 ├─ turno = ObtenerAbiertoPorEmpleadoAsync
 ├─ ventas = ObtenerPorTurnoAsync(turno.Id)
 ├─ esperado = TurnoCaja.CalcularEfectivoEsperado(ventas)
 ├─ turno.Cerrar(ahora, esperado, contado)
 ├─ auditoría(CierreTurno, diferencia)
 ├─ turnos.GuardarAsync
 └─ ResultadoArqueo(DentroDeTolerancia)
```

La tolerancia se lee de `caja.tolerancia_arqueo`.

### Reabastecimiento

```text
ServicioInventario.GenerarSolicitudesAsync
 ├─ Exigir(Reabastecer)
 ├─ alertas.ObtenerBajoMinimoAsync
 ├─ crea Reabastecimiento(Pendiente) si no existe uno pendiente
 └─ guarda

ServicioInventario.AtenderAsync(id)
 ├─ solicitud.Atender(ahora)
 ├─ TrasladarAEstanteAsync(producto, cantidad)
 └─ guarda
```

`Cancelar()` deja el estado `Cancelado` alcanzable y `Atender`/`Cancelar` rechazan solicitudes ya cerradas con `ValidacionException`.

### Nómina

```text
ServicioFinanzas.CalcularAsync(empleadoId, anio, mes)
 ├─ Exigir(GestionarPersonal)
 ├─ valida mes, año y duplicado del periodo
 ├─ lee parámetros de nómina de Parametro
 ├─ Nomina.Calcular(...)
 └─ guarda y devuelve la nómina
```

## Persistencia

### Modelo EF

- **TPH:** `Productos` (unitario/pesado), `Impuestos` (IVA/retención) y `Pago` (efectivo/tarjeta/transferencia) usan una tabla y `Discriminator`.
- **Tablas separadas:** `StockEstante` y `StockBodega` heredan de `StockLote`, que es abstracta y no se mapea.
- **Índices únicos:** código de barras, lote por producto, lote en cada stock, documento de empleado, documento de cliente, usuario, rol, clave de parámetro, widget por empleado y `Venta.ClaveIdempotencia` (permite valores `NULL`).
- **Borrado:** todas las FK son `Restrict`. Las ventas se anulan, no se eliminan.
- **Fechas:** `TimeProvider` inyectado; `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.

### Transacciones

Cada repositorio por módulo hereda de `RepositorioBase`, que expone `GuardarAsync` como `SaveChangesAsync` sobre el `SigsDbContext` compartido. Un caso de uso hace **una sola** llamada al final, normalmente a través del repositorio que cierra la operación (`ventas`, `turnos`, `clientes`, etcétera). Los servicios de dominio modifican entidades y registran movimientos, pero no abren transacciones propias.

Esto garantiza atomicidad dentro de un proceso local sin una capa adicional de persistencia.

## Seguridad

- `SesionActual` es singleton y guarda el `Usuario` autenticado.
- `Exigir(permiso)` falla si no hay sesión, si el empleado está inactivo (y cierra la sesión), si hay cambio de clave pendiente o si falta el permiso.
- El hash y el PIN usan BCrypt con salt (`HasheadorBCrypt`).
- El bloqueo cuenta intentos fallidos, incluido el caso de empleado inactivo con usuario existente.
- La auditoría se escribe en la misma confirmación del contexto que la acción.
- **Hallazgos abiertos:** la escalada de privilegios vía `GestionarPersonal` y la revocación de sesión están documentadas en [`pendientes-ui.md`](pendientes-ui.md). No se consideran cerradas para producción.

## Interfaz Gráfica (`Supermercado.GUI`)

La interfaz de usuario está construida sobre **Avalonia 12.1.4** y **.NET 10** bajo el patrón **MVVM**:
- **Inyección de Dependencias:** `App.axaml.cs` inicializa el contenedor con `servicios.AgregarLogica()`, registrando además los servicios de presentación (`IServicioPreferencias`, `IServicioTextosLegales`, `IImpresorTicket` e `ImpresorTicketPdf`) y los ViewModels para navegación y back-office.
- **Servicios de Presentación Consolidados:** `Supermercado.GUI/ServiciosGui.cs` reúne temas y preferencias (`TemaSigs`, `ServicioPreferencias`), textos legales, impresión térmica ESC/POS y PDF, y la plantilla de tirilla continua de 80mm calibrada.
- **Tokens y Temas:** Modelo en capas (`Primitives.axaml`, `Semantic.axaml`, `Geometry.axaml`, `Typography.axaml` y `SemiBridge.axaml`) con Claro Cálido (*Warm Light* anti-fatiga visual), Oscuro Slate y Alto Contraste. El tema elegido se persiste por máquina con `IServicioPreferencias` y se aplica antes de crear la ventana.
- **Configuración:** overlay compartido en `MainWindow.axaml` (`ConfiguracionView`) accesible desde login y shell; apariencia, textos legales empaquetados y versión. Ver ADR-028.
- **Ergonomía POS:** Atajos físicos `F1`–`F5` / `Escape`, auto-enfoque ininterrumpido en el escáner de código de barras (*keyboard wedge*) e integración automática con balanzas EAN-13 GS1 (`20`–`29`).
- **Detalle completo:** ver [`docs/sistema-diseno-tokens-ui.md`](sistema-diseno-tokens-ui.md).

## Dashboards

`IWidget` define `Id`, `Titulo`, `Grupo`, `Tamano`, `FrecuenciaActualizacion`, `PermisoRequerido` y `ObtenerDatosAsync`. `WidgetBase` concentra la implementación común y `ServicioDashboard` filtra por permiso, guarda la disposición por empleado y mantiene posiciones únicas (`Max + 1` al agregar, `0..n-1` al reordenar). Las 10 tarjetas de métricas residen en `Supermercado.Logica/DashboardWidgets.cs`: `ArqueoTurnoWidget`, `IvaGeneradoWidget`, `LotesPorVencerWidget`, `ReabastecimientosPendientesWidget`, `StockBajoMinimoWidget`, `TrmDelDiaWidget`, `VentasDelDiaWidget`, `ResumenOperativoWidget`, `VentasPorCategoriaWidget` y `MargenPorCategoriaWidget`.

## Integraciones externas y soporte

Los límites externos conservan sus contratos y viven en archivos planos de `Supermercado.Logica/`:

| Contrato | Implementación final registrada | Archivo final |
| :-- | :-- | :-- |
| `ITrmClient` | `ClienteTrmDatosGov` | `ClientesExternos.cs` |
| `IConsultaCodigoBarras` | `ClienteCodigoBarrasOff` | `ClientesExternos.cs` |
| `IVerificadorCorreo` | `VerificadorCorreoNulo` | `ClientesExternos.cs` |
| `IServicioRespaldo` | `ServicioRespaldo` (copia atómica en caliente de SQLite vía `VACUUM INTO`) | `Infraestructura.cs` |
| `IGeneradorTicket` | `GeneradorTicketQuestPdf` | `Reportes.cs` |
| `IServicioReportes` | `ServicioReportesQuestPdf` | `Reportes.cs` |
| `IClienteTelegram` | `ClienteTelegramHttp` | `TelegramClientes.cs` |
| `IClienteGemini` / `IVinculosTelegram` | `ClienteGeminiHttp` / `VinculosTelegram` | `TelegramClientes.cs` / `TelegramSoporte.cs` |

Las implementaciones de reserva se registran primero y las implementaciones reales las sustituyen cuando el límite está configurado. Apagado y URLs: ver ADR-019. `ServicioTrm` aplica timeout, reintentos con espera progresiva (200 ms, 400 ms, … hasta 2 s) y guarda el respaldo en una sola operación atómica.

## Base de datos y evolución

### Persistencia SQLite autónoma

SIGS opera de forma autónoma sobre **SQLite local** con inicialización automática desde `Supermercado.Datos/Semillas.cs` (`InicializadorBaseDatos.CrearYSembrarAsync()`) y migración inicial en `Supermercado.Datos/Migraciones/Inicial.cs`, creando el archivo de base de datos, el esquema completo y la semilla idempotente en el primer arranque sin requerir motores externos ni contenedores.

> La versión empresarial extendida (con proveedor dual PostgreSQL 17 + Npgsql, réplica Outbox hacia Supabase, facturación electrónica DIAN UBL 2.1 / CUDE y fidelización por puntos) se conserva íntegra en la rama `completo`.

## Pruebas

La suite xUnit vive en 22 archivos `.cs` en la raíz de `Supermercado.Pruebas/`, sin subcarpetas, con 425 pruebas en verde. Los dobles compartidos están en `Falsos.cs` y `TelegramFalsos.cs`; los repositorios migrados usan `ContextoPruebasSqlite` con una base SQLite en memoria por escenario, desactivando FK y dos índices únicos propios del modelo para reproducir el comportamiento de los antiguos falsos en memoria. La compilación de la solución termina con 0 advertencias y 0 errores.

## Decisiones relacionadas

- [`decisiones.md`](decisiones.md): ADR-001 a ADR-028.
- [`despliegue.md`](despliegue.md): escenarios de desarrollo y producción.
- [`diccionario-datos.md`](diccionario-datos.md): esquema real y mapeo previsto.

## Chatbot de Telegram

`TelegramWorker` es un `BackgroundService` hospedado durante la vida de Avalonia y recibe actualizaciones por long polling. Crea un scope por actualización; si falta `SIGS_TELEGRAM_BOT_TOKEN`, queda inactivo sin impedir abrir SIGS. `ClienteTelegramHttp` y `ClienteGeminiHttp` son los límites de red; Gemini solo recibe lenguaje natural y puede llamar las seis herramientas declaradas en `TelegramSoporte.cs`, nunca repositorios ni contexto EF.

Los comandos `/stock`, `/ventas`, `/caja` y `/resumen` se resuelven localmente y funcionan aunque Gemini no esté configurado. El acceso requiere chat privado vinculado, identidad de Telegram coincidente y límite de 20 mensajes por minuto. `/quien` informa el usuario y rol vinculados sin exponer secretos ni datos personales; `/desvincular` elimina el vínculo y lo audita.

Configuración emite un código aleatorio de un uso con vencimiento de 10 minutos. Enviar `/start CODIGO` en un chat ya vinculado sustituye el vínculo anterior, consume el código y registra la auditoría con `Origen: Telegram`; un código inválido o vencido conserva el vínculo actual y explica el motivo. `/start` sin código indica con qué usuario y rol está vinculado el chat, o cómo vincularlo. Los vínculos se guardan atómicamente en `%LOCALAPPDATA%/SIGS/telegram-vinculos.json` (o `~/.local/share/SIGS/telegram-vinculos.json`).

Las anulaciones se envían con botones a un Gerente vinculado y vuelven a comprobar el rol al aceptar; cada callback se consume una vez. Reabastecimiento también requiere confirmación. Las acciones usan un contexto de sesión aislado con la identidad vinculada, validan los permisos de Lógica y guardan una sola vez; la auditoría incluye `Origen: Telegram`.

Credenciales y modelo: `SIGS_TELEGRAM_BOT_TOKEN`, `SIGS_GEMINI_API_KEY`, `SIGS_GEMINI_MODEL` (por defecto `gemini-3.8-flash`) y `SIGS_GEMINI_MODEL_RESPALDO` (por defecto `gemini-3.5-flash`). La clave Gemini viaja en el encabezado `x-goog-api-key`; no se registra en los logs. Un `503` del modelo principal se reintenta una sola vez con el modelo de respaldo y se reporta como saturación si ambos fallan. El resumen programado es opcional con `SIGS_TELEGRAM_RESUMEN_HORA=HH:mm`, en hora local, y se envía a chats vinculados de Gerente. No se almacenan credenciales en la base de datos ni en el repositorio.
