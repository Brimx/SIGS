# Catálogo de código

Inventario curado de módulos, tipos, contratos, constructores, métodos públicos y registro de dependencias. Para el listado exhaustivo de símbolos existe [`scripts/inventario-codigo.sh`](../scripts/inventario-codigo.sh), que regenera un anexo con todos los tipos y métodos públicos.

## 1. Proyectos

| Proyecto | Responsabilidad | Referencias |
| :-- | :-- | :-- |
| `Supermercado.Entidades` | Dominio, enums, excepciones y contratos | Ninguna |
| `Supermercado.Datos` | `SigsDbContext`, configuraciones EF, repositorios, semilla | Entidades |
| `Supermercado.Logica` | Casos de uso, seguridad, cálculo, integraciones y widgets | Datos, Entidades |
| `Supermercado.GUI` | Avalonia + MVVM (shell inicial) | Lógica, Entidades |
| `Supermercado.Pruebas` | Pruebas xUnit y falsos en memoria | Lógica, Entidades |

## 2. Módulos

| Módulo | Carpetas | Servicios |
| :-- | :-- | :-- |
| Productos | `Entidades/Productos`, `Logica/Productos` | `ServicioProductos`, `ServicioBusquedaProductos`, `ServicioMantenimientoProductos`, `ServicioCatalogos` |
| Inventario | `Entidades/Inventario`, `Logica/Inventario` | `ServicioInventario`, `ServicioConsultaInventario`, `ServicioMermas`, `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| Ventas y caja | `Entidades/Ventas`, `Logica/Ventas` | `ServicioVentas`, `ServicioCaja` |
| Clientes | `Entidades/Clientes`, `Logica/Clientes` | `ServicioClientes` |
| Personal | `Entidades/Personal`, `Logica/Personal` | `ServicioEmpleados`, `ServicioUsuarios`, `ServicioEstructura` |
| Finanzas | `Entidades/Finanzas`, `Logica/Finanzas` | `ServicioFinanzas`, `ServicioImpuestos`, `ServicioNomina` |
| Seguridad | `Entidades/Seguridad`, `Logica/Seguridad` | `ServicioAutenticacion`, `ServicioParametros`, `ServicioAuditoria`, `ServicioConsultaAuditoria`, `ServicioInicializacion`, `SesionActual` |
| Dashboards | `Entidades/Dashboards`, `Logica/Dashboards` | `ServicioDashboard` + 9 widgets |
| DIAN | `Logica/Dian` | `ServicioDian`, `GeneradorCudeDian`, `GeneradorUblDian` |
| Sincronización | `Entidades/Sincronizacion`, `Logica/Servicios` | `ClienteSincronizacionSupabase`, `EventoOutbox` |
| Integraciones | `Logica/Servicios` | `ServicioTrm`, `ClienteTrmDatosGov`, `ClienteCodigoBarrasOff` |
| Reportes | `Logica/Reportes` | `GeneradorTicketQuestPdf`, `ServicioReportesQuestPdf` |
| Comunes | `Entidades/Base`, `Logica/Comunes` | `Dinero`, `ExtensionesReloj`, `Validador`, `TraductorExcepciones` |

## 3. Entidades

### Base

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `EntidadBase` (abstracta) | Identidad y auditoría de creación | `Id`, `FechaCreacion` |
| `Dinero` (estática) | Aritmética monetaria entera | `PuntosBasicosCien = 10000`, `DividirRedondeando`, `Porcentaje` |

### Productos

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Producto` (abstracta) | Producto vendible con precio, impuesto y mínimos | `CalcularSubtotal`, `UnidadMedida`, FKs `CategoriaId`, `ProveedorId`, `ImpuestoId` |
| `ProductoUnitario` (sealed) | Precio × cantidad | `CalcularSubtotal`; `UnidadMedida = "und"` |
| `ProductoPesado` (sealed) | Precio por kilo, cantidad en gramos | `GramosPorKilo = 1000`; redondeo con `Dinero` |
| `Categoria` | Agrupación de productos | `Nombre` |
| `Proveedor` | Datos del proveedor | `Nombre`, `Nit`, `Telefono`, `Correo` |
| `Lote` | Mercancía con vencimiento y costo | `EstaVencido`, `DiasParaVencer`, `NumeroLote`, `CostoUnitario` |

