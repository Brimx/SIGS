# Diccionario de datos

Estado actual de `main`: EF Core 10 sobre **SQLite**. No hay PostgreSQL, Supabase ni Outbox en el código de esta rama; la arquitectura objetivo se describe en [`despliegue.md`](despliegue.md) y se conserva en la rama `completo`.

Convenciones generales:

- Todas las tablas heredan de `EntidadBase`: `Id` (PK autoincremental) y `FechaCreacion` (UTC, asignada por `SigsDbContext`).
- El dinero es `long` en pesos colombianos. Los porcentajes son `int` en puntos básicos (1900 = 19 %).
- Las fechas de negocio se guardan en UTC como `DateTime`; las fechas sin hora usan `DateOnly`.
- Ninguna relación se borra en cascada: `DeleteBehavior.Restrict`. Las ventas se anulan, no se eliminan.

## Mapa entidad → tabla

| Entidad | Tabla | Notas |
| :-- | :-- | :-- |
| `Producto` / `ProductoUnitario` / `ProductoPesado` | `Productos` | TPH con `Discriminator` |
| `Categoria` | `Categorias` | |
| `Proveedor` | `Proveedores` | |
| `Lote` | `Lotes` | Único por `(ProductoId, NumeroLote)` |
| `StockEstante` | `StockEstante` | Tabla propia; único por `LoteId` |
| `StockBodega` | `StockBodega` | Tabla propia; único por `LoteId` |
| `MovimientoInventario` | `Movimientos` | FK a `Lotes` y `Ventas` |
| `Merma` | `Mermas` | |
| `Reabastecimiento` | `Reabastecimientos` | |
| `Venta` | `Ventas` | |
| `DetalleVenta` | `DetalleVenta` | |
| `VentaEnEspera` | `VentasEnEspera` | |
| `Pago` / `PagoEfectivo` / `PagoTarjeta` / `PagoTransferencia` | `Pago` | TPH con `Discriminator` |
| `Caja` | `Cajas` | |
| `TurnoCaja` | `Turnos` | FK a `Cajas` y `Empleados` |
| `RetiroCaja` | `RetirosCaja` | |
| `Cliente` | `Clientes` | Único por `Documento` |
| `Cargo` | `Cargos` | |
| `Rol` | `Roles` | Único por `Nombre` |
| `Empleado` | `Empleados` | Único por `Documento` |
| `Usuario` | `Usuarios` | Único por `NombreUsuario` |
| `Impuesto` / `Iva` / `Retencion` | `Impuestos` | TPH con `Discriminator` |
| `Gasto` | `Gastos` | |
| `Nomina` | `Nominas` | |
| `RegistroAuditoria` | `Auditoria` | |
| `IntentoAcceso` | `Intentos` | |
| `ConfiguracionWidget` | `Widgets` | Único por `(EmpleadoId, WidgetId)` |
| `Parametro` | `Parametros` | Único por `Clave` |

Total: **27 tablas**.

## Productos

### `Productos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Nombre` | TEXT | `text` | Sí | |
| `CodigoBarras` | TEXT | `text` | Sí | Índice único |
| `PrecioVenta` | INTEGER | `bigint` | Sí | Incluye IVA; en pesados es precio por kilo |
| `StockMinimoEstante` | INTEGER | `integer` | Sí | Dispara alerta de reabastecimiento |
| `Activo` | INTEGER | `boolean` | Sí | Una venta rechaza productos inactivos |
| `CategoriaId` | INTEGER | `integer` | Sí | FK `Categorias` (Restrict) |
| `ProveedorId` | INTEGER | `integer` | Sí | FK `Proveedores` (Restrict) |
| `ImpuestoId` | INTEGER | `integer` | Sí | FK `Impuestos` (Restrict) |
| `Discriminator` | TEXT | `text` | Sí | `ProductoUnitario` o `ProductoPesado` |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | UTC |

Reglas: `ProductoUnitario.CalcularSubtotal` multiplica precio × cantidad; `ProductoPesado` divide por 1000 con redondeo al peso (`GramosPorKilo`). El impuesto es obligatorio al crear el detalle de venta.

### `Categorias`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `Nombre` | TEXT | `text` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

### `Proveedores`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `Nombre` | TEXT | `text` | Sí |
| `Nit` | TEXT | `text` | Sí |
| `Telefono` | TEXT | `text` | Sí |
| `Correo` | TEXT | `text` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

