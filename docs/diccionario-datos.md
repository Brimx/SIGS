# Diccionario de datos

Estado actual: EF Core 10 sobre **SQLite**. Los tipos de la columna «SQLite» son los que genera el proveedor; la columna «PostgreSQL previsto» es el mapeo objetivo cuando se cambie el motor (ver [`despliegue.md`](despliegue.md)).

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
| `Pago` / `PagoEfectivo` / `PagoTarjeta` / `PagoTransferencia` / `PagoPuntos` | `Pago` | TPH con `Discriminator` |
| `Cliente` | `Clientes` | Único por `Documento` |
| `EventoOutbox` | `EventosOutbox` | Índice compuesto `(Procesado, Id)` |
| `Caja` | `Cajas` | |
| `TurnoCaja` | `Turnos` | FK a `Cajas` y `Empleados` |
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

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `ProductoId` | INTEGER | `integer` | Sí | Debe coincidir con `Lote.ProductoId` |
| `LoteId` | INTEGER | `integer` | Sí | FK `Lotes`; **índice único** por tabla |
| `Cantidad` | INTEGER | `integer` | Sí | Privada; solo cambia con `Aumentar`/`Disminuir`; nunca negativa |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Movimientos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `ProductoId` | INTEGER | `integer` | Sí | |
| `LoteId` | INTEGER | `integer` | Sí | FK `Lotes` (Restrict) |
| `VentaId` | INTEGER | `integer` | No | FK `Ventas` (Restrict); nulo en traslados, ingresos y mermas |
| `Tipo` | INTEGER | `smallint` | Sí | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `Cantidad` | INTEGER | `integer` | Sí | Positiva; el tipo expresa el sentido |
| `Fecha` | TEXT | `timestamptz` | Sí | |
| `Motivo` | TEXT | `text` | Sí | Texto libre; vacío si no aplica |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Mermas`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `ProductoId` | INTEGER | `integer` | Sí |
| `LoteId` | INTEGER | `integer` | Sí |
| `Cantidad` | INTEGER | `integer` | Sí |
| `Motivo` | TEXT | `text` | Sí |
| `Fecha` | TEXT | `timestamptz` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

### `Reabastecimientos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `ProductoId` | INTEGER | `integer` | Sí | |
| `CantidadSolicitada` | INTEGER | `integer` | Sí | |
| `Estado` | INTEGER | `smallint` | Sí | `Pendiente`, `Atendido`, `Cancelado` |
| `FechaSolicitud` | TEXT | `timestamptz` | Sí | |
| `FechaAtencion` | TEXT | `timestamptz` | No | Se limpia al cancelar |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

## Ventas y caja

