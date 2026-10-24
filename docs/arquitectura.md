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
servicios.AgregarDatos(rutaBaseDatos);          // DbContext + repositorios
servicios.AddSingleton<ISesionActual, SesionActual>();
servicios.AddSingleton<IHasheadorClave, HasheadorBCrypt>();
servicios.AddScoped<IEstrategiaSalida, SalidaFefo>();
servicios.AddScoped<IServicioVentas, ServicioVentas>();
// ... resto de servicios y widgets
```

`Supermercado.Datos/ExtensionesDatos.cs`:

```csharp
servicios.AddSingleton(TimeProvider.System);
servicios.AddDbContext<SigsDbContext>(
    opciones => opciones.UseSqlite($"Data Source={rutaBaseDatos}"));
servicios.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
```

`UseSqlite` es el **único punto** que decide el proveedor. Cambiar a PostgreSQL implica cambiar este archivo, añadir el paquete Npgsql y crear migraciones; la lógica no se toca.

## Flujos principales

### Venta

```text
GUI
 └─ ServicioVentas.RegistrarVentaAsync(SolicitudVenta)
     ├─ sesion.Exigir(Vender)
     ├─ turnos.ObtenerAbiertoPorEmpleadoAsync
     ├─ por línea: productos.ObtenerPorCodigoBarrasAsync
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
ServicioReabastecimiento.GenerarSolicitudesAsync
 ├─ Exigir(Reabastecer)
 ├─ alertas.ObtenerBajoMinimoAsync
 ├─ crea Reabastecimiento(Pendiente) si no existe uno pendiente
 └─ guarda

ServicioReabastecimiento.AtenderAsync(id)
 ├─ solicitud.Atender(ahora)
 ├─ inventario.TrasladarAEstanteAsync(producto, cantidad)
 └─ guarda
```

`Cancelar()` deja el estado `Cancelado` alcanzable y `Atender`/`Cancelar` rechazan solicitudes ya cerradas con `ValidacionException`.

### Nómina

```text
ServicioNomina.CalcularAsync(empleadoId, anio, mes)
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
- **Índices únicos:** código de barras, lote por producto, lote en cada stock, documento, usuario, rol, clave de parámetro y widget por empleado.
- **Borrado:** todas las FK son `Restrict`. Las ventas se anulan, no se eliminan.
- **Fechas:** `TimeProvider` inyectado; `SigsDbContext` asigna `FechaCreacion` en `SaveChanges` y `SaveChangesAsync`.

### Transacciones

`UnidadDeTrabajo.GuardarAsync` es un `SaveChangesAsync`. Un caso de uso hace **una sola** llamada al final. Los servicios de dominio modifican entidades y registran movimientos, pero no guardan.

Esto garantiza atomicidad dentro de un proceso, pero **no** serializa lecturas concurrentes. Ver la sección de concurrencia.

## Seguridad

- `SesionActual` es singleton y guarda el `Usuario` autenticado.
- `Exigir(permiso)` falla si no hay sesión, si el empleado está inactivo (y cierra la sesión), si hay cambio de clave pendiente o si falta el permiso.
- El hash y el PIN usan BCrypt con salt.
- El bloqueo cuenta intentos fallidos, incluido el caso de empleado inactivo con usuario existente.
- La auditoría se escribe en la misma unidad de trabajo que la acción.

## Dashboards

`IWidget` define `Id`, `Titulo`, `Tamano`, `FrecuenciaActualizacion`, `PermisoRequerido` y `ObtenerDatosAsync`. `ServicioDashboard` filtra por permiso, guarda la disposición por empleado y mantiene posiciones únicas (`Max + 1` al agregar, `0..n-1` al reordenar).

## Integraciones externas

| Interfaz | Implementación por defecto | Reemplazo previsto |
| :-- | :-- | :-- |
| `ITrmClient` | `ClienteTrmNoConfigurado` | Cliente HTTP de la TRM |
| `IConsultaCodigoBarras` | `ConsultaCodigoBarrasNula` | Servicio abierto de productos |
| `IVerificadorCorreo` | `VerificadorCorreoNulo` | Reacher u homólogo |
| `IGeneradorTicket` | `GeneradorTicketNulo` | QuestPDF |

`ServicioTrm` aplica timeout, reintentos con espera progresiva (200 ms, 400 ms, … hasta 2 s) y guarda el respaldo en una sola operación atómica.

## Base de datos y evolución

### Por qué SQLite no alcanza para varias cajas

SQLite es un archivo por equipo. Si cada caja tiene su archivo, no comparten stock, precios ni turnos. Eso rompe el inventario dual y el arqueo. La arquitectura objetivo es:

```text
Caja 1 ─┐
Caja 2 ─┼──► PostgreSQL local (LAN del supermercado) ───► Supabase (réplica de una vía)
Bodega ─┘        fuente de verdad                         respaldo / consulta remota
```

- **PostgreSQL local** en un equipo del supermercado o un mini-PC/NAS. Todas las terminales se conectan por LAN; no dependen de internet.
- **Supabase** recibe una copia de una sola vía para respaldo y consulta remota. **Nunca** se sincroniza de vuelta.
- Si se cae el servidor local, se cae la operación: se mitiga con UPS, respaldo y un equipo de respaldo. No se resuelve con sincronización bidireccional.

Esta arquitectura está **documentada como objetivo de producción, no implementada**. El código sigue con SQLite.

### Concurrencia multi-terminal (deuda técnica)

Con varias cajas escribiendo, los patrones actuales `leer → validar → escribir` tienen carreras:

| Operación | Riesgo | Mitigación planificada |
| :-- | :-- | :-- |
| Registrar venta / descontar stock | Dos cajas venden la última unidad | Transacción + `SELECT … FOR UPDATE` de las filas de stock ordenadas por vencimiento |
| Anular venta | Dos anulaciones reintegran dos veces | Bloquear la venta y revalidar el estado dentro de la transacción |
| Abrir turno | Dos cajeros abren la misma caja | Bloquear la `Caja` o índice único parcial `(CajaId) WHERE Estado = Abierto` |
| Cerrar turno | Entran ventas mientras se calcula el esperado | Bloquear el turno durante el cierre |

Además se prevé:

- `UseXminAsConcurrencyToken()` en `StockLote`, `Venta` y `TurnoCaja` (Npgsql).
- Reintentos acotados ante `40001` o `DbUpdateConcurrencyException`, con una `ConflictoConcurrenciaException` clara al agotarse.
- Pruebas de integración contra PostgreSQL, opcionales y saltadas si no hay servidor.

Nada de esto está implementado en esta entrega. Ver ADR-010 en [`decisiones.md`](decisiones.md).

### Camino de migración

1. Añadir `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Cambiar `UseSqlite` por `UseNpgsql` en `ExtensionesDatos`.
3. Generar migraciones EF; `InicializadorBaseDatos` ya usa `MigrateAsync` cuando existen.
4. Mapear `DateTime` a `timestamptz`, `DateOnly` a `date` y el dinero a `bigint`.
5. Añadir tokens de concurrencia y transacciones con bloqueo.
6. Configurar la réplica de una vía hacia Supabase.

## Decisiones relacionadas

- [`decisiones.md`](decisiones.md): ADR-001 a ADR-015.
- [`despliegue.md`](despliegue.md): escenarios de desarrollo y producción.
- [`diccionario-datos.md`](diccionario-datos.md): esquema real y mapeo previsto.
