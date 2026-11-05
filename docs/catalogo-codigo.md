# Catálogo de código

Inventario curado de módulos, tipos, contratos, constructores, métodos públicos y registro de dependencias tras la consolidación arquitectónica del sistema.

## 1. Proyectos

| Proyecto | Responsabilidad | Referencias |
| :-- | :-- | :-- |
| `Supermercado.Entidades` | Dominio puro, modelos, enums, excepciones de dominio y contratos | Ninguna |
| `Supermercado.Datos` | `SigsDbContext`, configuraciones EF Core, repositorios, proveedor SQLite local y semilla | Entidades |
| `Supermercado.Logica` | Casos de uso consolidados, seguridad, cálculo monetario, integraciones, reportes y dashboards | Datos, Entidades |
| `Supermercado.GUI` | Aplicación de escritorio Avalonia 12.1.4 + MVVM completo (11 módulos operativos y de back-office) | Lógica, Entidades |
| `Supermercado.Pruebas` | Suite de pruebas unitarias, de integración y GUI con xUnit y falsos en memoria (352 pruebas en verde) | Lógica, Entidades, GUI |

## 2. Módulos y servicios consolidados

| Módulo | Carpetas clave | Servicios de dominio consolidados |
| :-- | :-- | :-- |
| **Productos** | `Entidades/Productos`, `Logica/Productos` | `ServicioProductos` (consolida altas, búsqueda de catálogo, mantenimiento y categorización; integra `ParserCodigoBalanza` y `ConversorUnidades`) |
| **Inventario** | `Entidades/Inventario`, `Logica/Inventario` | `ServicioInventario` (consolida salidas FEFO, reintegros, traslados bodega→estante, ingresos, mermas, alertas de stock mínimo/vencimiento, kardex consolidado y reabastecimiento; usa `SalidaFefo` y `PlanDeSalida`) |
| **Ventas y caja** | `Entidades/Ventas`, `Logica/Ventas` | `ServicioVentas` (orquesta ventas, pagos mixtos, Habeas Data y ventas en espera) y `ServicioCaja` (apertura de turno, retiros parciales a caja fuerte, arqueo ciego y Cierre Z) |
| **Clientes** | `Entidades/Clientes`, `Logica/Clientes` | `ServicioClientes` (gestión de adquirentes, búsqueda por documento y consentimiento legal) |
| **Personal** | `Entidades/Personal`, `Logica/Personal` | `ServicioPersonal` (consolida empleados, usuarios temporales, cargos, cajas, roles y matriz de permisos) |
| **Finanzas** | `Entidades/Finanzas`, `Logica/Finanzas` | `ServicioFinanzas` (consolida gastos operativos, resumen financiero con cálculo de márgenes, tarifas de impuestos y liquidación mensual de nómina) |
| **Seguridad** | `Entidades/Seguridad`, `Logica/Seguridad` | `ServicioAutenticacion` (login con clave/PIN, bloqueo, cambio de credenciales), `ServicioInicializacion`, `ServicioAuditoria` (registro y consultas paginadas), `ServicioParametros`, `SesionActual` y `HasheadorBCrypt` |
| **Dashboards** | `Entidades/Dashboards`, `Logica/Dashboards` | `ServicioDashboard` + 9 widgets de métricas consolidados en `Widgets.cs` |
| **Integraciones y soporte** | `Logica/Servicios` | En `ClientesExternos.cs` (`ServicioTrm`, `ClienteTrmDatosGov`, `ClienteCodigoBarrasOff`, `ClienteVerificadorCorreoHttp`) y en `ServiciosSoporte.cs` (`ServicioRespaldo`, `LoggerArchivo`, `ProveedorLogArchivo`) |
| **Reportes** | `Logica/Reportes` | En `GeneradorTickets.cs` (`GeneradorTicketQuestPdf`, `GeneradorTicketNulo`) y `ServicioReportes.cs` (`ServicioReportesQuestPdf`) |
| **Comunes** | `Entidades/Base`, `Logica/Comunes` | `Dinero` (en `EntidadBase.cs`), `ExtensionesReloj`, `Validador`, `TraductorExcepciones` |

## 3. Entidades

### Base (`Supermercado.Entidades/Base`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `EntidadBase` (abstracta) | Identidad y auditoría de creación | `Id`, `FechaCreacion` |
| `Dinero` (estática, en `EntidadBase.cs`) | Aritmética monetaria entera en pesos | `PuntosBasicosCien = 10000`, `DividirRedondeando`, `Porcentaje` |

