# Catálogo de código

Inventario curado de módulos, tipos, contratos, constructores, métodos públicos y registro de dependencias tras la simplificación de datos. Las rutas son las reales en el árbol de trabajo: no quedan carpetas por dominio dentro de Entidades, Datos ni Lógica.

## 1. Proyectos

| Proyecto | Responsabilidad | Referencias |
| :-- | :-- | :-- |
| `Supermercado.Entidades` | Dominio puro: 11 archivos `.cs` con modelos, enums y excepciones | Ninguna |
| `Supermercado.Datos` | 12 archivos `.cs`: `SigsDbContext`, repositorios por módulo, EF Core, SQLite, semilla y migración | Entidades |
| `Supermercado.Logica` | 21 archivos `.cs`: casos de uso concretos, seguridad, cálculo, integraciones, reportes y dashboards | Datos, Entidades |
| `Supermercado.GUI` | 33 archivos `.cs`: Avalonia 12.1.4 + MVVM; el POS está repartido en 5 archivos | Lógica, Entidades |
| `Supermercado.Pruebas` | 29 archivos `.cs`: 22 en raíz y 7 en `GUI/`; suite xUnit en verde (conteo en `plan-pruebas.md`) | Lógica, Entidades, GUI |

La compilación de `Supermercado.slnx` termina con 0 advertencias y 0 errores. Ningún archivo supera las 900 líneas físicas; el más largo tiene 807 (`Supermercado.Pruebas/Falsos.cs`).

## 2. Módulos y servicios consolidados

Los módulos se identifican por archivo, no por carpeta. Cada dominio tiene su archivo en Entidades, su archivo de repositorios en Datos y su caso de uso en Lógica.

| Módulo | Archivos reales | Tipos principales |
| :-- | :-- | :-- |
| **Productos** | `Entidades/Productos.cs`, `Datos/Productos.cs`, `Logica/Productos.cs` | `ServicioProductos`, `ProductoRepositorio`, `CategoriaRepositorio`, `ProveedorRepositorio`, `ImpuestoRepositorio` |
| **Inventario** | `Entidades/Inventario.cs`, `Datos/Inventario.cs`, `Logica/Inventario.cs`, `Logica/InventarioSalidas.cs` | `ServicioInventario`, `EstrategiaSalida`, `SalidaFefo`, `PlanDeSalida`, repositorios de lotes, movimientos, stocks, mermas y reabastecimientos |
| **Ventas y caja** | `Entidades/Ventas.cs`, `Entidades/Caja.cs`, `Datos/Ventas.cs`, `Datos/Caja.cs`, `Logica/Ventas.cs`, `Logica/Caja.cs` | `ServicioVentas`, `ServicioCaja`, repositorios de ventas, ventas en espera, turnos, cajas y retiros |
| **Clientes** | `Entidades/Clientes.cs`, `Datos/Clientes.cs`, `Logica/Clientes.cs` | `ServicioClientes`, `ClienteRepositorio` |
| **Personal** | `Entidades/Personal.cs`, `Datos/Personal.cs`, `Logica/Personal.cs` | `ServicioPersonal`, `EmpleadoRepositorio`, `CargoRepositorio` |
| **Finanzas** | `Entidades/Finanzas.cs`, `Datos/Finanzas.cs`, `Logica/Finanzas.cs` | `ServicioFinanzas`, `GastoRepositorio`, `NominaRepositorio` |
| **Seguridad** | `Entidades/Seguridad.cs`, `Datos/Seguridad.cs`, `Logica/Seguridad.cs` | `ServicioAutenticacion`, `ServicioInicializacion`, `ServicioAuditoria`, `ServicioParametros`, `SesionActual`, `HasheadorBCrypt` |
| **Dashboards** | `Entidades/Dashboard.cs`, `Datos/Dashboard.cs`, `Logica/Dashboard.cs`, `Logica/DashboardWidgets.cs` | `ServicioDashboard`, `IWidget`, `WidgetBase` y 10 widgets |
| **Integraciones** | `Logica/ClientesExternos.cs`, `Logica/Infraestructura.cs`, `Logica/Reportes.cs`, `Logica/Telegram*.cs` | `ServicioTrm`, `ServicioRespaldo`, `GeneradorTicketQuestPdf`, `ServicioReportesQuestPdf`, `ProcesadorTelegram` |
| **Comunes y composición** | `Entidades/Base.cs`, `Entidades/Excepciones.cs`, `Logica/Comun.cs`, `Logica/DatosDemo.cs`, `Logica/Composicion.cs` | `EntidadBase`, `Dinero`, `PersonaBase`, `Validador`, `TraductorExcepciones`, `GeneradorDatosDemo` |
| **Persistencia transversal** | `Datos/Contexto.cs`, `Datos/Semillas.cs`, `Datos/Migraciones/Inicial.cs` | `SigsDbContext`, `ConfiguracionModelo`, `RutasDatos`, `InicializadorBaseDatos` |