### `Ventas`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Fecha` | TEXT | `timestamptz` | Sí | |
| `TurnoCajaId` | INTEGER | `integer` | Sí | Turno que registró la venta |
| `Estado` | INTEGER | `smallint` | Sí | `Abierta`, `Completada`, `Anulada` |
| `MotivoAnulacion` | TEXT | `text` | Sí | Vacío si no fue anulada |
| `ClienteId` | INTEGER | `integer` | No | FK `Clientes` (Restrict); nulo = consumidor final |
| `PuntosGanados` | INTEGER | `integer` | Sí | Puntos acreditados por esta venta |
| `PuntosRedimidos` | INTEGER | `integer` | Sí | Puntos gastados en esta venta |
| `ClaveIdempotencia` | TEXT | `text` | No | Evita el doble cobro; índice **no único** (ADR-021) |
| `DocumentoAdquirente` | TEXT | `text` | Sí | Documento DIAN; por defecto `222222222222` |
| `NombreAdquirente` | TEXT | `text` | Sí | Por defecto `CONSUMIDOR FINAL`; solo lleva datos del cliente con autorización |
| `Cude` | TEXT | `text` | No | Código Único de Documento Equivalente (SHA-384) |
| `CadenaQr` | TEXT | `text` | No | URL del catálogo VPFE con el CUDE |
| `PrefijoDian` | TEXT | `text` | No | Prefijo autorizado |
| `ConsecutivoDian` | INTEGER | `bigint` | Sí | Consecutivo dentro del rango autorizado |
| `FechaValidacionDian` | TEXT | `timestamptz` | No | Momento de generación local |
| `XmlUbl` | TEXT | `text` | No | XML UBL 2.1 del documento |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `DetalleVenta`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `VentaId` | INTEGER | `integer` | Sí | FK `Ventas` (Restrict) |
| `ProductoId` | INTEGER | `integer` | Sí | FK `Productos` (Restrict) |
| `Cantidad` | INTEGER | `integer` | Sí | Unidades o gramos según el producto |
| `PrecioUnitario` | INTEGER | `bigint` | Sí | Congelado al momento de la venta |
| `Subtotal` | INTEGER | `bigint` | Sí | |
| `PuntosBasicosIva` | INTEGER | `integer` | Sí | Congelado al momento de la venta |
| `Iva` | INTEGER | `bigint` | Sí | Extraído del precio |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Pago`

TPH: una sola tabla para los cuatro métodos.

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `VentaId` | INTEGER | `integer` | Sí | FK `Ventas` (Restrict) |
| `Monto` | INTEGER | `bigint` | Sí | > 0 |
| `Discriminator` | TEXT | `text` | Sí | `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia`, `PagoPuntos` |
| `UltimosDigitos` | TEXT | `text` | Solo tarjeta | Exactamente 4 |
| `Referencia` | TEXT | `text` | Solo transferencia | No vacía |
| `Puntos` | INTEGER | `integer` | Solo puntos | > 0 |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Cajas`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Nombre` | TEXT | `text` | Sí | |
| `Activa` | INTEGER | `boolean` | Sí | No se abre turno en una caja inactiva |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Turnos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `CajaId` | INTEGER | `integer` | Sí | FK `Cajas` (Restrict) |
| `EmpleadoId` | INTEGER | `integer` | Sí | FK `Empleados` (Restrict) |
| `FechaApertura` | TEXT | `timestamptz` | Sí | |
| `FechaCierre` | TEXT | `timestamptz` | No | |
| `EfectivoInicial` | INTEGER | `bigint` | Sí | ≥ 0 |
| `EfectivoEsperado` | INTEGER | `bigint` | No | Calculado al cerrar |
| `EfectivoContado` | INTEGER | `bigint` | No | ≥ 0 |
| `Estado` | INTEGER | `smallint` | Sí | `Abierto`, `Cerrado` |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

Regla de arqueo: `EfectivoEsperado = EfectivoInicial + Σ efectivo neto de ventas completadas − Σ efectivo neto de ventas anuladas`.

## Personal y seguridad

### `Cargos` y `Roles`

| Tabla | Campo | Tipo | Regla |
| :-- | :-- | :-- | :-- |
| `Cargos` | `Id`, `Nombre`, `FechaCreacion` | int, text, timestamp | |
| `Roles` | `Id`, `Nombre`, `Permisos`, `FechaCreacion` | int, text, integer, timestamp | `Nombre` único; `Permisos` es una máscara `Permiso` |

### `Empleados`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Documento` | TEXT | `text` | Sí | Índice único |
| `Nombres`, `Apellidos` | TEXT | `text` | Sí | |
| `Correo` | TEXT | `text` | Sí | |
| `CorreoVerificado` | INTEGER | `boolean` | Sí | |
| `Activo` | INTEGER | `boolean` | Sí | Al desactivar se invalida la sesión |
| `FechaIngreso` | TEXT | `date` | Sí | |
| `SalarioBase` | INTEGER | `bigint` | Sí | |
| `CargoId` | INTEGER | `integer` | Sí | FK `Cargos` (Restrict) |
| `RolId` | INTEGER | `integer` | Sí | FK `Roles` (Restrict) |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Usuarios`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `NombreUsuario` | TEXT | `text` | Sí | Índice único |
| `HashClave` | TEXT | `text` | Sí | BCrypt; nunca texto plano |
| `HashPin` | TEXT | `text` | No | BCrypt; PIN numérico de 4+ dígitos |
| `DebeCambiarClave` | INTEGER | `boolean` | Sí | Bloquea los casos de uso hasta el cambio |
| `Bloqueado` | INTEGER | `boolean` | Sí | Se activa al llegar al máximo de intentos |
| `IntentosFallidos` | INTEGER | `integer` | Sí | |
| `EmpleadoId` | INTEGER | `integer` | Sí | FK `Empleados` (Restrict) |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

### `Auditoria`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `Fecha` | TEXT | `timestamptz` | Sí |
| `EmpleadoId` | INTEGER | `integer` | No |
| `Accion` | INTEGER | `smallint` | Sí |
| `Entidad` | TEXT | `text` | Sí |
| `EntidadId` | INTEGER | `integer` | No |
| `Detalle` | TEXT | `text` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

Acciones (32): `InicioSesion`, `InicioSesionFallido`, `CambioClave`, `VentaAnulada`, `AjusteInventario`, `Merma`, `CambioRol`, `CambioTarifa`, `AperturaTurno`, `CierreTurno`, `EmpleadoCreado`, `EmpleadoDesactivado`, `GastoRegistrado`, `ParametroCambiado`, `UsuarioCreado`, `ProductoModificado`, `PrecioCambiado`, `CategoriaCreada`, `CategoriaModificada`, `ProveedorCreado`, `ProveedorModificado`, `CajaCreada`, `CajaModificada`, `CargoCreado`, `CargoModificado`, `RolCreado`, `RolModificado`, `UsuarioDesbloqueado`, `ClaveRestablecida`, `RespaldoBaseDatos`, `ClienteCreado`, `ClienteModificado`.

### `Intentos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `NombreUsuario` | TEXT | `text` | Sí |
| `Fecha` | TEXT | `timestamptz` | Sí |
| `Exitoso` | INTEGER | `boolean` | Sí |
| `UsoPin` | INTEGER | `boolean` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