### `Lotes`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `ProductoId` | INTEGER | `integer` | Sí | FK `Productos` (Restrict) |
| `NumeroLote` | TEXT | `text` | Sí | Único junto con `ProductoId` |
| `FechaIngreso` | TEXT | `date` | Sí | |
| `FechaVencimiento` | TEXT | `date` | Sí | `EstaVencido` compara contra la fecha local de hoy |
| `CantidadIngresada` | INTEGER | `integer` | Sí | |
| `CostoUnitario` | INTEGER | `bigint` | Sí | |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

## Inventario

### `StockEstante` y `StockBodega`

Misma estructura; se separan para que el compilador y el esquema impidan mezclar ubicaciones.

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `ProductoId` | INTEGER | Sí | Debe coincidir con `Lote.ProductoId` |
| `LoteId` | INTEGER | Sí | FK `Lotes`; **índice único** por tabla |
| `Cantidad` | INTEGER | Sí | Privada; solo cambia con `Aumentar`/`Disminuir`; nunca negativa |
| `FechaCreacion` | TEXT | Sí | |

### `Movimientos`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `ProductoId` | INTEGER | Sí | |
| `LoteId` | INTEGER | Sí | FK `Lotes` (Restrict) |
| `VentaId` | INTEGER | No | FK `Ventas` (Restrict); nulo en traslados, ingresos y mermas |
| `Tipo` | INTEGER | Sí | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `Cantidad` | INTEGER | Sí | Positiva; el tipo expresa el sentido |
| `Fecha` | TEXT | Sí | |
| `Motivo` | TEXT | Sí | Texto libre; vacío si no aplica |
| `FechaCreacion` | TEXT | Sí | |

### `Mermas`

| Campo | SQLite | Obligatorio |
| :-- | :-- | :--: |
| `Id` | INTEGER | Sí |
| `ProductoId` | INTEGER | Sí |
| `LoteId` | INTEGER | Sí |
| `Cantidad` | INTEGER | Sí |
| `Motivo` | TEXT | Sí |
| `Fecha` | TEXT | Sí |
| `FechaCreacion` | TEXT | Sí |

### `Reabastecimientos`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `ProductoId` | INTEGER | Sí | |
| `CantidadSolicitada` | INTEGER | Sí | |
| `Estado` | INTEGER | Sí | `Pendiente`, `Atendido`, `Cancelado` |
| `FechaSolicitud` | TEXT | Sí | |
| `FechaAtencion` | TEXT | No | Se limpia al cancelar |
| `FechaCreacion` | TEXT | Sí | |

## Ventas y caja

### `Ventas`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `Fecha` | TEXT | Sí | |
| `TurnoCajaId` | INTEGER | Sí | Turno que registró la venta |
| `Estado` | INTEGER | Sí | `Abierta`, `Completada`, `Anulada` |
| `MotivoAnulacion` | TEXT | Sí | Vacío si no fue anulada |
| `ClienteId` | INTEGER | No | FK `Clientes` (Restrict); nulo = consumidor final |
| `ClaveIdempotencia` | TEXT | No | Evita el doble cobro; índice único (valores `NULL` múltiples, ADR-021) |
| `DocumentoAdquirente` | TEXT | Sí | Por defecto `222222222222` (`Venta.DocumentoConsumidorFinal`) |
| `NombreAdquirente` | TEXT | Sí | Por defecto `CONSUMIDOR FINAL`; solo lleva datos del cliente con autorización |
| `FechaCreacion` | TEXT | Sí | |

### `DetalleVenta`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `VentaId` | INTEGER | Sí | FK `Ventas` (Restrict) |
| `ProductoId` | INTEGER | Sí | FK `Productos` (Restrict) |
| `Cantidad` | INTEGER | Sí | Unidades o gramos según el producto |
| `PrecioUnitario` | INTEGER | Sí | Congelado al momento de la venta |
| `Subtotal` | INTEGER | Sí | |
| `PuntosBasicosIva` | INTEGER | Sí | Congelado al momento de la venta |
| `Iva` | INTEGER | Sí | Extraído del precio |
| `FechaCreacion` | TEXT | Sí | |

### `Pago`

TPH: una sola tabla para los tres métodos.

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `VentaId` | INTEGER | Sí | FK `Ventas` (Restrict) |
| `Monto` | INTEGER | Sí | > 0 |
| `Discriminator` | TEXT | Sí | `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia` |
| `UltimosDigitos` | TEXT | Solo tarjeta | Exactamente 4 |
| `Referencia` | TEXT | Solo transferencia | No vacía |
| `FechaCreacion` | TEXT | Sí | |

