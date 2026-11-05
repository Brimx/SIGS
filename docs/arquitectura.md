# Arquitectura

## Vista de capas

```text
┌─────────────────────────────────────────────┐
│ Supermercado.GUI (Avalonia + MVVM)          │
│  Pantallas, ViewModels, comandos            │
└───────────────────┬─────────────────────────┘
                    │ interfaces de lógica
┌───────────────────▼─────────────────────────┐
│ Supermercado.Logica                         │
│  Casos de uso, permisos, auditoría,         │
│  cálculo, widgets, clientes externos        │
└───────────────────┬─────────────────────────┘
                    │ contratos de Entidades
┌───────────────────▼─────────────────────────┐
│ Supermercado.Datos                          │
│  SigsDbContext, configuraciones EF,         │
│  repositorios, unidad de trabajo, semilla   │
└───────────────────┬─────────────────────────┘
                    │ entidades y enums
┌───────────────────▼─────────────────────────┐
│ Supermercado.Entidades                      │
│  Dominio, contratos, excepciones            │
└─────────────────────────────────────────────┘
```

Reglas de dependencia:

1. **Entidades no referencia ningún proyecto.**
2. **Datos referencia Entidades.** Es el único proyecto que conoce EF Core y SQLite.
3. **Lógica referencia Datos y Entidades.** La lógica trabaja contra interfaces de repositorio, no contra `SigsDbContext`.
4. **GUI referencia Lógica y Entidades.** No referencia Datos: obtiene todo por `AgregarLogica()`.
5. **Pruebas referencia Lógica y Entidades.** Usa falsos en memoria; no necesita base de datos.

## Composición

`Supermercado.Logica/ExtensionesLogica.cs`:

```csharp
servicios.AgregarDatos(rutaBaseDatos, cadenaConexionPostgres); // DbContext + repositorios
servicios.AddSingleton<ISesionActual, SesionActual>();
servicios.AddSingleton<IHasheadorClave, HasheadorBCrypt>();
servicios.AddScoped<IEstrategiaSalida, SalidaFefo>();

// 8 servicios de dominio consolidados con reenvío de contratos especializados:
servicios.AddScoped<ServicioAuditoria>();
servicios.AddScoped<IServicioAuditoria>(sp => sp.GetRequiredService<ServicioAuditoria>());
servicios.AddScoped<IServicioConsultaAuditoria>(sp => sp.GetRequiredService<ServicioAuditoria>());

servicios.AddScoped<IServicioParametros, ServicioParametros>();
servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
servicios.AddScoped<IServicioInicializacion, ServicioInicializacion>();

servicios.AddScoped<ServicioPersonal>();
servicios.AddScoped<IServicioPersonal>(sp => sp.GetRequiredService<ServicioPersonal>());
servicios.AddScoped<IServicioEmpleados>(sp => sp.GetRequiredService<ServicioPersonal>());
servicios.AddScoped<IServicioEstructura>(sp => sp.GetRequiredService<ServicioPersonal>());
servicios.AddScoped<IServicioUsuarios>(sp => sp.GetRequiredService<ServicioPersonal>());

servicios.AddScoped<ServicioProductos>();
servicios.AddScoped<IServicioProductos>(sp => sp.GetRequiredService<ServicioProductos>());
servicios.AddScoped<IServicioBusquedaProductos>(sp => sp.GetRequiredService<ServicioProductos>());
servicios.AddScoped<IServicioMantenimientoProductos>(sp => sp.GetRequiredService<ServicioProductos>());
servicios.AddScoped<IServicioCatalogos>(sp => sp.GetRequiredService<ServicioProductos>());

servicios.AddScoped<ServicioInventario>();
servicios.AddScoped<IServicioInventario>(sp => sp.GetRequiredService<ServicioInventario>());
servicios.AddScoped<IServicioConsultaInventario>(sp => sp.GetRequiredService<ServicioInventario>());
servicios.AddScoped<IServicioMermas>(sp => sp.GetRequiredService<ServicioInventario>());
servicios.AddScoped<IServicioAlertasInventario>(sp => sp.GetRequiredService<ServicioInventario>());
servicios.AddScoped<IServicioReabastecimiento>(sp => sp.GetRequiredService<ServicioInventario>());

servicios.AddScoped<IServicioCaja, ServicioCaja>();
servicios.AddScoped<ServicioVentas>();
servicios.AddScoped<IServicioVentas>(sp => sp.GetRequiredService<ServicioVentas>());
servicios.AddScoped<IServicioVentasEnEspera>(sp => sp.GetRequiredService<ServicioVentas>());

servicios.AddScoped<ServicioFinanzas>();
servicios.AddScoped<IServicioFinanzas>(sp => sp.GetRequiredService<ServicioFinanzas>());
servicios.AddScoped<IServicioImpuestos>(sp => sp.GetRequiredService<ServicioFinanzas>());
servicios.AddScoped<IServicioNomina>(sp => sp.GetRequiredService<ServicioFinanzas>());

servicios.AddScoped<IServicioClientes, ServicioClientes>();
servicios.AddScoped<IServicioDashboard, ServicioDashboard>();
// ... clientes externos, reportes QuestPDF, logs y widgets en Widgets.cs
```