## Clientes

### `Clientes`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Documento` | TEXT | `text` | Sí | Único; 5–15 dígitos |
| `Nombres` | TEXT | `text` | Sí | Solo letras (sin números) |
| `Apellidos` | TEXT | `text` | No | Solo letras (sin números) |
| `Telefono` | TEXT | `text` | No | 7–10 dígitos |
| `Correo` | TEXT | `text` | No | Formato de correo |
| `Activo` | INTEGER | `boolean` | Sí | Un cliente inactivo no puede asociarse a ventas |
| `PuntosAcumulados` | INTEGER | `integer` | Sí | ≥ 0 |
| `AutorizaTratamientoDatos` | INTEGER | `boolean` | Sí | Ley 1581; si es falso, la venta usa «Consumidor Final» |
| `FechaAutorizacion` | TEXT | `timestamptz` | No | Momento de la autorización |
| `TerminalAutorizacion` | TEXT | `text` | No | Equipo donde se autorizó |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | UTC |

## Finanzas

### `Impuestos`

TPH para `Iva` y `Retencion`.

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `Nombre` | TEXT | `text` | Sí | |
| `PuntosBasicos` | INTEGER | `integer` | Sí | 1900 = 19 % |
| `Activo` | INTEGER | `boolean` | Sí | |
| `Discriminator` | TEXT | `text` | Sí | `Iva` o `Retencion` |
| `BaseMinima` | INTEGER | `bigint` | Solo retención | |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

`Iva.Calcular` extrae el impuesto de un precio que ya lo incluye. `Retencion.Calcular` aplica solo sobre la base mínima.

### `Gastos`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `Descripcion` | TEXT | `text` | Sí |
| `Monto` | INTEGER | `bigint` | Sí |
| `Fecha` | TEXT | `timestamptz` | Sí |
| `RegistradoPorEmpleadoId` | INTEGER | `integer` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

### `Nominas`

| Campo | SQLite | PostgreSQL previsto | Obligatorio |
| :-- | :-- | :-- | :--: |
| `Id` | INTEGER | `integer identity` | Sí |
| `EmpleadoId` | INTEGER | `integer` | Sí |
| `Anio` | INTEGER | `integer` | Sí |
| `Mes` | INTEGER | `integer` | Sí |
| `SalarioBase` | INTEGER | `bigint` | Sí |
| `AuxilioTransporte` | INTEGER | `bigint` | Sí |
| `DeduccionSalud` | INTEGER | `bigint` | Sí |
| `DeduccionPension` | INTEGER | `bigint` | Sí |
| `Neto` | INTEGER | `bigint` | Sí |
| `FechaCalculo` | TEXT | `timestamptz` | Sí |
| `FechaCreacion` | TEXT | `timestamptz` | Sí |

## Dashboards

### `Widgets`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `EmpleadoId` | INTEGER | `integer` | Sí | |
| `WidgetId` | TEXT | `text` | Sí | Único junto con `EmpleadoId` |
| `Posicion` | INTEGER | `integer` | Sí | Sin duplicados por empleado |
| `Visible` | INTEGER | `boolean` | Sí | |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | |

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
| `integraciones.supabase_habilitado` | `1` | booleano (`1`/`0`) | `ClienteSincronizacionSupabase` |
| `integraciones.supabase_url` | `(vacío)` | URL HTTPS | `ClienteSincronizacionSupabase` |
| `integraciones.supabase_key` | `(vacío)` | secreto | `ClienteSincronizacionSupabase`; usar `anon`, nunca `service_role` en el cliente |
| `integraciones.trm_token` | `(vacío)` | secreto | `ClienteTrmDatosGov` (opcional) |
| `integraciones.resend_token` | `(vacío)` | secreto | Envío de recibos (pendiente de integración) |
| `integraciones.resend_remitente` | `recibos@supermercado.local` | correo | Envío de recibos (pendiente) |
| `puntos.pesos_por_punto` | `1000` | entero (pesos) | `ServicioVentas`; `0` desactiva la acumulación |
| `puntos.valor_punto_pesos` | `10` | entero (pesos) | `ServicioVentas`; `0` desactiva la redención |
| `tributario.responsable_iva` | `1` | booleano (`1`/`0`) | `ServicioVentas`; si es `0`, no extrae IVA |
| `dian.habilitado` | `1` | booleano (`1`/`0`) | `ServicioDian` |
| `dian.resolucion_numero` | `18764102352817` | texto | `ServicioDian` |
| `dian.resolucion_fecha` | `2024-01-15` | fecha ISO | `ServicioDian` |
| `dian.resolucion_vigencia_hasta` | `2026-12-31` | fecha ISO | `ServicioDian` |
| `dian.prefijo` | `POS` | texto | `ServicioDian` |
| `dian.rango_desde` | `1` | entero | `ServicioDian` |
| `dian.rango_hasta` | `500000` | entero | `ServicioDian` |
| `dian.clave_tecnica` | `(semilla de ejemplo)` | secreto | `GeneradorCudeDian`; reemplazar por la asignada por la DIAN |
| `dian.siguiente_consecutivo` | `1` | entero | `ServicioDian` |
| `dian.tipo_ambiente` | `2` | `1` producción / `2` pruebas | `GeneradorCudeDian` |
| `negocio.nombre` / `negocio.nit` / `negocio.direccion` / `negocio.telefono` | `COMPLETAR` | texto | Ticket y XML DIAN |
| `sistema.carpeta_respaldos` | ruta de datos | ruta | `ServicioRespaldo` |