### Productos (`Supermercado.Entidades/Productos`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Producto` (abstracta) | Producto vendible con precio, impuesto y mínimos | `CalcularSubtotal`, `UnidadMedida`, FKs `CategoriaId`, `ProveedorId`, `ImpuestoId` |
| `ProductoUnitario` (sealed) | Precio × cantidad | `CalcularSubtotal`; `UnidadMedida = "und"` |
| `ProductoPesado` (sealed) | Precio por kilo, cantidad en gramos | `GramosPorKilo = 1000`; redondeo con `Dinero` |
| `Categoria` | Agrupación de productos | `Nombre` |
| `Proveedor` | Datos del proveedor | `Nombre`, `Nit`, `Telefono`, `Correo` |
| `Lote` | Mercancía con vencimiento y costo | `EstaVencido`, `DiasParaVencer`, `NumeroLote`, `CostoUnitario` |

### Inventario (`Supermercado.Entidades/Inventario`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `StockLote` (abstracta) | Cantidad por lote con invariantes | `Aumentar`, `Disminuir`; `Cantidad` privada |
| `StockEstante` (sealed) | Stock disponible para venta | Hereda `StockLote` |
| `StockBodega` (sealed) | Stock de reserva | Hereda `StockLote` |
| `StockConsolidado` | Resumen de existencias por producto | `ProductoId`, `Nombre`, `CodigoBarras`, `TotalEstante`, `TotalBodega`, `TotalGeneral` |
| `KardexMovimiento` | Trazabilidad detallada de movimientos | `Fecha`, `Tipo`, `Cantidad`, `Lote`, `Ubicacion`, `Motivo` |
| `MovimientoInventario` | Bitácora de entradas y salidas | `Tipo`, `Cantidad`, `LoteId`, `VentaId`, `Motivo` |
| `Merma` | Pérdida de mercancía | `ProductoId`, `LoteId`, `Cantidad`, `Motivo` |
| `Reabastecimiento` | Solicitud de reposición bodega→estante | `Atender(DateTime)`, `Cancelar()`, `Estado` |
| `TipoMovimiento` | Sentido del movimiento | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |
| `UbicacionStock` | Ubicación física | `Estante`, `Bodega` |
| `EstadoReabastecimiento` | Ciclo de la solicitud | `Pendiente`, `Atendido`, `Cancelado` |

### Ventas y caja (`Supermercado.Entidades/Ventas`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Venta` | Transacción con detalles, pagos y estado | `AgregarProducto`, `AgregarPago`, `Cerrar`, `Anular`; `Total`, `TotalIva`, `Cambio`, `EfectivoNeto`; `ClienteId`, `ClaveIdempotencia` |
| `DetalleVenta` | Línea con precio e IVA congelados | `Crear(Producto, int)`, `Subtotal`, `Iva` |
| `Pago` (abstracta) | Base de método de pago | `Monto`, `Metodo`, `Validar` |
| `PagoEfectivo` (sealed) | Pago en efectivo | `Metodo = "Efectivo"` |
| `PagoTarjeta` (sealed) | Pago con tarjeta | `UltimosDigitos`, `DigitosRequeridos = 4` |
| `PagoTransferencia` (sealed) | Transferencia bancaria | `Referencia` |
| `VentaEnEspera` | Venta aparcada temporalmente | `Codigo`, `FechaCreacion`, `JsonContenido`, `Total` |
| `Caja` | Terminal física de atención | `Nombre`, `Activa` |
| `TurnoCaja` | Turno de cajero con arqueo | `Abrir`, `CalcularEfectivoEsperado`, `Cerrar`; `Diferencia` |
| `RetiroCaja` | Retiro parcial a caja fuerte durante el turno | `TurnoCajaId`, `Monto`, `Fecha`, `Motivo`, `AutorizadoPorEmpleadoId` |
| `EstadoVenta` | Ciclo de la venta | `Abierta`, `Completada`, `Anulada` |
| `EstadoTurno` | Ciclo del turno | `Abierto`, `Cerrado` |

### Clientes (`Supermercado.Entidades/Clientes`)

| Tipo | Ubicación | Responsabilidad | Miembros clave |
| :-- | :-- | :-- | :-- |
| `Cliente` | `Entidades/Clientes` | Datos del cliente y Habeas Data | `Documento`, `AutorizarHabeasData`, `RevocarHabeasData`; `AutorizaTratamientoDatos` |