`Supermercado.Datos/ExtensionesDatos.cs`:

```csharp
servicios.AddSingleton(TimeProvider.System);
servicios.AddDbContext<SigsDbContext>(opciones =>
{
    var postgresEnv = Environment.GetEnvironmentVariable("SIGS_POSTGRES_CONNECTION");
    var connectionPostgres = !string.IsNullOrWhiteSpace(cadenaConexionPostgres)
        ? cadenaConexionPostgres
        : postgresEnv;

    if (!string.IsNullOrWhiteSpace(connectionPostgres))
    {
        opciones.UseNpgsql(connectionPostgres);
    }
    else
    {
        var carpeta = Path.GetDirectoryName(rutaBaseDatos);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        opciones.UseSqlite($"Data Source={rutaBaseDatos}");
    }
});

// Repositorios genéricos y específicos agrupados por dominio
servicios.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
servicios.AddScoped(typeof(IStockRepositorio<>), typeof(StockRepositorio<>));
servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
servicios.AddScoped<IInicializadorBaseDatos, InicializadorBaseDatos>();

servicios.AddScoped<IProductoRepositorio, ProductoRepositorio>();
servicios.AddScoped<ILoteRepositorio, LoteRepositorio>();
servicios.AddScoped<IMovimientoRepositorio, MovimientoRepositorio>();
servicios.AddScoped<IReabastecimientoRepositorio, ReabastecimientoRepositorio>();
servicios.AddScoped<IVentaRepositorio, VentaRepositorio>();
servicios.AddScoped<IClienteRepositorio, ClienteRepositorio>();
servicios.AddScoped<IEventoOutboxRepositorio, EventoOutboxRepositorio>();
servicios.AddScoped<ITurnoCajaRepositorio, TurnoCajaRepositorio>();
servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
servicios.AddScoped<IRolRepositorio, RolRepositorio>();
servicios.AddScoped<IParametroRepositorio, ParametroRepositorio>();
servicios.AddScoped<IGastoRepositorio, GastoRepositorio>();
servicios.AddScoped<IConfiguracionWidgetRepositorio, ConfiguracionWidgetRepositorio>();
```

`ExtensionesDatos` opera como un **proveedor dual**: si la variable de entorno `SIGS_POSTGRES_CONNECTION` (o configuración equivalente) está definida, conecta a PostgreSQL mediante Npgsql. En su defecto, utiliza SQLite local en la carpeta de datos de usuario sin configuración previa. La lógica y los ViewModels permanecen desacoplados de esta selección.

## Flujos principales

### Venta