### `Cajas`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `Nombre` | TEXT | Sí | |
| `Activa` | INTEGER | Sí | No se abre turno en una caja inactiva |
| `FechaCreacion` | TEXT | Sí | |

### `Turnos`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `CajaId` | INTEGER | Sí | FK `Cajas` (Restrict) |
| `EmpleadoId` | INTEGER | Sí | FK `Empleados` (Restrict) |
| `FechaApertura` | TEXT | Sí | |
| `FechaCierre` | TEXT | No | |
| `EfectivoInicial` | INTEGER | Sí | ≥ 0 |
| `EfectivoEsperado` | INTEGER | No | Calculado al cerrar |
| `EfectivoContado` | INTEGER | No | ≥ 0 |
| `Estado` | INTEGER | Sí | `Abierto`, `Cerrado` |
| `FechaCreacion` | TEXT | Sí | |

Regla de arqueo (`TurnoCaja.CalcularEfectivoEsperado`): `EfectivoEsperado = EfectivoInicial + Σ efectivo neto de ventas completadas − Σ efectivo neto de ventas anuladas − TotalRetiros`.

### `VentasEnEspera` y `RetirosCaja`

Ambas entidades tienen su propio `DbSet` en `SigsDbContext`. `VentaEnEspera` guarda `CajaId`, `EmpleadoId`, `IdentificadorCliente`, `ClienteId`, `LineasJson`, `TotalEstimado`, `Nota` y `Recuperada`. `RetiroCaja` registra retiros de efectivo del turno.

## Personal y seguridad

### `Cargos` y `Roles`

| Tabla | Campo | Tipo | Regla |
| :-- | :-- | :-- | :-- |
| `Cargos` | `Id`, `Nombre`, `FechaCreacion` | int, text, timestamp | |
| `Roles` | `Id`, `Nombre`, `Permisos`, `FechaCreacion` | int, text, integer, timestamp | `Nombre` único; `Permisos` es una máscara `Permiso` |

### `Empleados`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `Documento` | TEXT | Sí | Índice único |
| `Nombres`, `Apellidos` | TEXT | Sí | |
| `Correo` | TEXT | Sí | |
| `CorreoVerificado` | INTEGER | Sí | |
| `Activo` | INTEGER | Sí | Al desactivar se invalida la sesión |
| `FechaIngreso` | TEXT | Sí | |
| `SalarioBase` | INTEGER | Sí | |
| `CargoId` | INTEGER | Sí | FK `Cargos` (Restrict) |
| `RolId` | INTEGER | Sí | FK `Roles` (Restrict) |
| `FechaCreacion` | TEXT | Sí | |

### `Usuarios`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `NombreUsuario` | TEXT | Sí | Índice único |
| `HashClave` | TEXT | Sí | BCrypt; nunca texto plano |
| `HashPin` | TEXT | No | BCrypt; PIN numérico de 4+ dígitos |
| `DebeCambiarClave` | INTEGER | Sí | Bloquea los casos de uso hasta el cambio |
| `Bloqueado` | INTEGER | Sí | Se activa al llegar al máximo de intentos |
| `IntentosFallidos` | INTEGER | Sí | |
| `EmpleadoId` | INTEGER | Sí | FK `Empleados` (Restrict) |
| `FechaCreacion` | TEXT | Sí | |

### `Auditoria`

| Campo | SQLite | Obligatorio |
| :-- | :-- | :--: |
| `Id` | INTEGER | Sí |
| `Fecha` | TEXT | Sí |
| `EmpleadoId` | INTEGER | No |
| `Accion` | INTEGER | Sí |
| `Entidad` | TEXT | Sí |
| `EntidadId` | INTEGER | No |
| `Detalle` | TEXT | Sí |
| `FechaCreacion` | TEXT | Sí |

Las acciones están en `AccionAuditoria` (`Supermercado.Entidades/Seguridad.cs`). Los casos de uso registran inicio de sesión, cambios de clave, ventas anuladas, ajustes de inventario, mermas, cambios de rol y tarifa, apertura y cierre de turno, gastos, cambios de parámetro y altas de empleados, productos, clientes y usuarios, entre otras.

### `Intentos`

| Campo | SQLite | Obligatorio |
| :-- | :-- | :--: |
| `Id` | INTEGER | Sí |
| `NombreUsuario` | TEXT | Sí |
| `Fecha` | TEXT | Sí |
| `Exitoso` | INTEGER | Sí |
| `UsoPin` | INTEGER | Sí |
| `FechaCreacion` | TEXT | Sí |