## 3. Entidades

Todas las entidades son archivos planos en `Supermercado.Entidades/`.

### Base (`Entidades/Base.cs`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `EntidadBase` (abstracta) | Identidad y auditoría de creación | `Id`, `FechaCreacion` |
| `Dinero` (estática) | Aritmética monetaria entera en pesos | `PuntosBasicosCien`, `DividirRedondeando`, `Porcentaje` |
| `PersonaBase` (abstracta) | Datos comunes de personas | `Documento`, `Nombres`, `Apellidos`, `Correo`, `Activo`, `NombreCompleto` |

### Productos (`Entidades/Productos.cs`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Producto` (abstracta) | Producto vendible con precio, impuesto y mínimos | `CalcularSubtotal`, `UnidadMedida`, FKs `CategoriaId`, `ProveedorId`, `ImpuestoId` |
| `ProductoPesado` (sealed) | Precio por kilo, cantidad en gramos | `GramosPorKilo = 1000`, redondeo con `Dinero` |
| `ProductoUnitario` (sealed) | Precio × cantidad | `CalcularSubtotal` directo |
| `Categoria`, `Proveedor` | Catálogo y proveeduría | `Nombre`; `Nit`, `Telefono`, `Correo` |
| `Lote` | Mercancía con vencimiento y costo | `EstaVencido`, `DiasParaVencer`, `NumeroLote`, `CostoUnitario` |

### Inventario (`Entidades/Inventario.cs`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `StockLote` (abstracta) | Cantidad por lote con invariantes | `Aumentar`, `Disminuir`; `Cantidad` privada |
| `StockEstante`, `StockBodega` (sealed) | Existencias disponibles y de reserva | Heredan `StockLote`; EF las mapea en tablas separadas |
| `StockConsolidado`, `KardexMovimiento` | Resumen y trazabilidad | Totales por ubicación; `SaldoPosterior` |
| `MovimientoInventario`, `Merma` | Bitácora y pérdidas | `Tipo`, `Cantidad`, `LoteId`, `VentaId`, `Motivo` |
| `Reabastecimiento` | Solicitud bodega→estante | `Atender(DateTime)`, `Cancelar()`, `Estado` |
| `TipoMovimiento`, `UbicacionStock`, `EstadoReabastecimiento` | Enums de dominio | `Ingreso`, `Traslado`, `Venta`, `Devolucion`, `Merma`, `Ajuste` |

### Ventas y caja (`Entidades/Ventas.cs`, `Entidades/Caja.cs`)

| Tipo | Responsabilidad | Miembros clave |
| :-- | :-- | :-- |
| `Venta` | Transacción con detalles, pagos y estado | `AgregarProducto`, `AgregarPago`, `Cerrar`, `Anular`; `Total`, `TotalIva`, `Cambio`, `EfectivoNeto`, `ClaveIdempotencia` |
| `DetalleVenta` | Línea con precio e IVA congelados | `Crear(Producto, int)`, `Subtotal`, `Iva` |
| `Pago` (abstracta) | Base de método de pago | `Monto`, `Metodo`, `Validar` |
| `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia` (sealed) | Medios de pago | `Efectivo`, `UltimosDigitos`, `Referencia` |
| `VentaEnEspera` | Venta aparcada | `IdentificadorCliente`, `LineasJson`, `TotalEstimado`, `Recuperada` |
| `TurnoCaja`, `Caja`, `RetiroCaja` | Turno, terminal y retiro a caja fuerte | `Abrir`, `CalcularEfectivoEsperado`, `Cerrar`; `Diferencia`, `EmpleadoAutorizaId` |
| `EstadoVenta`, `EstadoTurno` | Enums de ciclo | `Abierta`, `Completada`, `Anulada`; `Abierto`, `Cerrado` |

