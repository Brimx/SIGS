# Conceptos aplicados

Referencias verificadas sobre el estado final de la rama tras la simplificación de datos. Cada fila apunta a una ruta y línea reales del repositorio.

| Concepto | Ruta:línea | Evidencia |
| :-- | :-- | :-- |
| Entidad base | `Supermercado.Entidades/Base.cs:3` | `EntidadBase` abstracta aporta `Id` y `FechaCreacion`. |
| Aritmética monetaria | `Supermercado.Entidades/Base.cs:10` | `Dinero` trabaja en pesos enteros y puntos básicos, sin `double`. |
| Persona base | `Supermercado.Entidades/Base.cs:21` | `PersonaBase` reúne documento, nombres, correo, estado y `NombreCompleto` virtual. |
| Herencia de personas | `Supermercado.Entidades/Clientes.cs:7`, `Supermercado.Entidades/Personal.cs:56` | `Cliente : PersonaBase` y `Empleado : PersonaBase`. |
| Herencia TPH | `Supermercado.Entidades/Productos.cs:6`, `Supermercado.Entidades/Ventas.cs:140`, `Supermercado.Entidades/Inventario.cs:96`, `Supermercado.Datos/Contexto.cs:87` | `Producto`, `Pago` e `Impuesto` comparten tabla con discriminador; `StockLote` se mapea como tablas separadas. |
| Polimorfismo de productos | `Supermercado.Entidades/Productos.cs:21`, `Supermercado.Entidades/Productos.cs:55`, `Supermercado.Entidades/Productos.cs:61` | `CalcularSubtotal` es abstracta y se resuelve distinto en `ProductoPesado` y `ProductoUnitario`. |
| Polimorfismo de pagos | `Supermercado.Entidades/Ventas.cs:145`, `Supermercado.Entidades/Ventas.cs:156`, `Supermercado.Entidades/Ventas.cs:165`, `Supermercado.Entidades/Ventas.cs:180` | `Pago.Metodo` abstracto y sus implementaciones de efectivo, tarjeta y transferencia. |
| Estrategia de salida | `Supermercado.Logica/InventarioSalidas.cs:38`, `Supermercado.Logica/InventarioSalidas.cs:44` | `EstrategiaSalida` abstrae el orden y `SalidaFefo` excluye vencidos y ordena por vencimiento y lote. |
| Contrato de widgets | `Supermercado.Logica/Dashboard.cs:348` | `IWidget` define identidad, grupo, tamaño, frecuencia, permiso y fuente de datos. |
| Base de widgets | `Supermercado.Logica/DashboardWidgets.cs:15` | `WidgetBase` implementa `IWidget` y centraliza los datos comunes de las 10 tarjetas. |
| S/O/D por módulo | `Supermercado.Logica/Productos.cs:12`, `Supermercado.Logica/Ventas.cs:16`, `Supermercado.Logica/Inventario.cs:13`, `Supermercado.Datos/Productos.cs:51`, `Supermercado.Datos/Ventas.cs:33` | Casos de uso y repositorios concretos se agrupan por dominio en archivos planos. |
| Límites externos | `Supermercado.Logica/Seguridad.cs:231`, `Supermercado.Logica/Reportes.cs:287`, `Supermercado.Logica/ClientesExternos.cs:63`, `Supermercado.Logica/Infraestructura.cs:15`, `Supermercado.Logica/TelegramClientes.cs:407`, `Supermercado.Logica/TelegramSoporte.cs:92` | Interfaces para hash, PDF, TRM/códigos/correo, respaldo y Telegram; el dominio usa clases concretas. |
| Tiempo inyectado | `Supermercado.Datos/Contexto.cs:188` | `TimeProvider.System` se registra una vez y se usa en auditoría, FEFO y vencimientos. |
| Repositorios por módulo | `Supermercado.Datos/Contexto.cs:246`, `Supermercado.Datos/Ventas.cs:14`, `Supermercado.Datos/Productos.cs:15` | Cada repositorio expone `GuardarAsync` sobre un `SigsDbContext` compartido. |
| Composición real | `Supermercado.Logica/Composicion.cs:32`, `Supermercado.Datos/Contexto.cs:184` | `AgregarLogica()` delega en `AgregarDatos()` y registra servicios concretos, límites externos y widgets. |
| Servicios de presentación | `Supermercado.GUI/ServiciosGui.cs:20`, `Supermercado.GUI/ServiciosGui.cs:66`, `Supermercado.GUI/ServiciosGui.cs:165`, `Supermercado.GUI/ServiciosGui.cs:282` | Tema, preferencias, impresión y plantilla de tirilla agrupados en un solo archivo de GUI. |
| MVVM | `Supermercado.GUI/ViewModels/ViewModelBase.cs:5`, `Supermercado.GUI/ViewModels/PosViewModel.cs:96`, `Supermercado.GUI/ViewLocator.cs:15` | `ObservableObject`, ViewModels parciales con `[ObservableProperty]` y resolución de vistas intacta. |
| Falsos de prueba | `Supermercado.Pruebas/Falsos.cs:31`, `Supermercado.Pruebas/Falsos.cs:58`, `Supermercado.Pruebas/Falsos.cs:128` | Hash, parámetros, TRM y `ContextoPruebasSqlite` sostienen las pruebas sin red ni base de datos externa. |
| Inversión de dependencias deliberada | `Supermercado.Datos/Ventas.cs:8`, `Supermercado.Datos/Seguridad.cs:10` | Solo `IVentaRepositorio` e `IUsuarioRepositorio` conservan abstracción; el resto de repositorios se consumen como clases concretas. |

## Métricas antes/después

| Indicador | Antes (`ba8d926`) | Después (cierre del refactor) |
| :-- | :-- | :-- |
| `.cs` en Entidades | 58 | 11 |
| `.cs` en Datos | 13 | 12 |
| `.cs` en Lógica | 49 | 21 |
| `.cs` en GUI | 31 | 33 (POS en 5 archivos) |
| `.cs` en Pruebas | 67 (22 en subcarpetas) | 22 en raíz |
| Widgets | 9 | 10 |
| Interfaces totales | — | 17: Entidades 0, Datos 2, Lógica 12, GUI 3, Pruebas 0 |
| Interfaces de repositorio | 26 | 2 |
| Pruebas xUnit | 352 | 425 |
| Archivo más largo | — | 807 líneas físicas (`Supermercado.Pruebas/Falsos.cs`) |

## Decisiones de diseño

- Las interfaces permanecen en límites externos, presentación, widgets y los dos contratos de persistencia con falsos propios; los casos de uso se consumen como clases concretas.
- No hay abstracción de unidad de trabajo: los repositorios por módulo comparten el contexto y una sola llamada a `GuardarAsync` confirma cada caso de uso.
- No hay adaptadores internos: la composición directa vive en `Supermercado.Logica/Composicion.cs` y `Supermercado.Datos/Contexto.cs`.
- Las integraciones conservan sus contratos y rutas finales, con implementaciones de reserva usadas solo cuando el límite está apagado.
- La suite quedó en verde al cierre del refactor, con falsos sobre SQLite en memoria y 22 archivos planos en `Supermercado.Pruebas/`.
- `ViewLocator` y el XAML de `Views/` se mantuvieron intactos; los cambios de GUI se concentraron en `ServiciosGui.cs` y `ViewModels/`.