### Personal (`Supermercado.Entidades/Personal`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Empleado` | Persona con cargo, rol y salario | `NombreCompleto`, `Documento`, `SalarioBase`, `Desactivar()` |
| `Cargo` | Cargo organizacional | `Nombre` |
| `Rol` | Perfil con máscara de permisos | `Tiene(Permiso)`; `Permisos` es `[Flags]` |
| `Usuario` | Credenciales de acceso y bloqueo | `RegistrarFallo`, `RegistrarExito`, `Desbloquear`, `CambiarClave`, `AsignarPin` |
| `NombresRol` (estática) | Nombres canónicos de roles | `Admin`, `Gerente`, `Cajero`, `Bodeguero` |

### Finanzas (`Supermercado.Entidades/Finanzas`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Impuesto` (abstracta) | Tarifa tributaria parametrizable | `PuntosBasicos`, `Porcentaje`, `Calcular` |
| `Iva` (sealed) | Impuesto de valor agregado (incluido en precio) | `Calcular` extrae el gravamen |
| `Retencion` (sealed) | Retención en la fuente con base mínima | `BaseMinima`, `Calcular` |
| `Gasto` | Egreso operativo auditado | `Descripcion`, `Monto`, `RegistradoPorEmpleadoId` |
| `Nomina` | Liquidación básica mensual de nómina | `Calcular(...)` estático; valida año, mes y deducciones |
| `ParametrosNomina` (record) | Parámetros legales de nómina colombiana | `SalarioMinimo`, `AuxilioTransporte`, `TopeAuxilioEnSmmlv`, `SaludPuntosBasicos`, `PensionPuntosBasicos` |

### Seguridad y configuración

| Tipo | Ubicación | Responsabilidad | Miembros clave |
| :-- | :-- | :-- | :-- |
| `Permiso` (`[Flags] long`) | `Entidades/Seguridad` | 18 permisos del sistema | `Vender`, `AnularVenta`, `AbrirCerrarTurno`, `VerInventario`, `AjustarInventario`, `Reabastecer`, `GestionarProductos`, `VerReportes`, `VerFinanzas`, `GestionarPersonal`, `GestionarRoles`, `GestionarTarifas`, `VerAuditoria`, `GestionarClientes`, etc. |
| `AccionAuditoria` | `Entidades/Seguridad` | Acciones auditables | 32 valores tipados (login, venta anulada, gasto, retiro de caja, parámetro, cliente, etc.) |
| `RegistroAuditoria` | `Entidades/Seguridad` | Registro cronológico inmutable | `Fecha`, `EmpleadoId`, `Accion`, `Entidad`, `EntidadId`, `Detalle` |
| `IntentoAcceso` | `Entidades/Seguridad` | Registro de intentos de autenticación | `NombreUsuario`, `Exitoso`, `UsoPin`, `Fecha` |
| `FiltroAuditoria` / `ResultadoPaginado` | `Entidades/Seguridad` | Criterios de consulta y paginación | `Desde`, `Hasta`, `EmpleadoId`, `Accion`, `Pagina`, `TamanoPagina` |
| `Parametro` | `Entidades/Configuracion/Parametro.cs` | Parámetro operativo clave/valor | `Clave`, `Valor`, `Descripcion` |
| `ClavesParametro` (estática) | `Entidades/Configuracion/Parametro.cs` | Constantes de claves | Constantes de seguridad, nómina, integraciones, balanzas y negocio |
| `ConfiguracionWidget` | `Entidades/Dashboards/ConfiguracionWidget.cs` | Preferencia de widget por usuario | `EmpleadoId`, `WidgetId`, `Posicion`, `Visible`, `Tamano` |
| `TamanoWidget` / `DisposicionPanel` | `Entidades/Dashboards/ConfiguracionWidget.cs` | Tamaño de tarjeta y layout | `Pequeno`, `Mediano`, `Grande` |

### Excepciones de dominio (`Supermercado.Entidades/Excepciones/ExcepcionesDominio.cs`)

Todas las excepciones del dominio se encuentran agrupadas en un único archivo cohesivo:

| Excepción | Cuándo se lanza |
| :-- | :-- |
| `ValidacionException` | Regla o invariante de negocio incumplida |
| `VentaInvalidaException` | Venta, detalle, pago o anulación inconsistente |
| `StockInsuficienteException` | Stock insuficiente en estante/bodega o lote inexistente |
| `TurnoInvalidoException` | Apertura o cierre de turno fuera de estado permitido |
| `AccesoDenegadoException` | El usuario carece del permiso requerido |
| `CredencialesInvalidasException` | Nombre de usuario, contraseña o PIN incorrectos |
| `CuentaBloqueadaException` | Cuenta bloqueada tras superar intentos fallidos |
| `SesionNoIniciadaException` | Invocación de operación protegida sin sesión activa |
| `DebeCambiarClaveException` | Acceso con cambio obligatorio de contraseña pendiente |
| `ParametroFaltanteException` | Clave de configuración no encontrada o valor malformado |
| `ServicioNoConfiguradoException` | Cliente externo invocado sin servicio configurado |