### Clientes, personal y finanzas

| Archivo | Tipos principales |
| :-- | :-- |
| `Entidades/Clientes.cs` | `Cliente : PersonaBase`, Habeas Data (`AutorizarHabeasData`, `RevocarHabeasData`) |
| `Entidades/Personal.cs` | `Empleado : PersonaBase`, `Usuario`, `Cargo`, `Rol`, `NombresRol` |
| `Entidades/Finanzas.cs` | `Impuesto`, `Iva`, `Retencion`, `Gasto`, `Nomina`, `ParametrosNomina` |

### Seguridad, dashboards y excepciones

| Archivo | Tipos principales |
| :-- | :-- |
| `Entidades/Seguridad.cs` | `Permiso` (`[Flags] long`, 18 valores), `AccionAuditoria` (incluye `ProductoCreado` e `IngresoBodegaRegistrado`), `RegistroAuditoria`, `IntentoAcceso`, `FiltroAuditoria`, `ResultadoPaginado<T>`, `Parametro`, `ClavesParametro` |
| `Entidades/Dashboard.cs` | `TamanoWidget`, `ConfiguracionWidget`, `DisposicionPanel`, `EntradaDisposicion` |
| `Entidades/Excepciones.cs` | 11 excepciones de dominio, de `ValidacionException` a `VentaInvalidaException` |

## 4. Contratos

El espacio de nombres `Supermercado.Entidades.Contratos` existe físicamente en los archivos planos de `Supermercado.Datos/`, pero ya no abstrae todos los repositorios. Tras la simplificación solo quedan dos contratos de persistencia, con implementación real y falsos propios; el resto se inyecta como clase concreta. Todos incluyen `GuardarAsync` y no hay contrato genérico ni unidad de trabajo.

| Archivo | Contratos declarados | Implementaciones |
| :-- | :-- | :-- |
| `Datos/Ventas.cs` | `IVentaRepositorio` | `VentaRepositorio` |
| `Datos/Seguridad.cs` | `IUsuarioRepositorio` | `UsuarioRepositorio` |

Los 24 repositorios restantes se registran y consumen como tipos concretos: `ProductoRepositorio`, `CategoriaRepositorio`, `ProveedorRepositorio`, `ImpuestoRepositorio`, `LoteRepositorio`, `MovimientoRepositorio`, `ReabastecimientoRepositorio`, `StockEstanteRepositorio`, `StockBodegaRepositorio`, `MermaRepositorio`, `VentaEnEsperaRepositorio`, `TurnoCajaRepositorio`, `CajaRepositorio`, `RetiroCajaRepositorio`, `ClienteRepositorio`, `EmpleadoRepositorio`, `CargoRepositorio`, `GastoRepositorio`, `NominaRepositorio`, `RolRepositorio`, `ParametroRepositorio`, `IntentoAccesoRepositorio`, `RegistroAuditoriaRepositorio` y `ConfiguracionWidgetRepositorio`.

Todos los repositorios heredan `GuardarAsync` de `RepositorioBase` (`Datos/Contexto.cs`), que delega en `SigsDbContext.SaveChangesAsync`. Un caso de uso realiza una sola llamada al final; el contexto compartido confirma de forma atómica todas las entidades modificadas.

## 5. Capa Datos (`Supermercado.Datos`)