### Inventario

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `StockLote` (abstracta) | Cantidad por lote con invariantes | `Aumentar`, `Disminuir`; `Cantidad` privada |
| `StockEstante` (sealed) | Stock disponible para venta | Hereda `StockLote` |
| `StockBodega` (sealed) | Stock de reserva | Hereda `StockLote` |
| `MovimientoInventario` | Bitácora de entradas y salidas | `Tipo`, `Cantidad`, `LoteId`, `VentaId`, `Motivo` |
| `Merma` | Pérdida de mercancía | `ProductoId`, `LoteId`, `Cantidad`, `Motivo` |
| `Reabastecimiento` | Solicitud de reposición | `Atender(DateTime)`, `Cancelar()` |
| `TipoMovimiento` | Sentido del movimiento | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `UbicacionStock` | Ubicación física | `Estante`, `Bodega` |
| `EstadoReabastecimiento` | Ciclo de la solicitud | `Pendiente`, `Atendido`, `Cancelado` |

### Ventas y caja

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Venta` | Venta con detalles, pagos, estado y datos legales/DIAN | `AgregarProducto`, `AgregarPago`, `Cerrar`, `Anular`; `Total`, `TotalIva`, `Cambio`, `EfectivoNeto`; `ClienteId`, `PuntosGanados`, `PuntosRedimidos`, `ClaveIdempotencia`; `DocumentoAdquirente`, `NombreAdquirente`, `Cude`, `CadenaQr`, `PrefijoDian`, `ConsecutivoDian`, `XmlUbl` |
| `Cliente` | Cliente del programa de puntos y Habeas Data | `Documento`, `PuntosAcumulados`, `AcumularPuntos`, `RedimirPuntos`, `RevertirVenta`, `AutorizarHabeasData`, `RevocarHabeasData`; `AutorizaTratamientoDatos` |
| `PagoPuntos` | Pago con puntos | `Puntos`; `Monto` = puntos × valor del punto |
| `DetalleVenta` | Línea con precio e IVA congelados | `Crear(Producto, int)` |
| `Pago` (abstracta) | Pago y validación base | `Monto`, `Metodo`, `Validar` |
| `PagoEfectivo` (sealed) | Pago en efectivo | `Metodo = "Efectivo"` |
| `PagoTarjeta` (sealed) | Pago con tarjeta | `UltimosDigitos`, `DigitosRequeridos = 4` |
| `PagoTransferencia` (sealed) | Transferencia bancaria | `Referencia` |
| `Caja` | Punto de atención | `Nombre`, `Activa` |
| `TurnoCaja` | Turno con arqueo | `Abrir`, `CalcularEfectivoEsperado`, `Cerrar`; `Diferencia` |
| `EstadoVenta` | Estado de la venta | `Abierta`, `Completada`, `Anulada` |
| `EstadoTurno` | Estado del turno | `Abierto`, `Cerrado` |

### Personal

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Empleado` | Persona con cargo, rol y salario | `NombreCompleto`, `Desactivar()` |
| `Cargo` | Cargo organizacional | `Nombre` |
| `Rol` | Conjunto de permisos | `Tiene(Permiso)`; `Permisos` es `[Flags]` |
| `Usuario` | Credenciales y bloqueo | `RegistrarFallo`, `RegistrarExito`, `Desbloquear`, `CambiarClave` |
| `NombresRol` (estática) | Nombres canónicos | `Admin`, `Gerente`, `Cajero`, `Bodeguero` |

### Finanzas

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Impuesto` (abstracta) | Tarifa parametrizable | `PuntosBasicos`, `Porcentaje`, `Calcular` |
| `Iva` (sealed) | IVA incluido en el precio | `Calcular` extrae el impuesto |
| `Retencion` (sealed) | Retención con base mínima | `BaseMinima`, `Calcular` |
| `Gasto` | Egreso operativo | `Descripcion`, `Monto`, `RegistradoPorEmpleadoId` |
| `Nomina` | Liquidación básica mensual | `Calcular(...)` estático; valida año, mes y parámetros |
| `ParametrosNomina` (record) | Entrada del cálculo | `SalarioMinimo`, `AuxilioTransporte`, `TopeAuxilioEnSmmlv`, `SaludPuntosBasicos`, `PensionPuntosBasicos` |

### Seguridad y configuración

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Permiso` (`[Flags] long`) | Permisos del sistema | `Vender`, `AnularVenta`, `AbrirCerrarTurno`, `VerInventario`, `AjustarInventario`, `Reabastecer`, `GestionarProductos`, `VerReportes`, `VerFinanzas`, `GestionarPersonal`, `GestionarRoles`, `GestionarTarifas`, `VerAuditoria`, `GestionarClientes` |
| `AccionAuditoria` | Acciones auditables | 32 valores (login, venta anulada, gasto, parámetro, cliente, catálogos, respaldo, etc.) |
| `RegistroAuditoria` | Entrada de auditoría | `Fecha`, `EmpleadoId`, `Accion`, `Entidad`, `EntidadId`, `Detalle` |
| `IntentoAcceso` | Intento de login/PIN | `NombreUsuario`, `Exitoso`, `UsoPin` |
| `Parametro` | Valor operativo editable | `Clave`, `Valor`, `Descripcion` |
| `ConfiguracionWidget` | Disposición de dashboard | `EmpleadoId`, `WidgetId`, `Posicion`, `Visible` |
| `ClavesParametro` (estática) | Claves de parámetros | 38 constantes documentadas en el diccionario (seguridad, nómina, integraciones, puntos, DIAN y negocio) |