## 4. Contratos (`Supermercado.Entidades/Contratos`)

Los contratos de persistencia y ciclo de vida se agrupan en 4 archivos temáticos cohesivos por dominio y 3 contratos base:

| Archivo | Interfaz | Implementación | Métodos clave |
| :-- | :-- | :-- | :-- |
| `IRepositorio.cs` | `IRepositorio<T>` | `Repositorio<T>` | `ObtenerPorIdAsync`, `ObtenerTodosAsync`, `AgregarAsync`, `Actualizar` |
| `IUnidadDeTrabajo.cs` | `IUnidadDeTrabajo` | `UnidadDeTrabajo` | `GuardarAsync` |
| `IInicializadorBaseDatos.cs` | `IInicializadorBaseDatos` | `InicializadorBaseDatos` | `CrearYSembrarAsync` |
| `IContratosInventario.cs` | `IStockRepositorio<T>` | `StockRepositorio<T>` | `ObtenerPorProductoAsync`, `ObtenerPorLoteAsync`, `ObtenerTotalesPorProductoAsync` |
| | `ILoteRepositorio` | `LoteRepositorio` | `ObtenerPorVencerAsync` |
| | `IMovimientoRepositorio` | `MovimientoRepositorio` | `ObtenerSalidasDeVentaAsync` |
| | `IReabastecimientoRepositorio` | `ReabastecimientoRepositorio` | `ObtenerPendientesAsync` |
| `IContratosVentas.cs` | `IVentaRepositorio` | `VentaRepositorio` | `ObtenerPorTurnoAsync`, `ObtenerPorRangoAsync`, `ObtenerPorClaveIdempotenciaAsync` |
| | `ITurnoCajaRepositorio` | `TurnoCajaRepositorio` | `ObtenerAbiertoPorEmpleadoAsync`, `ObtenerAbiertoPorCajaAsync` |
| `IContratosSeguridad.cs` | `IUsuarioRepositorio` | `UsuarioRepositorio` | `ObtenerPorNombreAsync` (incluye `Empleado.Rol`) |
| | `IRolRepositorio` | `RolRepositorio` | `ObtenerPorNombreAsync` |
| | `IParametroRepositorio` | `ParametroRepositorio` | `ObtenerPorClaveAsync` |
| `IContratosNegocio.cs` | `IProductoRepositorio` | `ProductoRepositorio` | `ObtenerPorCodigoBarrasAsync` |
| | `IClienteRepositorio` | `ClienteRepositorio` | `ObtenerPorDocumentoAsync` |
| | `IGastoRepositorio` | `GastoRepositorio` | `ObtenerPorRangoAsync` |
| | `IConfiguracionWidgetRepositorio` | `ConfiguracionWidgetRepositorio` | `ObtenerPorEmpleadoAsync` |

## 5. Capa Datos (`Supermercado.Datos`)

| Archivo / Tipo | Responsabilidad |
| :-- | :-- |
| `SigsDbContext.cs` | DbSets de entidades, configuración del modelo y asignación automática de `FechaCreacion` en `SaveChanges`/`SaveChangesAsync` |
| `ConfiguracionModelo.cs` | Mapeo TPH, índices únicos (idempotencia, documento, código de barras) y `DeleteBehavior.Restrict` (en raíz de `Supermercado.Datos/`) |
| `ValoresIniciales.cs` | Semilla idempotente de los 4 roles canónicos (`Admin`, `Gerente`, `Cajero`, `Bodeguero`), cargos, impuestos, cajas y parámetros de arranque (en raíz de `Supermercado.Datos/`) |
| `ExtensionesDatos.cs` | Configuración de SQLite local autónomo, registro de repositorios y unidad de trabajo |
| `RutasDatos.cs` | Resolución de ruta de base de datos local `%LOCALAPPDATA%/SIGS/sigs.db` (Windows) o `~/.local/share/SIGS/sigs.db` (Linux) |
| `InicializadorBaseDatos.cs` | `Migrate` si existen migraciones EF; `EnsureCreated` y siembra idempotente si arranca en frío |

### Implementación de repositorios (`Supermercado.Datos/Repositorios/`)

Los 15 repositorios se agrupan en 5 archivos por área de dominio:

| Archivo | Clases contenidas |
| :-- | :-- |
| `RepositorioGenerico.cs` | `Repositorio<T>` (CRUD EF genérico) y `UnidadDeTrabajo` (`SaveChangesAsync` atómico) |
| `RepositoriosInventario.cs` | `StockRepositorio<T>`, `LoteRepositorio`, `MovimientoRepositorio`, `ReabastecimientoRepositorio` |
| `RepositoriosVentas.cs` | `VentaRepositorio`, `TurnoCajaRepositorio` |
| `RepositoriosSeguridad.cs` | `UsuarioRepositorio`, `RolRepositorio`, `ParametroRepositorio` |
| `RepositoriosNegocio.cs` | `ProductoRepositorio`, `ClienteRepositorio`, `GastoRepositorio`, `ConfiguracionWidgetRepositorio` |

## 6. Capa Lógica (`Supermercado.Logica`)

### Los 8 Servicios de Dominio Consolidados

| Servicio consolidado | Archivo de implementación | Interfaces implementadas | Responsabilidad unificada |
| :-- | :-- | :-- | :-- |
| `ServicioProductos` | `Productos/ServicioProductos.cs` | `IServicioProductos`, `IServicioBusquedaProductos`, `IServicioMantenimientoProductos`, `IServicioCatalogos` | Registro de productos unitarios/pesados, búsqueda reactiva, modificación de precios/catálogos, activación/desactivación y gestión de proveedores y categorías. |
| `ServicioInventario` | `Inventario/ServicioInventario.cs` | `IServicioInventario`, `IServicioConsultaInventario`, `IServicioMermas`, `IServicioAlertasInventario`, `IServicioReabastecimiento` | Descuento FEFO por venta, reintegro por anulación, traslados bodega→estante, ingresos de lotes, registro de mermas, alertas de stock mínimo/vencimiento, kardex y solicitudes de reposición. |
| `ServicioVentas` | `Ventas/ServicioVentas.cs` | `IServicioVentas`, `IServicioVentasEnEspera` | Orquestación completa de venta con comprobación de turno y stock, desglose de IVA (0%, 5%, 19%), Habeas Data, anulación y venta en espera. |
| `ServicioCaja` | `Ventas/ServicioCaja.cs` | `IServicioCaja` | Apertura de turno con fondo inicial, retiros parciales a caja fuerte auditados, cálculo de efectivo esperado, arqueo ciego y Cierre Z con tolerancia. |
| `ServicioClientes` | `Clientes/ServicioClientes.cs` | `IServicioClientes` | Altas, edición, búsqueda y desactivación de clientes con verificación de formato y consentimiento legal. |
| `ServicioPersonal` | `Personal/ServicioPersonal.cs` | `IServicioPersonal`, `IServicioEmpleados`, `IServicioEstructura`, `IServicioUsuarios` | Administración integral de empleados, usuarios y contraseñas temporales, cargos, cajas, roles y matriz de 18 permisos con prevención de auto-escalada. |
| `ServicioFinanzas` | `Finanzas/ServicioFinanzas.cs` | `IServicioFinanzas`, `IServicioImpuestos`, `IServicioNomina` | Registro de gastos auditados, resumen financiero de ingresos/egresos/márgenes, mantenimiento de tarifas tributarias y liquidación mensual de nómina según legislación colombiana. |
| `ServicioAutenticacion` | `Seguridad/ServicioAutenticacion.cs` | `IServicioAutenticacion`, `IServicioUsuarios` (parcial), contiene `ServicioInicializacion` | Autenticación mediante usuario/contraseña o PIN táctil de 4 dígitos, bloqueo temporal por intentos fallidos, cambio de contraseña, asignación de PIN y semilla administrativa de arranque. |

### Servicios de Soporte, Integración y Seguridad Interna