| Archivo / Tipo | Responsabilidad |
| :-- | :-- |
| `Contexto.cs` / `SigsDbContext` | DbSets, `ConfiguracionModelo`, `RutasDatos`, asignación de `FechaCreacion`, TPH y registro de `AgregarDatos` con SQLite local |
| `Semillas.cs` / `ValoresIniciales`, `InicializadorBaseDatos` | Roles, cargos, impuestos, cajas y parámetros idempotentes; `CrearYSembrarAsync` completa los permisos faltantes de roles ya creados |
| `Migraciones/Inicial.cs` | Migración `20260420000000_Inicial` enlazada a `SigsDbContext` |
| `Productos.cs`, `Inventario.cs`, `Ventas.cs`, `Caja.cs`, `Clientes.cs`, `Personal.cs`, `Finanzas.cs`, `Seguridad.cs`, `Dashboard.cs` | Repositorios concretos de cada módulo, en archivos planos; solo `Ventas.cs` y `Seguridad.cs` conservan interfaces |

## 6. Capa Lógica (`Supermercado.Logica`)

Los 21 archivos son planos. Los casos de uso se consumen como clases concretas; no hay interfaces para casos de uso internos ni adaptadores entre módulos. En total hay 17 interfaces: 2 en Datos (`IVentaRepositorio`, `IUsuarioRepositorio`), 12 en Lógica (límites externos, widgets y `IHasheadorClave`) y 3 en GUI (preferencias, textos legales e impresión).

| Archivo | Contenido principal |
| :-- | :-- |
| `Composicion.cs` | `ExtensionesLogica.AgregarLogica`, `AgregarClientesExternos` |
| `Comun.cs` | `Validador`, `TraductorExcepciones`, `ExtensionesReloj` |
| `Seguridad.cs` | `ServicioAutenticacion`, `ServicioInicializacion`, `ServicioAuditoria`, `ServicioParametros`, `SesionActual`, `HasheadorBCrypt` |
| `Personal.cs` | `ServicioPersonal` |
| `Productos.cs` | `ServicioProductos`: altas desde GUI (`CrearProductoAsync`, `ObtenerProductosActivosAsync`, `ObtenerImpuestosAsync`), ingreso de lotes reutilizando lote existente y auditoría `ProductoCreado`/`IngresoBodegaRegistrado` |
| `Inventario.cs` | `ServicioInventario`: `RegistrarIngresoAsync` reutiliza `StockBodega` por lote y registra `TipoMovimiento.Ingreso` |
| `InventarioSalidas.cs` | `PlanDeSalida`, `EstrategiaSalida`, `SalidaFefo` |
| `Ventas.cs` | `ServicioVentas` |
| `Caja.cs` | `ServicioCaja` |
| `Clientes.cs` | `ServicioClientes` |
| `Finanzas.cs` | `ServicioFinanzas` |
| `Dashboard.cs` | `ServicioDashboard`, `IWidget`, `WidgetEnPanel`, `DatoWidget` |
| `DashboardWidgets.cs` | `WidgetBase` y 10 widgets: `ArqueoTurnoWidget`, `IvaGeneradoWidget`, `LotesPorVencerWidget`, `ReabastecimientosPendientesWidget`, `StockBajoMinimoWidget`, `TrmDelDiaWidget`, `VentasDelDiaWidget`, `ResumenOperativoWidget`, `VentasPorCategoriaWidget`, `MargenPorCategoriaWidget` |
| `ClientesExternos.cs` | `ITrmClient`, `IConsultaCodigoBarras`, `IVerificadorCorreo`, `ServicioTrm` y clientes HTTP |
| `Infraestructura.cs` | `IServicioRespaldo`, `ServicioRespaldo`, `ProveedorLogArchivo`, `LoggerArchivo` |
| `Reportes.cs` | `IServicioReportes`, `ServicioReportesQuestPdf`, `IGeneradorTicket`, `GeneradorTicketQuestPdf` |
| `Telegram.cs` | `ProcesadorTelegram` (comandos y actualizaciones) |
| `TelegramAprobaciones.cs` | `ProcesadorTelegram` (aprobaciones y callbacks) |
| `TelegramClientes.cs` | `IClienteTelegram`, `ClienteTelegramHttp`, `ClienteGeminiHttp` |
| `TelegramSoporte.cs` | `IClienteGemini`, `Asistente`, `IVinculosTelegram`, `VinculosTelegram`, `TelegramWorker` |
| `DatosDemo.cs` | `GeneradorDatosDemo` |