`Parametro` tiene `Clave` única, `Valor` y `Descripcion`. La semilla agrega solo las claves faltantes y nunca sobrescribe valores editados. `ServicioParametros.GuardarAsync` exige `GestionarPersonal` y audita; `GuardarRespaldoAsync` es interno y atómico.

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
| `Ventas` | `ClaveIdempotencia` | **No** (pendiente, ADR-021) |
| `EventosOutbox` | `(Procesado, Id)` | No |

Todas las claves foráneas usan `Restrict` y los índices de FK que EF crea por convención no son únicos.

## Enumeraciones

| Enum | Valores |
| :-- | :-- |
| `EstadoVenta` | `Abierta`, `Completada`, `Anulada` |
| `EstadoTurno` | `Abierto`, `Cerrado` |
| `TipoMovimiento` | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `UbicacionStock` | `Estante`, `Bodega` |
| `EstadoReabastecimiento` | `Pendiente`, `Atendido`, `Cancelado` |
| `AccionAuditoria` | Ver lista en `Auditoria` |
| `Permiso` | `Vender`, `AnularVenta`, `AbrirCerrarTurno`, `VerInventario`, `AjustarInventario`, `Reabastecer`, `GestionarProductos`, `VerReportes`, `VerFinanzas`, `GestionarPersonal`, `GestionarRoles`, `GestionarTarifas`, `VerAuditoria`, `GestionarClientes` |



## Sincronización

### `EventosOutbox`

| Campo | SQLite | PostgreSQL previsto | Obligatorio | Regla |
| :-- | :-- | :-- | :--: | :-- |
| `Id` | INTEGER | `integer identity` | Sí | PK |
| `TipoEvento` | TEXT | `text` | Sí | `VentaCompletada`, `VentaAnulada` |
| `EntidadId` | TEXT | `text` | Sí | ID de la entidad afectada |
| `PayloadJson` | TEXT | `jsonb` | Sí | Carga JSON serializada de la transacción |
| `Procesado` | INTEGER | `boolean` | Sí | `0` pendiente, `1` replicado a la nube |
| `FechaProcesado` | TEXT | `timestamptz` | No | Fecha UTC de confirmación en Supabase |
| `Reintentos` | INTEGER | `integer` | Sí | Cantidad de intentos fallidos |
| `UltimoError` | TEXT | `text` | No | Mensaje de excepción o código HTTP |
| `FechaCreacion` | TEXT | `timestamptz` | Sí | UTC |

## Brechas conocidas del modelo

Documentadas, no implementadas en esta entrega:

- Sin migración incremental para `Clientes`, `EventosOutbox`, los campos DIAN ni las columnas de puntos en `Ventas`: `Inicial` se genera desde el modelo, así que bases anteriores deben recrearse (ADR-018, ADR-022).
- `Venta.ClaveIdempotencia` tiene índice **no único**: dos solicitudes concurrentes pueden duplicar la clave (ADR-021).
- Sin token de concurrencia (`xmin` en PostgreSQL) en `StockLote`, `Venta` ni `TurnoCaja`.
- Sin índice único parcial que garantice un solo turno abierto por caja; hoy se valida en la lógica.
- Los campos `Cude`, `CadenaQr` y `XmlUbl` se generan localmente; no hay transmisión ni firma DIAN.
- `ProductoId` en stock, movimientos, mermas, reabastecimientos y nóminas no tiene FK explícita; se valida en los servicios.
- SQLite no aplica tipos ni longitudes; PostgreSQL sí. El mapeo previsto está en la columna correspondiente.
