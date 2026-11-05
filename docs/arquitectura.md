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
servicios.AgregarDatos(rutaBaseDatos); // DbContext + repositorios
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
    var carpeta = Path.GetDirectoryName(rutaBaseDatos);
    if (!string.IsNullOrEmpty(carpeta))
        Directory.CreateDirectory(carpeta);

    opciones.UseSqlite($"Data Source={rutaBaseDatos}");
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
servicios.AddScoped<ITurnoCajaRepositorio, TurnoCajaRepositorio>();
servicios.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
servicios.AddScoped<IRolRepositorio, RolRepositorio>();
servicios.AddScoped<IParametroRepositorio, ParametroRepositorio>();
servicios.AddScoped<IGastoRepositorio, GastoRepositorio>();
servicios.AddScoped<IConfiguracionWidgetRepositorio, ConfiguracionWidgetRepositorio>();
```

`ExtensionesDatos` utiliza **SQLite local** en la carpeta de datos del usuario (`%LOCALAPPDATA%/SIGS/sigs.db` en Windows o `~/.local/share/SIGS/sigs.db` en Linux) sin configuración previa. La lógica y los ViewModels permanecen desacoplados del motor de persistencia.

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

- **TPH:** `Productos` (unitario/pesado), `Impuestos` (IVA/retención) y `Pago` (efectivo/tarjeta/transferencia) usan una tabla y `Discriminator`.
- **Tablas separadas:** `StockEstante` y `StockBodega` heredan de `StockLote`, que es abstracta y no se mapea.
- **Índices únicos:** código de barras, lote por producto, lote en cada stock, documento de empleado, documento de cliente, usuario, rol, clave de parámetro, widget por empleado y `Venta.ClaveIdempotencia` (permite valores `NULL`).
- **Borrado:** todas las FK son `Restrict`. Las ventas se anulan, no se eliminan.
- **Fechas:** `TimeProvider` inyectado; `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.

### Transacciones

`UnidadDeTrabajo.GuardarAsync` es un `SaveChangesAsync`. Un caso de uso hace **una sola** llamada al final. Los servicios de dominio modifican entidades y registran movimientos, pero no guardan.

Esto garantiza atomicidad dentro de un proceso local.

## Seguridad

- `SesionActual` es singleton y guarda el `Usuario` autenticado.
- `Exigir(permiso)` falla si no hay sesión, si el empleado está inactivo (y cierra la sesión), si hay cambio de clave pendiente o si falta el permiso.
- El hash y el PIN usan BCrypt con salt (`HasheadorBCrypt`).
- El bloqueo cuenta intentos fallidos, incluido el caso de empleado inactivo con usuario existente.
- La auditoría se escribe en la misma unidad de trabajo que la acción.
- **Hallazgos abiertos:** la escalada de privilegios vía `GestionarPersonal` y la revocación de sesión están documentadas en [`pendientes-ui.md`](pendientes-ui.md). No se consideran cerradas para producción.

## Interfaz Gráfica (`Supermercado.GUI`)

La interfaz de usuario está construida sobre **Avalonia 12.1.4** y **.NET 10** bajo el patrón **MVVM**:
- **Inyección de Dependencias:** `App.axaml.cs` inicializa el contenedor con `servicios.AgregarLogica()`, registrando `IImpresorTicket` (`ImpresorTicketEscPos` e `ImpresorTicketPdf`) y los 11 ViewModels en ciclo de vida transitorio para navegación y back-office.
- **Servicios de Presentación Consolidados:** Agrupados en 3 módulos en `Supermercado.GUI/Servicios/`: `ConfiguracionYTema.cs` (temas y preferencias), `ImpresionTicket.cs` (impresión térmica ESC/POS y PDF) y `PlantillaTicketPos.cs` (tirilla continua de 80mm calibrada).
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

## Base de datos y evolución

### Persistencia SQLite autónoma

En la rama principal (`main`), SIGS opera de forma autónoma sobre **SQLite local** con inicialización automática (`InicializadorBaseDatos.CrearYSembrarAsync()`), creando el archivo de base de datos, el esquema completo y la semilla idempotente en el primer arranque sin requerir motores externos ni contenedores.

> La versión empresarial extendida (con proveedor dual PostgreSQL 17 + Npgsql, réplica Outbox hacia Supabase, facturación electrónica DIAN UBL 2.1 / CUDE y fidelización por puntos) se conserva íntegra en la rama `completo`.

## Decisiones relacionadas

- [`decisiones.md`](decisiones.md): ADR-001 a ADR-028.
- [`despliegue.md`](despliegue.md): escenarios de desarrollo y producción.
- [`diccionario-datos.md`](diccionario-datos.md): esquema real y mapeo previsto.