## 7. Capa de Presentación (`Supermercado.GUI`)

Avalonia 12.1.4, .NET 10 y CommunityToolkit.Mvvm. Los 33 archivos `.cs` se reparten en 4 archivos raíz, 17 ViewModels y 12 code-behind de vistas. `ViewLocator.cs` permanece intacto; `Views/InventarioView.axaml` incorpora los formularios de nuevo producto y entrada a bodega, y el resto del XAML no cambia.

### Archivos raíz

| Archivo | Responsabilidad |
| :-- | :-- |
| `App.axaml.cs` | Inicializa el contenedor con `AgregarLogica()` y registra los servicios de presentación; inyecta `SesionActual` en `InventarioViewModel` |
| `Program.cs` | Punto de entrada de Avalonia |
| `ServiciosGui.cs` | `TemaSigs`, `IServicioPreferencias`, `ServicioPreferencias`, `IServicioTextosLegales`, `ServicioTextosLegales`, `IImpresorTicket`, `ImpresorTicketEscPos`, `ImpresorTicketPdf`, `PlantillaTicketPos` |
| `ViewLocator.cs` | Resolución reflexiva de `ViewModel` a `View` |

### ViewModels (`Supermercado.GUI/ViewModels/`)

`ViewModelBase`, `MainViewModel`, `LoginViewModel`, `ShellViewModel`, `DashboardViewModel`, `InventarioViewModel`, `CajaTurnoViewModel`, `ComprasProveedoresViewModel`, `ContabilidadDianViewModel`, `PersonalRolesViewModel`, `ConfiguracionViewModel`, `RegistroUsuarioViewModel` y el POS dividido en cinco archivos: `PosViewModel.cs`, `PosViewModel.Carrito.cs`, `PosViewModel.Operaciones.cs`, `PosViewModel.Pagos.cs` y `PosViewModel.Tickets.cs`.

Buenas prácticas aplicadas: `ViewModelBase : ObservableObject`, propiedades `[ObservableProperty]`, comandos `RelayCommand`/`AsyncRelayCommand`, navegación por ViewModels y ninguna referencia desde la GUI a `Supermercado.Datos`. `InventarioViewModel` expone `PuedeGestionarInventario` desde `SesionActual.Tiene(Permiso.GestionarProductos)` y los comandos `AbrirModalNuevoProducto`, `CerrarModalNuevoProducto`, `ConfirmarNuevoProducto`, `AbrirModalEntradaBodega`, `CerrarModalEntradaBodega` y `ConfirmarEntradaBodega`.

## 8. Inyección de dependencias

La GUI solo llama a `ExtensionesLogica.AgregarLogica()` (`Supermercado.Logica/Composicion.cs`), que a su vez invoca `AgregarDatos()` en `Supermercado.Datos/Contexto.cs`. No hay otra composición oculta.

| Ciclo de vida | Registros reales |
| :-- | :-- |
| **Singleton** | `TimeProvider.System`, `SesionActual`, `IHasheadorClave → HasheadorBCrypt`, `ILoggerProvider → ProveedorLogArchivo`, `HttpClient`, `OpcionesApisExternas`, `IClienteTelegram → ClienteTelegramHttp`, `IVinculosTelegram → VinculosTelegram`; en GUI, preferencias y textos legales |
| **Scoped (datos)** | `SigsDbContext`, `InicializadorBaseDatos`, 24 repositorios concretos y 2 interfaces de repositorio (`IVentaRepositorio`, `IUsuarioRepositorio`) registrados en `Contexto.cs` |
| **Scoped (dominio)** | `ServicioAuditoria`, `ServicioParametros`, `ServicioAutenticacion`, `ServicioInicializacion`, `ServicioPersonal`, `ServicioProductos`, `ServicioInventario`, `ServicioCaja`, `ServicioVentas`, `ServicioFinanzas`, `ServicioClientes`, `ServicioDashboard`, `EstrategiaSalida → SalidaFefo`, `TraductorExcepciones`, `GeneradorDatosDemo` |
| **Scoped (externos)** | `IServicioTrm → ServicioTrm`, `IClienteGemini → ClienteGeminiHttp`, `IGeneradorTicket → GeneradorTicketQuestPdf`, `IServicioReportes → ServicioReportesQuestPdf`, `IServicioRespaldo → ServicioRespaldo`; `ITrmClient`, `IConsultaCodigoBarras` e `IVerificadorCorreo` se registran primero en modo reserva y se sustituyen por sus clientes reales cuando el límite está configurado |
| **Scoped (widgets)** | 10 implementaciones de `IWidget`, una por tarjeta listada en la sección 6 |
| **Hosted / GUI** | `TelegramWorker` como `BackgroundService`; en `App.axaml.cs`, `IServicioPreferencias`, `IServicioTextosLegales`, `IImpresorTicket → ImpresorTicketEscPos` e `ImpresorTicketPdf` |