```text
GUI
 └─ ServicioVentas.RegistrarVentaAsync(SolicitudVenta)
     ├─ sesion.Exigir(Vender)
     ├─ idempotencia: ventas.ObtenerPorClaveIdempotenciaAsync
     ├─ turnos.ObtenerAbiertoPorEmpleadoAsync
     ├─ por línea: productos.ObtenerPorCodigoBarrasAsync o ParserCodigoBalanza
     ├─ cliente / puntos / autorización Habeas Data
     ├─ Venta.AgregarProducto / AgregarPago / Cerrar
     ├─ ServicioDian.EmitirDocumentoPosAsync (CUDE, UBL, QR local)
     ├─ AplicarPuntos (solo si la venta ya es válida)
     ├─ ventas.AgregarAsync
     ├─ por detalle: ServicioInventario.DescontarVentaAsync
     │   ├─ SalidaFefo (excluye vencidos, ordena por vencimiento y LoteId)
     │   ├─ PlanDeSalida.Repartir
     │   ├─ StockLote.Disminuir
     │   └─ MovimientoInventario(Tipo = Venta, VentaId)
     ├─ EventoOutbox.Crear("VentaCompletada", …)
     └─ trabajo.GuardarAsync   ← una sola transacción
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
 └─ trabajo.GuardarAsync
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
 ├─ trabajo.GuardarAsync
 └─ ResultadoArqueo(DentroDeTolerancia)
```

La tolerancia se lee de `caja.tolerancia_arqueo`.

### Reabastecimiento

```text
ServicioInventario.GenerarSolicitudesAsync (vía IServicioReabastecimiento)
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
ServicioFinanzas.CalcularAsync(empleadoId, anio, mes) (vía IServicioNomina)
 ├─ Exigir(GestionarPersonal)
 ├─ valida mes, año y duplicado del periodo
 ├─ lee parámetros de nómina de Parametro
 ├─ Nomina.Calcular(...)
 └─ guarda y devuelve la nómina
```

## Persistencia

### Modelo EF

