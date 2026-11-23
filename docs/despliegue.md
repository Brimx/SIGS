# Despliegue

> **Antes de producción:** el backend tiene hallazgos abiertos de seguridad y concurrencia (ver [`pendientes-ui.md`](pendientes-ui.md) y la auditoría en la memoria del proyecto). Este documento describe el despliegue técnico, no certifica que el sistema esté listo para operar con dinero real.

## 1. Escenarios

| Escenario | Base de datos | Estado | Uso |
| :-- | :-- | :-- | :-- |
| Desarrollo | SQLite local | **Implementado** | Un programador, pruebas y demo |
| Producción de una terminal | SQLite local | **Implementado** | Un solo equipo |
| Producción multi-terminal | PostgreSQL local + réplica Supabase | **Objetivo futuro, no implementado en `main`** | Varias cajas en la misma red; arquitectura extendida conservada en la rama `completo` |
| Nube | Supabase como fuente | **Objetivo futuro, no implementado en `main`** | Arquitectura extendida conservada en la rama `completo`; latencia y dependencia de internet desaconsejan su operación |

En `main` el único proveedor es SQLite (`UseSqlite` en `Supermercado.Datos/Contexto.cs`): no hay Npgsql, Docker, Outbox ni parámetros `dian.*`. Los escenarios de la sección 4 son objetivo futuro fuera del alcance de esta entrega.

## 2. Desarrollo

### Requisitos

- .NET SDK 10
- Git
- Linux x64 o Windows 10+

### Pasos

```bash
git clone https://github.com/Brimx/SIGS.git
cd SIGS
dotnet restore Supermercado.slnx
dotnet build Supermercado.slnx
dotnet run --project Supermercado.GUI
```

### Base de datos

- Ruta por defecto: `RutasDatos.BaseDatosPorDefecto()`.
  - Windows: `%LOCALAPPDATA%\SIGS\sigs.db`
  - Linux: `~/.local/share/SIGS/sigs.db`
- El arranque crea la carpeta y el archivo.
- `InicializadorBaseDatos` usa `MigrateAsync` si existen migraciones; si no, `EnsureCreatedAsync`.
- `Supermercado.Datos` incluye `FabricaSigsDbContext` (`IDesignTimeDbContextFactory<SigsDbContext>`) para ejecutar comandos de EF Core sin que la GUI referencie a Datos:
  ```bash
  dotnet ef migrations add <Nombre> --project Supermercado.Datos --startup-project Supermercado.Datos --output-dir Migraciones
  dotnet ef database update --project Supermercado.Datos --startup-project Supermercado.Datos
  ```
- La semilla inserta roles, cargos, impuestos, cajas y parámetros faltantes; nunca sobrescribe valores editados.
- Para empezar de cero, cierre la aplicación y borre el archivo. **Pierde todos los datos.**

### Publicación autocontenida

```bash
dotnet publish Supermercado.GUI/Supermercado.GUI.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -o publicacion/linux
```

Para Windows:

```bash
dotnet publish Supermercado.GUI/Supermercado.GUI.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o publicacion/windows
```

El ejecutable no requiere el runtime instalado. La base sigue en la carpeta de datos del usuario, no junto al ejecutable.

## 3. Producción de una terminal

1. Publique en `Release`.
2. Copie la carpeta publicada al equipo.
3. Ejecute una vez para crear la base.
4. Inicialice el administrador con una clave temporal; el sistema obliga a cambiarla.
5. Configure los parámetros vigentes: `seguridad.max_intentos`, `inventario.dias_alerta`, `caja.tolerancia_arqueo`, `nomina.*`, `integraciones.*` (timeout, reintentos, TRM, código de barras y tokens de TRM/Resend) y `tributario.responsable_iva`.
6. Confirme `negocio.nombre`, `negocio.nit`, `negocio.direccion` y `negocio.telefono` para el ticket, y `sistema.carpeta_respaldos` como destino de respaldos.
7. Programe el respaldo con `ServicioRespaldo` y pruebe la restauración (ver «Respaldo»).