## 9. Pruebas automatizadas (`Supermercado.Pruebas`)

Suite xUnit en verde (0 fallos, 0 omitidas); el conteo vigente está en [`plan-pruebas.md`](plan-pruebas.md). Los 29 archivos `.cs`: 22 en raíz y 7 en `GUI/`. Los 7 de `GUI/` son `AccesoGuiTests.cs`, `CajaTurnoGuiTests.cs`, `ComprasContabilidadGuiTests.cs`, `MensajesGuiTests.cs`, `PersonalDashboardGuiTests.cs`, `PosGuiTests.cs` y `ShellConfiguracionGuiTests.cs`.

| Archivo | Módulo o regla cubierta |
| :-- | :-- |
| `CajaTests.cs` | Apertura de turno, retiros, arqueo y `ServicioCaja` |
| `ClientesTests.cs` | Clientes, Habeas Data, validadores, TRM y clientes externos por defecto |
| `DashboardTests.cs` | Widgets, disposición, filtrado por permiso y exportación/importación |
| `DatosIntegracionTests.cs` | Flujo punta a punta SQLite, idempotencia y mapeo del modelo |
| `DatosTests.cs` | Datos demo, respaldo, inicialización, SQLite y repositorios |
| `EstructuraTests.cs` | Altas de caja, cargo y rol con auditoría y una sola confirmación |
| `Falsos.cs` | Reloj, hash, parámetros, TRM, `ContextoPruebasSqlite` y repositorios falsos sobre SQLite en memoria |
| `FinanzasTests.cs` | `Dinero`, impuestos, nómina y servicios financieros |
| `GUIInventarioTests.cs` | Nuevos módulos GUI, `InventarioViewModel`, permisos de alta, creación desde GUI y validaciones de entrada |
| `GUIPosTests.cs` | Escaneo POS, pagos, modales, tirilla y regulación |
| `GUITests.cs` | Temas, tokens y `DashboardGuiTests` |
| `InventarioTests.cs` | FEFO, alertas, mermas, traslados y reabastecimiento |
| `PersonalTests.cs` | Empleados, usuarios, credenciales y permisos |
| `ProductosDianTests.cs` | Balanzas GS1, conversiones y régimen tributario |
| `ProductosTests.cs` | Catálogo, mantenimiento, subtotales, altas de productos, códigos duplicados, lotes y entradas a bodega de `ServicioProductos` |
| `ReportesTests.cs` | Ticket POS de 80 mm, desprendible de nómina y reportes PDF |
| `SeguridadTests.cs` | Bloqueo, permisos, autenticación, parámetros y consultas de lectura |
| `ServicioDashboardTests.cs` | Registro, reordenamiento y consulta de los 10 widgets |
| `TelegramComandosTests.cs` | Comandos, composición, permisos, revinculación y sesión aislada |
| `TelegramFalsos.cs` | Escenario Telegram, reloj variable y dobles de red y repositorios |
| `TelegramTests.cs` | `Asistente` y herramientas de lenguaje natural |
| `VentasTests.cs` | Totales, pagos, anulación, ventas en espera y `ServicioVentas` |