- **TPH:** `Productos` (unitario/pesado), `Impuestos` (IVA/retención) y `Pago` (efectivo/tarjeta/transferencia/puntos) usan una tabla y `Discriminator`.
- **Tablas separadas:** `StockEstante` y `StockBodega` heredan de `StockLote`, que es abstracta y no se mapea.
- **Índices únicos:** código de barras, lote por producto, lote en cada stock, documento de empleado, documento de cliente, usuario, rol, clave de parámetro y widget por empleado.
- **Índices no únicos:** `EventosOutbox (Procesado, Id)`. `Venta.ClaveIdempotencia` tiene índice único; valores `NULL` se permiten.
- **Borrado:** todas las FK son `Restrict`. Las ventas se anulan, no se eliminan.
- **Fechas:** `TimeProvider` inyectado; `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.

### Transacciones

`UnidadDeTrabajo.GuardarAsync` es un `SaveChangesAsync`. Un caso de uso hace **una sola** llamada al final. Los servicios de dominio modifican entidades y registran movimientos, pero no guardan.

Esto garantiza atomicidad dentro de un proceso, pero **no** serializa lecturas concurrentes. Ver la sección de concurrencia.

## Seguridad

- `SesionActual` es singleton y guarda el `Usuario` autenticado.
- `Exigir(permiso)` falla si no hay sesión, si el empleado está inactivo (y cierra la sesión), si hay cambio de clave pendiente o si falta el permiso.
- El hash y el PIN usan BCrypt con salt (`HasheadorBCrypt`).
- El bloqueo cuenta intentos fallidos, incluido el caso de empleado inactivo con usuario existente.
- La auditoría se escribe en la misma unidad de trabajo que la acción.
- **Hallazgos abiertos:** la validación de `PagoPuntos`, la escalada de privilegios vía `GestionarPersonal` y la revocación de sesión están documentadas en [`pendientes-ui.md`](pendientes-ui.md). No se consideran cerradas para producción.

## Interfaz Gráfica (`Supermercado.GUI`)

La interfaz de usuario está construida sobre **Avalonia 12.1.4** y **.NET 10** bajo el patrón **MVVM**:
- **Inyección de Dependencias:** `App.axaml.cs` inicializa el contenedor con `servicios.AgregarLogica()`, registrando `IImpresorTicket` (`ImpresorTicketEscPos` e `ImpresorTicketPdf`) y los 11 ViewModels en ciclo de vida transitorio para navegación y back-office.
- **Servicios de Presentación Consolidados:** Agrupados en 4 módulos en `Supermercado.GUI/Servicios/`: `ConfiguracionYTema.cs` (temas y preferencias), `ImpresionTicket.cs` (impresión térmica ESC/POS y PDF), `PlantillaTicketPos.cs` (tirilla continua de 80mm calibrada) y `ValidadorDian.cs` (Módulo 11 NIT y textos de Habeas Data).
- **Tokens y Temas:** Modelo en capas (`Primitives.axaml`, `Semantic.axaml`, `Geometry.axaml`, `Typography.axaml` y `SemiBridge.axaml`) con Claro Cálido (*Warm Light* anti-fatiga visual), Oscuro Slate y Alto Contraste. El tema elegido se persiste por máquina con `IServicioPreferencias` y se aplica antes de crear la ventana.
- **Configuración:** overlay compartido en `MainWindow.axaml` (`ConfiguracionView`) accesible desde login y shell; apariencia, textos legales empaquetados y versión. Ver ADR-028.
- **Ergonomía POS:** Atajos físicos `F1`–`F5` / `Escape`, auto-enfoque ininterrumpido en el escáner de código de barras (*keyboard wedge*) e integración automática con balanzas EAN-13 GS1 (`20`–`29`).
- **Detalle completo:** ver [`docs/sistema-diseno-tokens-ui.md`](sistema-diseno-tokens-ui.md).

## Dashboards

`IWidget` define `Id`, `Titulo`, `Tamano`, `FrecuenciaActualizacion`, `PermisoRequerido` y `ObtenerDatosAsync`. `ServicioDashboard` filtra por permiso, guarda la disposición por empleado y mantiene posiciones únicas (`Max + 1` al agregar, `0..n-1` al reordenar). Las 9 implementaciones de tarjetas de métricas residen en `Supermercado.Logica/Dashboards/Widgets.cs`.

## Integraciones externas y soporte

Los clientes externos e infraestructura de soporte se organizan en módulos consolidados en `Supermercado.Logica/Servicios/` y `Supermercado.Logica/Reportes/`:

| Interfaz | Implementación por defecto | Módulo fuente | Reemplazo previsto |
| :-- | :-- | :-- | :-- |
| `ITrmClient` | `ClienteTrmDatosGov` (activo por defecto; `ClienteTrmNoConfigurado` si se apaga) | `ClientesExternos.cs` | Otra fuente de TRM |
| `IConsultaCodigoBarras` | `ClienteCodigoBarrasOff` (activo por defecto; `ConsultaCodigoBarrasNula` si se apaga) | `ClientesExternos.cs` | Otro catálogo de productos |
| `IVerificadorCorreo` | `VerificadorCorreoNulo`; `ClienteVerificadorCorreoHttp` solo con `SIGS_EMAIL_VERIFIER_URL` | `ClientesExternos.cs` | Motor abierto pendiente de elegir |
| `IServicioRespaldo` | `ServicioRespaldo` (copia atómica en caliente de SQLite vía `VACUUM INTO`) | `ServiciosSoporte.cs` | Almacenamiento en nube / NAS |
| `IGeneradorTicket` | `GeneradorTicketQuestPdf` (registrado en DI; `GeneradorTicketNulo` para pruebas) | `GeneradorTickets.cs` | QuestPDF |
| `IServicioReportes` | `ServicioReportesQuestPdf` | `ServicioReportes.cs` | Generador alternativo de PDFs |

Apagado y URLs: ver ADR-019. `ServicioTrm` aplica timeout, reintentos con espera progresiva (200 ms, 400 ms, … hasta 2 s) y guarda el respaldo en una sola operación atómica.

### DIAN y réplica

- `ServicioDian` genera localmente el Documento Equivalente POS: CUDE con SHA-384, XML UBL 2.1 y cadena QR. **No transmite, no firma y no maneja contingencia**; el consecutivo se avanza en `Parametros`.
- `EventoOutbox` registra `VentaCompletada` y `VentaAnulada` en la misma transacción local. `ClienteSincronizacionSupabase` los despacha por REST cuando `integraciones.supabase_habilitado` está activo y hay URL/clave. No hay proceso programado ni receptor desplegado.

## Base de datos y evolución

### Por qué SQLite no alcanza para varias cajas

SQLite es un archivo por equipo. Si cada caja tiene su archivo, no comparten stock, precios ni turnos. Eso rompe el inventario dual y el arqueo. La arquitectura objetivo es:

```text
Caja 1 ─┐
Caja 2 ─┼──► PostgreSQL local (LAN del supermercado) ───► Supabase (réplica de una vía)
Bodega ─┘        fuente de verdad                         respaldo / consulta remota
```

- **PostgreSQL local** en un equipo del supermercado o un mini-PC/NAS. Todas las terminales se conectan por LAN; no dependen de internet.
- **Supabase / Neon** recibe una copia de una sola vía vía patrón Outbox (`EventoOutbox` + `ClienteSincronizacionSupabase`). **Nunca** se sincroniza de vuelta.
- Si se cae el servidor local, se cae la operación: se mitiga con UPS, respaldo y un equipo de respaldo. No se resuelve con sincronización bidireccional.

Esta arquitectura cuenta con **soporte dual implementado**:
- **PostgreSQL 17** activo mediante `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 cuando se configura `SIGS_POSTGRES_CONNECTION` (orquestable con `docker-compose.yml` en la raíz).
- **SQLite** como respaldo zero-config para estaciones aisladas o desarrollo.
- `InicializadorBaseDatos.CrearYSembrarAsync()` se ejecuta al inicio de la aplicación en ambos motores para garantizar la creación de tablas e índices únicos de idempotencia.