### Excepciones

| Tipo | Cuándo se lanza |
| :-- | :-- |
| `ValidacionException` | Regla de negocio incumplida |
| `VentaInvalidaException` | Venta, pago o anulación inválidos |
| `StockInsuficienteException` | No alcanza el stock o el lote no existe |
| `TurnoInvalidoException` | Apertura/cierre de turno inválido |
| `AccesoDenegadoException` | Falta el permiso |
| `CredencialesInvalidasException` | Usuario o secreto incorrecto |
| `CuentaBloqueadaException` | Cuenta bloqueada por intentos |
| `SesionNoIniciadaException` | Se usó `EmpleadoId` sin sesión |
| `DebeCambiarClaveException` | Cambio de clave pendiente |
| `ParametroFaltanteException` | Falta un parámetro o es inválido |
| `ServicioNoConfiguradoException` | Cliente externo no reemplazado |

## 4. Contratos (`Entidades/Contratos`)

| Interfaz | Implementación | Métodos |
| :-- | :-- | :-- |
| `IRepositorio<T>` | `Repositorio<T>` | `ObtenerPorIdAsync`, `ObtenerTodosAsync`, `AgregarAsync`, `Actualizar` |
| `IProductoRepositorio` | `ProductoRepositorio` | `ObtenerPorCodigoBarrasAsync` |
| `ILoteRepositorio` | `LoteRepositorio` | `ObtenerPorVencerAsync` |
| `IStockRepositorio<T>` | `StockRepositorio<T>` | `ObtenerPorProductoAsync`, `ObtenerPorLoteAsync`, `ObtenerTotalesPorProductoAsync` |
| `IMovimientoRepositorio` | `MovimientoRepositorio` | `ObtenerSalidasDeVentaAsync` |
| `IVentaRepositorio` | `VentaRepositorio` | `ObtenerPorTurnoAsync`, `ObtenerPorRangoAsync`, `ObtenerPorClaveIdempotenciaAsync` |
| `IClienteRepositorio` | `ClienteRepositorio` | `ObtenerPorDocumentoAsync` |
| `IEventoOutboxRepositorio` | `EventoOutboxRepositorio` | `ObtenerPendientesAsync(limite)` |
| `ITurnoCajaRepositorio` | `TurnoCajaRepositorio` | `ObtenerAbiertoPorEmpleadoAsync`, `ObtenerAbiertoPorCajaAsync` |
| `IUsuarioRepositorio` | `UsuarioRepositorio` | `ObtenerPorNombreAsync` (incluye `Empleado.Rol`) |
| `IRolRepositorio` | `RolRepositorio` | `ObtenerPorNombreAsync` |
| `IParametroRepositorio` | `ParametroRepositorio` | `ObtenerPorClaveAsync` |
| `IGastoRepositorio` | `GastoRepositorio` | `ObtenerPorRangoAsync` |
| `IReabastecimientoRepositorio` | `ReabastecimientoRepositorio` | `ObtenerPendientesAsync` |
| `IConfiguracionWidgetRepositorio` | `ConfiguracionWidgetRepositorio` | `ObtenerPorEmpleadoAsync` |
| `IUnidadDeTrabajo` | `UnidadDeTrabajo` | `GuardarAsync` |
| `IInicializadorBaseDatos` | `InicializadorBaseDatos` | `CrearYSembrarAsync` |

## 5. Capa Datos

