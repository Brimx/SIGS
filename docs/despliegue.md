# Despliegue

> **Antes de producción:** el backend tiene hallazgos abiertos de seguridad y concurrencia (ver [`pendientes-ui.md`](pendientes-ui.md) y la auditoría en la memoria del proyecto). Este documento describe el despliegue técnico, no certifica que el sistema esté listo para operar con dinero real.

## 1. Escenarios

| Escenario | Base de datos | Estado | Uso |
| :-- | :-- | :-- | :-- |
| Desarrollo | SQLite local | **Implementado** | Un programador, pruebas y demo |
| Producción de una terminal | SQLite local | **Implementado** | Un solo equipo |
| Producción multi-terminal | PostgreSQL local + réplica Supabase | **Documentado, no implementado** | Varias cajas en la misma red |
| Nube | Supabase como fuente | **Descartado** | Latencia y dependencia de internet |

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
5. Configure los parámetros del negocio: intentos, días de alerta, tolerancia, nómina, integraciones, puntos, `tributario.responsable_iva` y `dian.*`.
6. Revise la resolución DIAN: prefijo, rango, vigencia y clave técnica. Si no va a emitir, ponga `dian.habilitado = 0`.
7. Programe respaldos con `VACUUM INTO` y pruebe la restauración.
8. Si usa réplica, configure `integraciones.supabase_*` y verifique que el Outbox drena; no comparta `service_role`.

### Respaldo

No copie el archivo con `cp`/`Copy-Item` mientras la aplicación escribe: una copia a mitad de transacción puede quedar inconsistente. Use la API de respaldo de SQLite (`VACUUM INTO`) o cierre la aplicación.

```bash
# Linux, con la aplicación cerrada o desde una conexión de solo lectura
sqlite3 ~/.local/share/SIGS/sigs.db \
  "VACUUM INTO '$HOME/respaldos/sigs-$(date +%F).db';"
```

```powershell
# Windows PowerShell, con la aplicación cerrada
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

- `integraciones.supabase_key`, `integraciones.trm_token`, `integraciones.resend_token` y `dian.clave_tecnica` no van en el repositorio ni en capturas.
- La base SQLite no está cifrada: si el equipo se comparte, use cifrado de disco y cuentas separadas.
- En Supabase use solo la clave `anon` en el cliente; **nunca** `service_role`. El esquema de [`supabase-schema.sql`](supabase-schema.sql) debe aplicar RLS de inserción y vistas con `security_invoker=true`.

## 4. Producción multi-terminal (objetivo)

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
2. Cambiar `UseSqlite` por `UseNpgsql` en `ExtensionesDatos`; es el único punto del proveedor.
3. Mapear `DateTime` a `timestamptz`, `DateOnly` a `date` y dinero a `bigint`.
4. Generar migraciones EF y probar `MigrateAsync` en una base vacía.
5. Migrar los datos existentes de SQLite si hay historial que conservar.
6. Desplegar el esquema receptor de [`supabase-schema.sql`](supabase-schema.sql) con RLS de inserción y vistas `security_invoker=true`; programar el drenaje del Outbox.
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

Nada de esto está implementado en esta entrega. Ver ADR-010 en [`decisiones.md`](decisiones.md).

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
- [ ] `dotnet test` en verde (302).
- [ ] CI en verde en `main`.
- [ ] Base creada y semilla aplicada en un equipo limpio.
- [ ] Login de admin con cambio de clave obligatorio.
- [ ] Parámetros del negocio cargados (incluidos puntos y `tributario.responsable_iva`).
- [ ] Resolución DIAN revisada o `dian.habilitado = 0` si no se emitirá.
- [ ] Respaldo con `VACUUM INTO` probado y restauración verificada.
- [ ] Outbox configurado y probado si hay réplica; clave `anon`, nunca `service_role`.
- [ ] Rutas de datos confirmadas en Windows y Linux.
- [ ] Documentación actualizada (`README`, diccionario, matriz de permisos).

## 7. Solución de problemas

| Síntoma | Causa | Acción |
| :-- | :-- | :-- |
| «database is locked» | Dos procesos sobre el mismo archivo | Cerrar la otra instancia; SQLite no es multi-terminal |
| La base se recrea vacía | Ruta distinta o carpeta sin permisos | Revisar `RutasDatos` y permisos |
| `ServicioNoConfiguradoException` | Se pidió una integración con su cliente nulo | Activar la API o registrar la implementación real después de `AgregarLogica()` |
| `ParametroFaltanteException` | Falta una clave | Insertar el parámetro con `GuardarAsync` |
| Build falla por SDK | Versión distinta de .NET | Instalar .NET 10 y revisar `global.json` si existe |
| CI no encuentra el proyecto | Ruta o nombre cambiado | Actualizar `.github/workflows/ci.yml` |