### Concurrencia multi-terminal (deuda técnica en red LAN)

Con varias cajas escribiendo simultáneamente sobre el PostgreSQL central, los patrones actuales `leer → validar → escribir` pueden tener carreras si dos cajas operan sobre la misma unidad:

| Operación | Riesgo | Mitigación planificada |
| :-- | :-- | :-- |
| Registrar venta / descontar stock | Dos cajas venden la última unidad | Transacción + `SELECT … FOR UPDATE` de las filas de stock ordenadas por vencimiento |
| Anular venta | Dos anulaciones reintegran dos veces | Bloquear la venta y revalidar el estado dentro de la transacción |
| Abrir turno | Dos cajeros abren la misma caja | Bloquear la `Caja` o índice único parcial `(CajaId) WHERE Estado = Abierto` |
| Cerrar turno | Entran ventas mientras se calcula el esperado | Bloquear el turno durante el cierre |

Siguientes pasos en concurrencia:
- `UseXminAsConcurrencyToken()` en `StockLote`, `Venta` y `TurnoCaja` (Npgsql).
- Reintentos acotados ante `40001` o `DbUpdateConcurrencyException`, con `ConflictoConcurrenciaException`.
- Réplica Outbox asíncrona hacia Supabase vía `ClienteSincronizacionSupabase`.

## Decisiones relacionadas

- [`decisiones.md`](decisiones.md): ADR-001 a ADR-022.
- [`despliegue.md`](despliegue.md): escenarios de desarrollo y producción.
- [`diccionario-datos.md`](diccionario-datos.md): esquema real y mapeo previsto.