| Clase / Tipo | Archivo | Responsabilidad |
| :-- | :-- | :-- |
| `SesionActual` (singleton) | `Seguridad/SesionActual.cs` | `ISesionActual`: mantiene el contexto del usuario autenticado en el hilo de ejecución; `Tiene()`, `Exigir()`, `Iniciar()`, `Cerrar()`. |
| `HasheadorBCrypt` | `Seguridad/HasheadorClave.cs` | `IHasheadorClave`: hasheo y verificación segura de credenciales mediante algoritmo BCrypt. |
| `ServicioParametros` | `Seguridad/ServicioParametros.cs` | `IServicioParametros`: lectura/escritura de parámetros del sistema con auditoría; `ExtensionesParametros.ObtenerBoolAsync` para interruptores. |
| `ServicioAuditoria` | `Seguridad/ServicioAuditoria.cs` | `IServicioAuditoria`, `IServicioConsultaAuditoria`: registro inmutable en base de datos y consultas paginadas con filtros. |
| `ServicioDashboard` | `Dashboards/ServicioDashboard.cs` | `IServicioDashboard`: orquestación de widgets, persistencia de disposiciones por empleado y filtrado por permisos. |
| `Widgets` (9 clases) | `Dashboards/Widgets.cs` | `ArqueoTurnoWidget`, `IvaGeneradoWidget`, `LotesPorVencerWidget`, `MargenPorCategoriaWidget`, `ReabastecimientosPendientesWidget`, `StockBajoMinimoWidget`, `TrmDelDiaWidget`, `VentasDelDiaWidget`, `VentasPorCategoriaWidget`. |
| `ServicioTrm` y Clientes API | `Servicios/ClientesExternos.cs` | Clientes HTTP con fallback local y tolerancia a fallos: `ClienteTrmDatosGov` (datos.gov.co), `ClienteCodigoBarrasOff` (Open Food Facts), `ClienteVerificadorCorreoHttp`. |
| `ServicioRespaldo` y Logs | `Servicios/ServiciosSoporte.cs` | `IServicioRespaldo` (copia de seguridad en caliente de SQLite con `VACUUM INTO`), `LoggerArchivo` y `ProveedorLogArchivo`. |
| `GeneradorTickets` | `Reportes/GeneradorTickets.cs` | `IGeneradorTicket`, `GeneradorTicketQuestPdf`, `GeneradorTicketNulo`. |
| `ServicioReportes` | `Reportes/ServicioReportes.cs` | `IServicioReportes`, `ServicioReportesQuestPdf` (generación de desprendibles de nómina y reportes de ventas en PDF). |
| `Validador` (estática) | `Comunes/Validador.cs` | Reglas de formato puro compartidas con la UI: validación de NIT con Módulo 11 oficial, cédulas, correos, nombres y teléfonos. |
| `ParserCodigoBalanza` | `Productos/ParserCodigoBalanza.cs` | Interpretación de códigos de balanza EAN-13 GS1 (prefijos 20 a 29) con extracción de PLU y gramos. |
| `ConversorUnidades` | `Productos/ConversorUnidades.cs` | Factores de conversión estándar para compras: bultos de 50kg, arrobas, libras, kilos y gramos. |

## 7. Capa de Presentación (`Supermercado.GUI`)

Interfaz gráfica de escritorio para Windows y Linux desarrollada en **Avalonia 12.1.4** y **.NET 10** bajo el patrón **MVVM** con **CommunityToolkit.Mvvm**:

### ViewModels (`Supermercado.GUI/ViewModels/`)

1. `ViewModelBase`: Base observable con propagación de propiedades.
2. `MainViewModel`: Conmutador maestro de nivel raíz entre la pantalla de autenticación (`LoginViewModel`) y el entorno operativo (`ShellViewModel`).
3. `LoginViewModel`: Autenticación con contraseña o PIN numérico rápido de caja, detección de primer arranque (onboarding con creación de admin) y botones de acceso rápido de demostración para los 4 roles (`Admin`, `Gerente`, `Cajero`, `Bodeguero`).
4. `ShellViewModel`: Shell adaptativa RBAC con menú lateral condicional según los 18 permisos del usuario autenticado, conmutador de temas en caliente y transiciones `CrossFade 150ms`.
5. `PosViewModel`: Punto de venta de alta velocidad optimizado para escáner continuo (*keyboard wedge*), soporte de balanzas GS1 (`20`–`29`), atajos de teclado `F1`–`F5` / `Escape`, visor de total a 32px, discriminación tributaria (C/M/J) y modales integrados (cobro, búsqueda, Habeas Data, ticket).
6. `InventarioViewModel`: Catálogo visual con `DataGrid` reactivo, filtros en tiempo real, alertas de stock mínimo y lotes por vencer, y diálogo de bajas por merma con auditoría.
7. `CajaTurnoViewModel`: Gestión integral de caja: apertura con base inicial, retiros parciales a caja fuerte, arqueo ciego con cálculo automático de sobrante/faltante y generación de Cierre Z.
8. `DashboardViewModel`: Tablero operativo modular con tarjetas de métricas, drag & drop interactivo para reorganizar la disposición del usuario y refresco periódico.
9. `ComprasProveedoresViewModel`: Directorio de proveedores con NIT, registro de compras y sugeridos de reabastecimiento basados en algoritmos FEFO.
10. `ContabilidadDianViewModel`: Desglose contable de ventas e IVA generado por tarifas (0%, 5%, 19%), cierres Z y exportación de reportes.
11. `PersonalRolesViewModel`: Administración de empleados, credenciales de acceso, asignación de PIN, cargos y matriz completa de permisos por rol.
12. `ConfiguracionViewModel`: Panel de preferencias visuales (Claro Cálido, Oscuro Slate, Alto Contraste), visualizador de documentos legales empaquetados y créditos.
13. `RegistroUsuarioViewModel`: Formulario modular de registro con validación de Dígito de Verificación Módulo 11 (`Validador`) y consentimiento de Habeas Data (Ley 1581 de 2012).