## Clientes

### `Clientes`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `Documento` | TEXT | Sí | Único; 5–15 dígitos |
| `Nombres` | TEXT | Sí | Solo letras (sin números) |
| `Apellidos` | TEXT | No | Solo letras (sin números) |
| `Telefono` | TEXT | No | 7–10 dígitos |
| `Correo` | TEXT | No | Formato de correo |
| `Activo` | INTEGER | Sí | Un cliente inactivo no puede asociarse a ventas |
| `AutorizaTratamientoDatos` | INTEGER | Sí | Ley 1581; si es falso, la venta usa «Consumidor Final» |
| `FechaAutorizacion` | TEXT | No | Momento de la autorización |
| `TerminalAutorizacion` | TEXT | No | Equipo donde se autorizó |
| `FechaCreacion` | TEXT | Sí | UTC |

## Finanzas

### `Impuestos`

TPH para `Iva` y `Retencion`.

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `Nombre` | TEXT | Sí | |
| `PuntosBasicos` | INTEGER | Sí | 1900 = 19 % |
| `Activo` | INTEGER | Sí | |
| `Discriminator` | TEXT | Sí | `Iva` o `Retencion` |
| `BaseMinima` | INTEGER | Solo retención | |
| `FechaCreacion` | TEXT | Sí | |

`Iva.Calcular` extrae el impuesto de un precio que ya lo incluye. `Retencion.Calcular` aplica solo sobre la base mínima.

### `Gastos`

| Campo | SQLite | Obligatorio |
| :-- | :-- | :--: |
| `Id` | INTEGER | Sí |
| `Descripcion` | TEXT | Sí |
| `Monto` | INTEGER | Sí |
| `Fecha` | TEXT | Sí |
| `RegistradoPorEmpleadoId` | INTEGER | Sí |
| `FechaCreacion` | TEXT | Sí |

### `Nominas`

| Campo | SQLite | Obligatorio |
| :-- | :-- | :--: |
| `Id` | INTEGER | Sí |
| `EmpleadoId` | INTEGER | Sí |
| `Anio` | INTEGER | Sí |
| `Mes` | INTEGER | Sí |
| `SalarioBase` | INTEGER | Sí |
| `AuxilioTransporte` | INTEGER | Sí |
| `DeduccionSalud` | INTEGER | Sí |
| `DeduccionPension` | INTEGER | Sí |
| `Neto` | INTEGER | Sí |
| `FechaCalculo` | TEXT | Sí |
| `FechaCreacion` | TEXT | Sí |

## Dashboards

### `Widgets`

| Campo | SQLite | Obligatorio | Regla |
| :-- | :-- | :--: | :-- |
| `Id` | INTEGER | Sí | PK |
| `EmpleadoId` | INTEGER | Sí | |
| `WidgetId` | TEXT | Sí | Único junto con `EmpleadoId` |
| `Posicion` | INTEGER | Sí | Sin duplicados por empleado |
| `Visible` | INTEGER | Sí | |
| `FechaCreacion` | TEXT | Sí | |

## Tabla `Parametro`

| Clave | Valor inicial | Tipo | Consumidor |
| :-- | :-- | :-- | :-- |
| `seguridad.max_intentos` | `5` | entero | `ServicioAutenticacion` |
| `inventario.dias_alerta` | `15` | entero | `ServicioAlertasInventario` |
| `caja.tolerancia_arqueo` | `0` | entero (pesos) | `ServicioCaja` |
| `nomina.salario_minimo` | `0` | entero (pesos) | `ServicioNomina`, `Nomina.Calcular` |
| `nomina.auxilio_transporte` | `0` | entero (pesos) | `ServicioNomina` |
| `nomina.tope_auxilio_smmlv` | `2` | entero | `ServicioNomina` |
| `nomina.salud_pb` | `400` | puntos básicos | `ServicioNomina` |
| `nomina.pension_pb` | `400` | puntos básicos | `ServicioNomina` |
| `integraciones.timeout_seg` | `10` | entero (segundos) | `ServicioTrm` |
| `integraciones.reintentos` | `2` | entero | `ServicioTrm` |
| `integraciones.trm_centavos` | `0` | entero (centavos) | `ServicioTrm` (respaldo) |
| `integraciones.trm_fecha` | `1970-01-01` | fecha ISO | `ServicioTrm` (respaldo) |
| `integraciones.trm_habilitada` | `1` | booleano (`1`/`0`) | `ServicioTrm` |
| `integraciones.codigo_barras_habilitado` | `1` | booleano (`1`/`0`) | `ClienteCodigoBarrasOff` |
| `integraciones.trm_token` | `(vacío)` | secreto | `ClienteTrmDatosGov` (opcional) |
| `integraciones.resend_token` | `(vacío)` | secreto | Envío de recibos (pendiente de integración) |
| `integraciones.resend_remitente` | `recibos@supermercado.local` | correo | Envío de recibos (pendiente) |
| `tributario.responsable_iva` | `1` | booleano (`1`/`0`) | `ServicioVentas`; si es `0`, no extrae IVA |
| `negocio.nombre` / `negocio.nit` / `negocio.direccion` / `negocio.telefono` | `COMPLETAR` | texto | Plantilla de tirilla POS |
| `sistema.carpeta_respaldos` | ruta de datos | ruta | `ServicioRespaldo` |