> Puntos, `dian.*`, resolución DIAN, Supabase y Outbox no forman parte de esta entrega: son objetivo futuro fuera del alcance de `main`, conservada la arquitectura extendida en la rama `completo`.

### Respaldo

`ServicioRespaldo` (`Supermercado.Logica/Infraestructura.cs`) copia el archivo con `File.Copy` a `sistema.carpeta_respaldos` y registra auditoría; no implementa `VACUUM INTO`. Para una copia consistente, copie con la aplicación cerrada o use la herramienta `sqlite3` con `VACUUM INTO` como recomendación operativa de SQLite, no como una función del sistema.

```bash
# Linux, opcional: herramienta externa sqlite3, con la aplicación cerrada
sqlite3 ~/.local/share/SIGS/sigs.db \
  "VACUUM INTO '$HOME/respaldos/sigs-$(date +%F).db';"
```

```powershell
# Windows PowerShell, opcional: herramienta externa sqlite3, con la aplicación cerrada
$origen = "$env:LOCALAPPDATA\SIGS\sigs.db"
$destino = "$env:USERPROFILE\respaldos\sigs-$(Get-Date -Format yyyy-MM-dd).db"
sqlite3 $origen "VACUUM INTO '$destino';"
```

Recomendaciones:

- Programe el respaldo fuera del horario de caja y verifique que el archivo resultante abre.
- Conserve al menos 7 días y una copia fuera del equipo.
- Pruebe la restauración antes de confiar en el respaldo.
- No sincronice el archivo por Dropbox/OneDrive con la aplicación abierta: puede corromperse.
- El archivo contiene datos personales y secretos de integración: cífrelo en reposo y restrinja el acceso.

### Secretos y datos personales

- `integraciones.trm_token` e `integraciones.resend_token` no van en el repositorio ni en capturas.
- La base SQLite no está cifrada: si el equipo se comparte, use cifrado de disco y cuentas separadas.

> Los secretos de la arquitectura extendida (`integraciones.supabase_key`, `dian.clave_tecnica`) y la recomendación sobre `service_role` pertenecen al objetivo futuro de la sección 4; no se configuran en `main`.

## 4. Producción multi-terminal (objetivo)

> **Estado:** objetivo futuro fuera del alcance de `main`, no una guía operativa de esta entrega. Esta topología no está implementada: `main` solo usa SQLite (`UseSqlite` en `Supermercado.Datos/Contexto.cs`) y no contiene Npgsql, Docker, Outbox ni `dian.*`. El proveedor PostgreSQL dual, Docker y el Outbox hacia Supabase se retiraron en `2c38285`; el código se conserva en la rama `completo`. Los secretos `integraciones.supabase_key` y `dian.clave_tecnica` pertenecen a esa arquitectura objetivo. Lo que sigue describe esa arquitectura, no lo que hoy ejecuta la aplicación.

### Topología

```text
Caja 1 ─┐
Caja 2 ─┼──► PostgreSQL local (LAN) ───► Supabase (réplica de una vía)
Bodega ─┘     fuente de verdad            respaldo / consulta remota
```

Decisiones:

- **PostgreSQL local** en un equipo del supermercado, mini-PC o NAS. Es la única fuente de verdad.
- **Supabase** recibe una copia de una sola vía. **Nunca** se escribe de vuelta.
- Las cajas no dependen de internet: si Supabase falla, la operación continúa.
- Si falla el servidor local, se detiene la operación. Se mitiga con UPS, respaldo frecuente y un equipo de respaldo; **no** con sincronización bidireccional.

### Por qué no hay sincronización bidireccional

- Dos terminales pueden vender la misma unidad y ambas "ganar".
- Los identificadores autoincrementales chocan.
- El arqueo suma ventas duplicadas.
- Resolver conflictos de stock requiere reglas que el negocio no puede aplicar en caja.

Una réplica de una vía evita el problema: solo un nodo escribe.

### Pasos de migración