| Tipo | Responsabilidad |
| :-- | :-- |
| `SigsDbContext` | DbSets, configuración del modelo y `FechaCreacion` en `SaveChanges`/`SaveChangesAsync` |
| `ConfiguracionModelo` | TPH, índices únicos y `DeleteBehavior.Restrict` |
| `ExtensionesDatos.AgregarDatos` | Registra `TimeProvider`, `UseSqlite`, repositorios y unidad de trabajo |
| `RutasDatos` | Ruta `%LOCALAPPDATA%/SIGS/sigs.db` (o `~/.local/share`) |
| `InicializadorBaseDatos` | `Migrate` si hay migraciones; si no, `EnsureCreated`; siembra idempotente |
| `ValoresIniciales` | Roles, cargos, impuestos, cajas y parámetros de arranque |
| `Repositorio<T>` | CRUD EF; `Actualizar` marca la entidad |
| `UnidadDeTrabajo` | `SaveChangesAsync` como única transacción del caso de uso |

Repositorios específicos: `ProductoRepositorio`, `LoteRepositorio`, `StockRepositorio<T>`, `MovimientoRepositorio`, `VentaRepositorio`, `TurnoCajaRepositorio`, `ClienteRepositorio`, `EventoOutboxRepositorio`, `UsuarioRepositorio`, `RolRepositorio`, `ParametroRepositorio`, `GastoRepositorio`, `ReabastecimientoRepositorio`, `ConfiguracionWidgetRepositorio`.

`SigsDbContext` incluye `Clientes` y `EventosOutbox`; `ConfiguracionModelo` mantiene el índice **no único** de `Venta.ClaveIdempotencia` y el índice `(Procesado, Id)` del Outbox.

## 6. Capa Lógica

### Seguridad

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `SesionActual` (singleton) | — | `Tiene`, `Exigir`, `Iniciar`, `Cerrar` | `AccesoDenegado`, `SesionNoIniciada`, `DebeCambiarClave` |
| `HasheadorBCrypt` | — | `Hashear`, `Verificar` | BCrypt con salt |
| `ServicioAutenticacion` | `IUsuarioRepositorio`, `IRepositorio<IntentoAcceso>`, `IServicioAuditoria`, `IServicioParametros`, `IHasheadorClave`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `IniciarSesionAsync`, `IniciarSesionConPinAsync`, `CambiarClaveAsync`, `AsignarPinAsync`, `CerrarSesion` | `CredencialesInvalidas`, `CuentaBloqueada`, `SesionNoIniciada`; constantes `LongitudMinimaClave = 8`, `LongitudMinimaPin = 4` |
| `ServicioParametros` | `IParametroRepositorio`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo` | `ObtenerTextoAsync`, `ObtenerEnteroAsync`, `ObtenerLongAsync`, `GuardarAsync`, `GuardarRespaldoAsync` | `GuardarAsync` exige `GestionarPersonal` y audita; el respaldo es interno y atómico |
| `ServicioAuditoria` | `IRepositorio<RegistroAuditoria>`, `ISesionActual`, `TimeProvider` | `RegistrarAsync(accion, entidad, entidadId?, detalle)` | No exige permiso |
| `ServicioInicializacion` | `IInicializadorBaseDatos`, `IRepositorio<Usuario>`, `IRepositorio<Empleado>`, `IRepositorio<Cargo>`, `IRolRepositorio`, `IHasheadorClave`, `IUnidadDeTrabajo`, `TimeProvider` | `InicializarAsync(claveAdminInicial)` | `ValidacionException` si faltan clave, rol o cargo; `UsuarioAdministrador = "admin"` |

### Personal

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `ServicioEmpleados` | `IRepositorio<Empleado>`, `IRepositorio<Rol>`, `IUsuarioRepositorio`, `IVerificadorCorreo`, `IHasheadorClave`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `CrearAsync`, `EditarAsync`, `CambiarRolAsync`, `DesactivarAsync`, `CrearUsuarioAsync` | `GestionarPersonal`; `CambiarRol` exige `GestionarRoles`; si cambia el rol exige además `GestionarRoles`; audita creación/desactivación/cambio de rol |

### Productos

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `ServicioProductos` | `IProductoRepositorio`, `IRepositorio<Lote>`, `IServicioInventario`, `IConsultaCodigoBarras`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `RegistrarAsync`, `BuscarPorCodigoBarrasAsync`, `ConsultarExternoAsync`, `RegistrarIngresoLoteAsync` | `GestionarProductos`; solo captura errores de red/timeout en la consulta externa |

### Inventario

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `ServicioInventario` | `IStockRepositorio<StockEstante>`, `IStockRepositorio<StockBodega>`, `IMovimientoRepositorio`, `IRepositorio<Merma>`, `IEstrategiaSalida`, `TimeProvider` | `DescontarVentaAsync`, `ReintegrarVentaAsync`, `TrasladarAEstanteAsync`, `RegistrarIngresoAsync`, `RegistrarMermaAsync` | `StockInsuficiente`, `ValidacionException`; no guarda (lo hace el caso de uso) |
| `ServicioMermas` | `IServicioInventario`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo` | `RegistrarAsync(productoId, loteId, ubicacion, cantidad, motivo)` | `AjustarInventario`; exige motivo |
| `ServicioAlertasInventario` | `IProductoRepositorio`, `IStockRepositorio<StockEstante>`, `ILoteRepositorio`, `IServicioParametros`, `ISesionActual`, `TimeProvider` | `ObtenerBajoMinimoAsync`, `ObtenerLotesPorVencerAsync` | `VerInventario`; devuelve `AlertaStockMinimo` |
| `ServicioReabastecimiento` | `IReabastecimientoRepositorio`, `IServicioAlertasInventario`, `IServicioInventario`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `GenerarSolicitudesAsync`, `ObtenerPendientesAsync`, `AtenderAsync` | `Reabastecer`; al atender traslada de bodega a estante |
| `SalidaFefo` | — | `Ordenar<T>(stocks, hoy)` | Excluye vencidos; desempata por `LoteId` |
| `PlanDeSalida` (estática) | — | `Repartir<T>(filas, cantidad)` | `StockInsuficiente` si no alcanza |