`Parametro` tiene `Clave` única, `Valor` y `Descripcion`. La semilla agrega solo las claves faltantes y nunca sobrescribe valores editados. `ServicioParametros.GuardarAsync` exige `GestionarPersonal` y audita; `GuardarRespaldoAsync` es interno y atómico. Las claves vigentes están en `ClavesParametro` (`Supermercado.Entidades/Seguridad.cs:120-148`).

## Índices y restricciones

| Tabla | Índice | Único |
| :-- | :-- | :--: |
| `Productos` | `CodigoBarras` | Sí |
| `Lotes` | `(ProductoId, NumeroLote)` | Sí |
| `StockEstante` / `StockBodega` | `LoteId` | Sí |
| `Empleados` | `Documento` | Sí |
| `Clientes` | `Documento` | Sí |
| `Usuarios` | `NombreUsuario` | Sí |
| `Roles` | `Nombre` | Sí |
| `Parametros` | `Clave` | Sí |
| `Widgets` | `(EmpleadoId, WidgetId)` | Sí |
| `Ventas` | `ClaveIdempotencia` | **Sí** (ADR-021) |

Todas las claves foráneas usan `Restrict` y los índices de FK que EF crea por convención no son únicos. Los índices se declaran en `ConfiguracionModelo.DefinirIndicesUnicos` (`Supermercado.Datos/Contexto.cs:105-121`).

## Enumeraciones

| Enum | Valores |
| :-- | :-- |
| `EstadoVenta` | `Abierta`, `Completada`, `Anulada` |
| `EstadoTurno` | `Abierto`, `Cerrado` |
| `TipoMovimiento` | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `UbicacionStock` | `Estante`, `Bodega` |
| `EstadoReabastecimiento` | `Pendiente`, `Atendido`, `Cancelado` |
| `AccionAuditoria` | Ver `Supermercado.Entidades/Seguridad.cs` |
| `Permiso` | 18 banderas: `Ninguno`, `Vender`, `AnularVenta`, `AbrirCerrarTurno`, `VerInventario`, `AjustarInventario`, `Reabastecer`, `GestionarProductos`, `VerReportes`, `VerFinanzas`, `GestionarPersonal`, `GestionarRoles`, `GestionarTarifas`, `VerAuditoria`, `GestionarClientes`, `AutorizarOperacionPos`, `GestionarCompras`, `GestionarContabilidad`, `MantenimientoSistema` |

## Brechas conocidas del modelo

Documentadas, no implementadas en esta entrega:

- Sin migración incremental: `Inicial` se genera desde el modelo, así que una base creada con un esquema anterior debe recrearse.
- `Venta.ClaveIdempotencia` tiene índice único; las bases con duplicados históricos requieren revisión antes de iniciar.
- Sin token de concurrencia en `StockLote`, `Venta` ni `TurnoCaja`.
- Sin índice único parcial que garantice un solo turno abierto por caja; hoy se valida en la lógica.
- SQLite no aplica tipos ni longitudes; los tipos de la columna «SQLite» son los que genera el proveedor.

La contabilidad DIAN de esta rama es un consolidado fiscal calculado a partir de ventas y arqueos, con exportación CSV/PDF desde `ContabilidadDianViewModel`; no hay emisión de Documento Equivalente, CUDE, XML UBL ni QR. La rama `completo` conserva esa funcionalidad como objetivo futuro, fuera del alcance de `main`.