### Servicios de Presentación (`Supermercado.GUI/Servicios/`)

Aplanados y consolidados en 3 archivos principales:

| Archivo | Tipos contenidos | Responsabilidad |
| :-- | :-- | :-- |
| `ConfiguracionYTema.cs` | `TemaSigs`, `IServicioPreferencias`, `ServicioPreferencias`, `IServicioTextosLegales`, `ServicioTextosLegales` | Gestión de temas visuales (`ThemeVariant`), persistencia local en `preferencias.json` y lectura offline de textos legales desde recursos empaquetados `avares://Assets/Legal/`. |
| `ImpresionTicket.cs` | `IImpresorTicket`, `ImpresorTicketEscPos`, `ImpresorTicketPdf` | Generación de comandos crudos ESC/POS para impresoras térmicas de 80mm (corte de papel `\x1D\x56\x00` y pulso de gaveta `\x1B\x70\x00\x19\xFA`) y exportación alternativa a documento PDF con QuestPDF. |
| `PlantillaTicketPos.cs` | `PlantillaTicketPos` | Formateador de tirilla continua de 80mm (48 columnas) con discriminación tributaria de IVA (0%, 5%, 19%) y resumen de medios de pago. |

## 8. Inyección de dependencias

La composición se realiza a través de `ExtensionesLogica.AgregarLogica()`, la cual invoca internamente a `ExtensionesDatos.AgregarDatos()`:

| Ciclo de vida | Tipos registrados |
| :-- | :-- |
| **Singleton** | `TimeProvider.System`, `ISesionActual → SesionActual`, `IHasheadorClave → HasheadorBCrypt`, `ILoggerProvider → ProveedorLogArchivo`, `HttpClient`, `OpcionesApisExternas`, `IServicioPreferencias → ServicioPreferencias` (GUI), `IServicioTextosLegales → ServicioTextosLegales` (GUI) |
| **Scoped (datos)** | `SigsDbContext`, `IRepositorio<> → Repositorio<>`, `IStockRepositorio<> → StockRepositorio<>`, `IUnidadDeTrabajo → UnidadDeTrabajo`, `IInicializadorBaseDatos → InicializadorBaseDatos`, repositorios específicos de `IContratos*` implementados en `Repositorios*` |
| **Scoped (servicios)** | `ServicioAuditoria` (`IServicioAuditoria`, `IServicioConsultaAuditoria`), `IServicioParametros → ServicioParametros`, `IServicioAutenticacion → ServicioAutenticacion`, `IServicioInicializacion → ServicioInicializacion`, `ServicioPersonal` (`IServicioPersonal`, `IServicioEmpleados`, `IServicioEstructura`, `IServicioUsuarios`), `ServicioProductos` (`IServicioProductos`, `IServicioBusquedaProductos`, `IServicioMantenimientoProductos`, `IServicioCatalogos`), `ServicioInventario` (`IServicioInventario`, `IServicioConsultaInventario`, `IServicioMermas`, `IServicioAlertasInventario`, `IServicioReabastecimiento`), `IServicioCaja → ServicioCaja`, `ServicioVentas` (`IServicioVentas`, `IServicioVentasEnEspera`), `ServicioFinanzas` (`IServicioFinanzas`, `IServicioImpuestos`, `IServicioNomina`), `IServicioClientes → ServicioClientes`, `IServicioDashboard → ServicioDashboard`, `IServicioTrm → ServicioTrm`, `IGeneradorDatosDemo → GeneradorDatosDemo`, `IServicioRespaldo → ServicioRespaldo`, `ITraductorExcepciones → TraductorExcepciones`, `IEstrategiaSalida → SalidaFefo`, `IImpresorTicket → ImpresorTicketEscPos` (GUI), `ImpresorTicketPdf` (GUI) |
| **Scoped (externos)** | `ITrmClient → ClienteTrmDatosGov` (o `ClienteTrmNoConfigurado`), `IConsultaCodigoBarras → ClienteCodigoBarrasOff` (o `ConsultaCodigoBarrasNula`), `IVerificadorCorreo → VerificadorCorreoNulo` (o `ClienteVerificadorCorreoHttp`), `IGeneradorTicket → GeneradorTicketQuestPdf`, `IServicioReportes → ServicioReportesQuestPdf` |
| **Scoped (widgets)** | 9 implementaciones de `IWidget` (`ArqueoTurnoWidget`, `IvaGeneradoWidget`, `LotesPorVencerWidget`, `MargenPorCategoriaWidget`, `ReabastecimientosPendientesWidget`, `StockBajoMinimoWidget`, `TrmDelDiaWidget`, `VentasDelDiaWidget`, `VentasPorCategoriaWidget`) |
| **Transient (GUI)** | Todos los ViewModels (`MainViewModel`, `LoginViewModel`, `ShellViewModel`, `PosViewModel`, `InventarioViewModel`, `CajaTurnoViewModel`, `DashboardViewModel`, `ComprasProveedoresViewModel`, `ContabilidadDianViewModel`, `PersonalRolesViewModel`, `ConfiguracionViewModel`, `RegistroUsuarioViewModel`) |