### Ventas y caja

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `ServicioClientes` | `IClienteRepositorio`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo` | `CrearAsync`, `EditarAsync`, `DesactivarAsync`, `BuscarPorDocumentoAsync`, `ObtenerTodosAsync` | `GestionarClientes` (buscar: o `Vender`); valida con `Validador` |
| `ServicioVentas` | `IProductoRepositorio`, `IVentaRepositorio`, `ITurnoCajaRepositorio`, `IServicioInventario`, `IClienteRepositorio`, `IServicioParametros`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider`, `IEventoOutboxRepositorio?`, `IServicioDian?` | `RegistrarVentaAsync(SolicitudVenta)`, `AnularVentaAsync(ventaId, motivo)` | `Vender` / `AnularVenta`; `TurnoInvalido`, `VentaInvalida`; idempotencia, puntos, Habeas Data y emisión DIAN local |
| `ServicioCaja` | `ITurnoCajaRepositorio`, `IVentaRepositorio`, `IRepositorio<Caja>`, `IServicioParametros`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `AbrirTurnoAsync`, `CerrarTurnoAsync`, `ObtenerTurnoActualAsync` | `AbrirCerrarTurno`; valida caja existente y activa; devuelve `ResultadoArqueo` |

### Finanzas

| Clase | Constructor | Métodos públicos | Permiso / excepciones |
| :-- | :-- | :-- | :-- |
| `ServicioFinanzas` | `IVentaRepositorio`, `IGastoRepositorio`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `RegistrarGastoAsync`, `ObtenerResumenAsync` | `VerFinanzas`; audita gastos |
| `ServicioImpuestos` | `IRepositorio<Impuesto>`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo` | `ObtenerActivosAsync`, `CambiarTarifaAsync` | `GestionarTarifas`; tarifa 0–100 %; audita |
| `ServicioNomina` | `IRepositorio<Empleado>`, `IRepositorio<Nomina>`, `IServicioParametros`, `ISesionActual`, `IUnidadDeTrabajo`, `TimeProvider` | `CalcularAsync(empleadoId, anio, mes)` | `GestionarPersonal`; rechaza duplicados del periodo |

### Dashboards

| Clase | Constructor | Métodos públicos |
| :-- | :-- | :-- |
| `ServicioDashboard` | `IEnumerable<IWidget>`, `IConfiguracionWidgetRepositorio`, `ISesionActual`, `IUnidadDeTrabajo` | `ObtenerDisponibles`, `ObtenerDisposicionAsync`, `AgregarAsync`, `QuitarAsync`, `ReordenarAsync` |
| `VentasDelDiaWidget` | `IVentaRepositorio`, `TimeProvider` | `ObtenerDatosAsync`; permiso `VerReportes` |
| `ReabastecimientosPendientesWidget` | `IReabastecimientoRepositorio` | `ObtenerDatosAsync`; permiso `VerInventario` |
| `TrmDelDiaWidget` | `IServicioTrm` | `ObtenerDatosAsync`; permiso `Ninguno` |

### Integraciones y reportes

| Clase | Constructor | Métodos públicos | Notas |
| :-- | :-- | :-- | :-- |
| `ServicioTrm` | `ITrmClient`, `IServicioParametros`, `TimeProvider` | `ObtenerAsync(ct)` | Timeout, reintentos con espera 200 ms → 2 s, respaldo atómico e interruptor `trm_habilitada` |
| `ClienteTrmDatosGov` | `HttpClient`, `IServicioParametros`, `OpcionesApisExternas` | `ObtenerAsync` | Activo por defecto contra datos.gov.co; `ClienteTrmNoConfigurado` si se apaga |
| `ClienteCodigoBarrasOff` | `HttpClient`, `IServicioParametros`, `OpcionesApisExternas` | `ConsultarAsync` | Activo por defecto contra Open Food Facts; `ConsultaCodigoBarrasNula` si se apaga |
| `ClienteVerificadorCorreoHttp` | `HttpClient`, `OpcionesApisExternas` | `VerificarAsync` | Solo se registra con `SIGS_EMAIL_VERIFIER_URL` |
| `ClienteSincronizacionSupabase` | `HttpClient`, `IEventoOutboxRepositorio`, `IServicioParametros`, `OpcionesApisExternas`, `IUnidadDeTrabajo`, `TimeProvider` | `SincronizarPendientesAsync(ct)` | Despacha Outbox por REST; no hay proceso programado |
| `GeneradorTicketQuestPdf` | `IServicioParametros` | `GenerarPdfAsync(venta)` | Ticket POS de 80 mm con datos DIAN si existen |
| `ServicioReportesQuestPdf` | `ISesionActual` | `GenerarDesprendibleNominaAsync`, `GenerarReporteVentasAsync` | Exige `GestionarPersonal` o `VerReportes` |
| `GeneradorTicketNulo` | — | `GenerarPdfAsync` | Devuelve `byte[]` vacío; queda para pruebas |

### Clientes, DIAN y sincronización

| Clase | Constructor | Métodos públicos | Notas |
| :-- | :-- | :-- | :-- |
| `ServicioClientes` | `IClienteRepositorio`, `IServicioAuditoria`, `ISesionActual`, `IUnidadDeTrabajo` | `CrearAsync`, `EditarAsync`, `DesactivarAsync`, `BuscarPorDocumentoAsync`, `ObtenerTodosAsync` | `GestionarClientes`; buscar también con `Vender` |
| `ServicioDian` | `IServicioParametros`, `TimeProvider` | `EmitirDocumentoPosAsync`, `ConsultarEstadoResolucionAsync` | Genera CUDE/UBL/QR local; no transmite ni firma |
| `GeneradorCudeDian` (estática) | — | `CalcularCude`, `GenerarCadenaQr` | SHA-384; catálogo VPFE |
| `GeneradorUblDian` (estática) | — | `GenerarXml` | UBL 2.1, perfil Documento Equivalente POS |
| `EventoOutbox` | — | `Crear`, `MarcarProcesado`, `RegistrarFallo` | Se escribe en la misma transacción de la venta |

### Comunes

| Tipo | Responsabilidad |
| :-- | :-- |
| `ExtensionesReloj` | `AhoraUtc`, `HoyLocal`, `RangoDeHoyUtc` |
| `ExtensionesLogica.AgregarLogica` | Composición de todos los servicios y clientes |

## 7. DTOs y records

| Record | Campos | Uso |
| :-- | :-- | :-- |
| `SolicitudVenta` | `Lineas`, `Pagos`, `int? ClienteId`, `int PuntosARedimir`, `string? ClaveIdempotencia` | Entrada de `RegistrarVentaAsync` |
| `ResultadoEmisionDian` | `Cude`, `CadenaQr`, `Prefijo`, `Consecutivo`, `FechaValidacion`, `XmlUbl` | Salida de `EmitirDocumentoPosAsync` |
| `EstadoResolucionDian` | `Numero`, `Prefijo`, `VigenciaHasta`, `RangoDesde/Hasta`, `SiguienteConsecutivo`, alertas y folios | Estado de la resolución |
| `ResultadoCodigoBalanza` | `EsCodigoBalanza`, `Prefijo`, `Plu`, `Gramos`, `CodigoOriginal` | Parseo EAN-13 de balanza |
| `FactorConversionUnidad` | `UnidadOrigen`, `UnidadDestino`, `Factor` | Conversión de unidades de compra |
| `Validador` (estática, `Logica/Comunes`) | `EsNombreValido`, `EsDocumentoValido`, `EsNitValido`, `EsTelefonoValido`, `EsCorreoValido`, `Exigir*` | Reglas de formato compartidas con la UI |
| `ExtensionesParametros.ObtenerBoolAsync` | Interruptores de parámetros | Falta el parámetro → valor por defecto |
| `LineaVenta` | `CodigoBarras`, `Cantidad` | Línea solicitada |
| `ResultadoArqueo` | `Esperado`, `Contado`, `Diferencia`, `DentroDeTolerancia` | Salida del cierre de turno |
| `ResumenFinanciero` | `Ingresos`, `Egresos`, `IvaGenerado`, `Margen` | Resumen de finanzas |
| `AlertaStockMinimo` | `Producto`, `CantidadEnEstante` | Alerta de inventario |
| `Trm` | `Centavos`, `Fecha` | TRM normalizada |
| `InfoProductoExterno` | `Nombre`, `Marca`, `Categoria` | Consulta externa |
| `ParametrosNomina` | 5 valores | Entrada de nómina |
| `PuntoDato` | `Etiqueta`, `Valor` | Punto de widget |
| `DatoWidget` | `IReadOnlyList<PuntoDato> Puntos` | Datos de widget |
| `TamanoWidget` (enum) | `Pequeno`, `Mediano`, `Grande` | Tamaño visual |

## 8. Constantes

| Constante | Valor | Dónde |
| :-- | :-- | :-- |
| `Dinero.PuntosBasicosCien` | `10000` | `Entidades/Base/Dinero` |
| `ProductoPesado.GramosPorKilo` | `1000` | `Entidades/Productos` |
| `PagoTarjeta.DigitosRequeridos` | `4` | `Entidades/Ventas` |
| `ServicioAutenticacion.LongitudMinimaClave` | `8` | `Logica/Seguridad` |
| `ServicioAutenticacion.LongitudMinimaPin` | `4` | `Logica/Seguridad` |
| `ServicioInicializacion.UsuarioAdministrador` | `"admin"` | `Logica/Seguridad` |
| `NombresRol.*` | `Admin`, `Gerente`, `Cajero`, `Bodeguero` | `Entidades/Personal` |
| `ClavesParametro.*` | 38 claves | `Entidades/Configuracion` |
| `GeneradorCudeDian.UrlCatalogoVpfe` | `https://catalogo-vpfe.dian.gov.co/...` | `Logica/Dian` |
| `ConversorUnidades.Bulto50Kg` / `Arroba` / `Libra` / `Kilo` / `Gramo` | Factores de compra | `Logica/Productos` |