1. Agregar `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Cambiar `UseSqlite` por `UseNpgsql` en `Supermercado.Datos/Contexto.cs`; es el único punto del proveedor.
3. Mapear `DateTime` a `timestamptz`, `DateOnly` a `date` y dinero a `bigint`.
4. Generar migraciones EF y probar `MigrateAsync` en una base vacía.
5. Migrar los datos existentes de SQLite si hay historial que conservar.
6. Desplegar el esquema receptor (rama `completo`) con RLS de inserción y vistas `security_invoker=true`; programar el drenaje del Outbox.
7. Configurar la réplica de una vía hacia Supabase (publicación lógica o proceso programado) y usar solo la clave `anon` en el cliente.
8. Añadir concurrencia (ver siguiente sección) **antes** de habilitar varias cajas.

### Concurrencia (deuda técnica)

Con varias cajas escribiendo, `leer → validar → escribir` tiene carreras:

| Operación | Riesgo | Mitigación |
| :-- | :-- | :-- |
| Venta / descuento de stock | Dos cajas venden la última unidad | Transacción + `SELECT … FOR UPDATE` de las filas ordenadas por vencimiento |
| Anulación | Dos anulaciones reintegran dos veces | Bloquear la venta y revalidar el estado |
| Apertura de turno | Dos cajeros abren la misma caja | Bloquear la caja o índice único parcial `(CajaId) WHERE Estado = Abierto` |
| Cierre de turno | Entran ventas mientras se calcula el esperado | Bloquear el turno durante el cierre |

Además:

- `UseXminAsConcurrencyToken()` en `StockLote`, `Venta` y `TurnoCaja`.
- Reintentos acotados ante `40001` o `DbUpdateConcurrencyException`.
- `ConflictoConcurrenciaException` clara al agotar reintentos.
- Pruebas de integración contra PostgreSQL, opcionales si no hay servidor.

Nada de esto está implementado en `main` ni entra en el alcance de esta entrega: es objetivo futuro conservado en la rama `completo`. Ver ADR-010 en [`decisiones.md`](decisiones.md).

## 5. Integración continua

`.github/workflows/ci.yml`:

1. Checkout.
2. `setup-dotnet` con `10.0.x`.
3. `dotnet restore Supermercado.slnx`.
4. `dotnet build --no-restore -c Release`.
5. `dotnet test --no-build -c Release` con logger `.trx`.
6. Publica el artefacto de resultados.

El pipeline falla si el build tiene errores o alguna prueba falla. Las advertencias no rompen el pipeline, pero el objetivo es zero warnings.

## 6. Checklist de entrega

- [ ] `dotnet build` sin errores ni advertencias.
- [ ] `dotnet test` en verde.
- [ ] CI en verde en `main`.
- [ ] Base SQLite creada con la semilla de 4 roles en un equipo limpio.
- [ ] Login de admin con cambio de clave obligatorio.
- [ ] Parámetros vigentes cargados (`negocio.*`, `tributario.responsable_iva`, `integraciones.*`, `nomina.*` y `sistema.carpeta_respaldos`).
- [ ] Consolidado fiscal y desglose de IVA revisados en la pantalla de contabilidad.
- [ ] Respaldo con `ServicioRespaldo` probado y restauración verificada.
- [ ] Rutas de datos confirmadas en Windows y Linux.
- [ ] Documentación actualizada (`README`, diccionario, matriz de permisos).

## 7. Solución de problemas

| Síntoma | Causa | Acción |
| :-- | :-- | :-- |
| «database is locked» | Dos procesos sobre el mismo archivo | Cerrar la otra instancia; SQLite no es multi-terminal |
| La base se recrea vacía | Ruta distinta o carpeta sin permisos | Revisar `RutasDatos` y permisos |
| `ServicioNoConfiguradoException` | Se pidió una integración con su cliente nulo | Activar la API o registrar la implementación real después de `AgregarLogica()` |
| `ParametroFaltanteException` | Falta una clave | Insertar el parámetro con `GuardarAsync` |
| Build falla por SDK | Versión distinta de .NET | Instalar .NET 10 y confirmar la versión con `dotnet --version` |
| CI no encuentra el proyecto | Ruta o nombre cambiado | Actualizar `.github/workflows/ci.yml` |