## 9. Pruebas automatizadas (`Supermercado.Pruebas`)

Suite de pruebas unitarias y de integración construida sobre **xUnit 2.9.3**, con un total de **352 pruebas en verde** (0 fallos, 0 omitidas):

| Archivo de prueba | Módulo o regla cubierta |
| :-- | :-- |
| `DineroTests.cs` | Aritmética entera, redondeos comerciales y porcentajes en puntos básicos |
| `ImpuestosTests.cs` | Extracción de IVA incluido y retención en la fuente con base mínima |
| `ProductoTests.cs` | Subtotales para productos unitarios y pesados por gramos, invariantes |
| `VentasTests.cs` | Totales, congelación de precio/IVA en detalle, pagos combinados y anulación |
| `CajaTests.cs` | Apertura de turno, cálculo de efectivo esperado descontando ventas anuladas y arqueo |
| `InventarioTests.cs` | Algoritmo FEFO, partición de stock no negativo, transferencias y solicitudes |
| `ServicioInventarioTests.cs` | Descuento automático por FEFO, reintegro de venta, traslados, mermas y alertas |
| `ServicioVentasTests.cs` | Flujo completo de venta, validación de stock, medios de pago, idempotencia y doble anulación |
| `ServicioCajaTests.cs` | Validación de caja activa, retiros parciales, arqueos y tolerancias |
| `NominaTests.cs` | Cálculo de devengados, auxilio de transporte y deducciones de salud/pensión (norma colombiana) |
| `SeguridadTests.cs` | Bloqueo por intentos, verificación de permisos por flags, aislamiento de sesión y BCrypt |
| `ServicioAutenticacionTests.cs` | Login con contraseña o PIN, bloqueo, empleado inactivo y cambio de clave |
| `ServicioParametrosTests.cs` | Permisos administrativos, auditoría de cambios y respaldo atómico |
| `ServicioFinanzasTests.cs` | Registro de gastos auditados y cálculo de márgenes operativos |
| `ServicioTrmTests.cs` | Respaldo, degradación limpia ante desconexión e interruptor de TRM |
| `ValidadorTests.cs` | Formato de nombres, teléfonos, correos y Dígito de Verificación Módulo 11 |
| `ClientesPuntosTests.cs` | Gestión de clientes, consentimiento Habeas Data (Ley 1581) y reemplazo por consumidor final |
| `ApisPorDefectoTests.cs` | Clientes externos activos por defecto y apagado controlado por variables de entorno |
| `ClientesSqliteTests.cs` | Persistencia de clientes, unicidad de documento y discriminador TPH en SQLite |
| `EventoOutboxSqliteTests.cs` | Idempotencia de ventas contra SQLite local |
| `DianYBalanzasTests.cs` | Parsing de balanzas GS1 (`20`–`29`), conversor de unidades mayoristas y régimen tributario |
| `ReportesPdfTests.cs` | Generación de ticket POS de 80mm, desprendible de nómina y reporte de ventas con QuestPDF |
| `FlujoPuntaAPuntaSqliteTests.cs` | Escenario transaccional completo de supermercado sobre base de datos SQLite real |
| `GeneradorDatosDemoTests.cs` | Poblado sintético de datos de demostración |
| `ServicioDashboardTests.cs` | Registro, reordenamiento y consulta de los 9 widgets |
| `GUI/PosViewModelTests.cs` | Escaneo en POS, interpretación de stickers GS1, cálculo de cambio y modales |
| `GUI/InventarioViewModelTests.cs` | Búsqueda reactiva, filtros de stock bajo/por vencer y bajas por merma |
| `GUI/PlantillaYRegulacionGuiTests.cs` | Formato de tirilla 48 cols, Módulo 11 NIT y consentimiento Ley 1581 |
| `GUI/DashboardGuiTests.cs` | Integración gráfica y ciclo de vida de widgets en el tablero |