## 9. Inyección de dependencias

`AgregarLogica()` llama a `AgregarDatos(ruta)` y registra:

| Ciclo de vida | Registros |
| :-- | :-- |
| **Singleton** | `TimeProvider.System`, `ISesionActual → SesionActual`, `IHasheadorClave → HasheadorBCrypt` |
| **Scoped** | `SigsDbContext`, `IRepositorio<> → Repositorio<>`, `IStockRepositorio<> → StockRepositorio<>`, `IUnidadDeTrabajo → UnidadDeTrabajo`, `IInicializadorBaseDatos → InicializadorBaseDatos`, todos los repositorios específicos |
| **Scoped (lógica)** | `IServicioAuditoria`, `IServicioConsultaAuditoria`, `IServicioParametros`, `IServicioAutenticacion`, `IServicioUsuarios`, `IServicioInicializacion`, `IServicioProductos`, `IServicioBusquedaProductos`, `IServicioMantenimientoProductos`, `IServicioCatalogos`, `IServicioInventario`, `IServicioConsultaInventario`, `IServicioMermas`, `IServicioAlertasInventario`, `IServicioReabastecimiento`, `IServicioCaja`, `IServicioVentas`, `IServicioClientes`, `IServicioEmpleados`, `IServicioEstructura`, `IServicioImpuestos`, `IServicioFinanzas`, `IServicioNomina`, `IServicioDashboard`, `IServicioTrm`, `IServicioDian`, `IServicioSincronizacionRemota`, `IEstrategiaSalida` |
| **Scoped (externos)** | `ITrmClient → ClienteTrmDatosGov` (o `ClienteTrmNoConfigurado` si se apaga), `IConsultaCodigoBarras → ClienteCodigoBarrasOff` (o `ConsultaCodigoBarrasNula`), `IVerificadorCorreo → VerificadorCorreoNulo` (o `ClienteVerificadorCorreoHttp`), `IGeneradorTicket → GeneradorTicketQuestPdf`, `IServicioReportes → ServicioReportesQuestPdf` |
| **Widgets** | `IWidget → VentasDelDiaWidget`, `ReabastecimientosPendientesWidget`, `TrmDelDiaWidget`, `IvaGeneradoWidget`, `MargenPorCategoriaWidget`, `LotesPorVencerWidget`, `StockBajoMinimoWidget`, `VentasPorCategoriaWidget`, `ArqueoTurnoWidget` |

`AgregarLogica()` registra primero los clientes nulos y luego `AgregarClientesExternos()`, que añade TRM y código de barras reales si las URLs están activas. Para reemplazar un cliente, registre otra implementación **después** de `AgregarLogica()`; el contenedor toma la última inscripción. La GUI no debe registrar `SigsDbContext` ni repositorios.

## 10. GUI

| Tipo | Estado |
| :-- | :-- |
| `App` | Inicializa Avalonia y abre `MainWindow` |
| `Program` | Punto de entrada y configuración del `AppBuilder` |
| `MainViewModel` | Shell de ejemplo (`Greeting`) |
| `MainWindow` | Ventana inicial con un `TextBox` |

La GUI todavía no consume la lógica. El punto de conexión previsto es `services.AddSingleton<ISesionActual, SesionActual>()` ya resuelto por `AgregarLogica()`, más las pantallas de login, POS, inventario y caja.

## 11. Pruebas

| Archivo | Cubre |
| :-- | :-- |
| `DineroTests` | Redondeo y porcentajes |
| `ImpuestosTests` | IVA incluido y retención con base mínima |
| `ProductoTests` | Subtotal unitario/pesado y validaciones |
| `VentasTests` | Totales, IVA congelado, pagos mixtos y anulación |
| `CajaTests` | Apertura, arqueo, anulación y cierre único |
| `InventarioTests` | FEFO, reparto, stock no negativo, reabastecimiento |
| `ServicioInventarioTests` | Descuento, reintegro, traslado, merma, lote ajeno |
| `ServicioVentasTests` | Venta, stock, permisos, tarjeta/transferencia, doble anulación |
| `ServicioCajaTests` | Caja activa, arqueo y tolerancia |
| `NominaTests` | Cálculo y validaciones |
| `SeguridadTests` | Bloqueo, permisos, sesión, BCrypt |
| `ServicioAutenticacionTests` | Login, bloqueo, PIN, empleado inactivo, cambio de clave |
| `ServicioParametrosTests` | Permiso, auditoría, respaldo, parámetro faltante |
| `ServicioFinanzasTests` | Permiso y auditoría de gastos |
| `ServicioTrmTests` | Respaldo, reintentos y ausencia total |
| `ValidadorTests` | Formato de nombres, documento, NIT, teléfono y correo |
| `ClientesPuntosTests` | Acumulación, redención, anulación, permisos y alta |
| `ApisPorDefectoTests` | APIs activas y apagables por entorno o parámetro |
| `ClientesSqliteTests` | Persistencia de `Cliente`, índice único y TPH de `PagoPuntos` |
| `EventoOutboxSqliteTests` | Outbox e idempotencia contra SQLite |
| `OutboxYSincronizacionTests` | Payload, despacho y reintentos del Outbox |
| `ReportesPdfTests` | Ticket, desprendible y reporte PDF |
| `FlujoPuntaAPuntaSqliteTests` | Flujo completo contra archivo SQLite temporal |

Falsos en `Supermercado.Pruebas/Falsos`: `RepositorioEnMemoria<T>`, `StockEnMemoria<T>`, repositorios específicos, `ParametrosFalsos`, `UnidadDeTrabajoFalsa`, `AuditoriaFalsa`, `HasheadorFalso`, `RelojFijo`, `ClienteTrmFalso`, `EscenarioInventario`.

## 12. Anexo generado

Para regenerar la lista completa de tipos y métodos públicos:

```bash
./scripts/inventario-codigo.sh > docs/anexo-simbolos.md
```

El anexo no se versiona por defecto; se usa para revisar cambios de API.
