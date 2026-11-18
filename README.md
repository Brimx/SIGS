<div align="center">

# SIGS

**Sistema Integrado de Gestión para Supermercados**

Punto de venta, inventario dual estante/bodega, caja con arqueo, personal, finanzas y auditoría.

[![Windows](https://img.shields.io/badge/Windows-10%2B-0078D6?style=flat-square&logo=windows&logoColor=white)](#requisitos)
[![Linux](https://img.shields.io/badge/Linux-x64-FCC624?style=flat-square&logo=linux&logoColor=black)](#requisitos)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.1-8B44AC?style=flat-square&logo=avalonia&logoColor=white)](https://avaloniaui.net)

[![Estado](https://img.shields.io/badge/estado-en%20desarrollo-orange?style=flat-square)](#estado-del-proyecto)
[![Build](https://img.shields.io/github/actions/workflow/status/Brimx/SIGS/ci.yml?style=flat-square&label=build)](https://github.com/Brimx/SIGS/actions)
[![Pruebas](https://img.shields.io/badge/pruebas-355%20passing-brightgreen?style=flat-square)](#pruebas)
[![Licencia](https://img.shields.io/github/license/Brimx/SIGS?style=flat-square&label=licencia)](LICENSE)
[![Último commit](https://img.shields.io/github/last-commit/Brimx/SIGS?style=flat-square&label=%C3%BAltimo%20commit)](https://github.com/Brimx/SIGS/commits)

[![Documentación](https://img.shields.io/badge/Documentaci%C3%B3n-ver%20docs-4B8BBE?style=for-the-badge&logo=readthedocs&logoColor=white)](docs/)
[![Empezar](https://img.shields.io/badge/Empezar-c%C3%B3mo%20ejecutar-2EA043?style=for-the-badge&logo=dotnet&logoColor=white)](#c%C3%B3mo-ejecutar)
[![Colaborar](https://img.shields.io/badge/Colaborar-gu%C3%ADa-8957E5?style=for-the-badge&logo=git&logoColor=white)](CONTRIBUTING.md)

[Características](#características) · [Arquitectura](#arquitectura) · [Ejecutar](#cómo-ejecutar) · [Documentación](#documentación) · [Estado](#estado-del-proyecto)

</div>

> [!TIP]
> ¿Nuevo en el equipo? Empieza por [`CONTRIBUTING.md`](CONTRIBUTING.md) y [`docs/catalogo-codigo.md`](docs/catalogo-codigo.md).
> Si el proyecto te resulta útil, dale una estrella al repositorio para apoyarlo.

## Qué es

SIGS es una aplicación de escritorio en C# para el núcleo operativo de un supermercado mediano. Cubre el ciclo completo de una venta —apertura de turno, cobro, descuento de inventario, cierre y arqueo— y lo complementa con gestión de personal, cálculo de impuestos y nómina, dashboards configurables y consumo tolerante a fallos de APIs externas.

El proyecto prioriza tres cosas:

- **Reglas de negocio probadas y separadas de la interfaz.** La lógica no depende de EF Core ni de Avalonia.
- **Integridad del dinero.** Todos los montos son enteros en pesos; los porcentajes usan puntos básicos.
- **Trazabilidad.** Cada anulación, ajuste de inventario, cambio de tarifa o acción administrativa deja auditoría.

## Características

| Módulo | Qué hace hoy |
| :-- | :-- |
| **Seguridad** | Login con clave o PIN, hash BCrypt, bloqueo por intentos fallidos, permisos por rol y auditoría. |
| **Personal** | Empleados con cargo, rol, salario y estado; creación de usuario y desactivación con cierre de sesión. |
| **Productos** | Productos unitarios y pesados, categorías, proveedores, lotes, consulta externa por código de barras y códigos de balanza GS1. |
| **Inventario** | Stock separado en estante y bodega, salida FEFO, traslados, ingresos, mermas, alertas y reabastecimiento. |
| **POS** | Turnos por caja, ventas con pagos mixtos (efectivo, tarjeta, transferencia), cambio, espera y anulación con reembolso. |
| **Clientes** | Registro con documento único, validación de NIT (Módulo 11) y autorización de tratamiento de datos (Ley 1581). |
| **Caja** | Arqueo con efectivo esperado, diferencia y tolerancia configurable; el cierre descuenta las ventas anuladas. |
| **Finanzas** | IVA y retenciones parametrizables, gastos, resumen de ingresos/egresos/margen y nómina básica idempotente. |
| **Dashboards** | Nueve widgets filtrados por permiso, gráficos de barras verticales nativos, disposición por usuario, exportar/importar y reordenar. |
| **Reportes** | Ticket POS (PDF con QuestPDF y ESC/POS 80mm), desprendible de nómina y reporte de ventas. |
| **Integraciones** | TRM y consulta por código de barras (Open Food Facts) activos por defecto, apagables y con respaldo offline. |

## Stack

| Componente | Tecnología |
| :-- | :-- |
| Plataforma | .NET 10 / C# 14 |
| Interfaz | Avalonia 12 con MVVM (CommunityToolkit.Mvvm) |
| Persistencia | EF Core 10 + SQLite |
| Seguridad | BCrypt.Net-Next |
| Pruebas | xUnit + coverlet |
| CI | GitHub Actions |

## Arquitectura

```text
Supermercado.GUI ──► Supermercado.Logica ──► Supermercado.Datos ──► Supermercado.Entidades
        │                    │                      │
        └────────────────────┴──────────────────────┘
                    contratos en Entidades
```

- **Entidades**: dominio, enumeraciones y contratos de repositorio. No referencia nada.
- **Datos**: `SigsDbContext`, configuraciones EF, repositorios y semilla.
- **Lógica**: casos de uso, permisos, auditoría, cálculo y clientes externos. Depende de Datos y Entidades.
- **GUI**: Avalonia. Solo llama a `AgregarLogica()`; no referencia Datos.
- **Pruebas**: xUnit con repositorios en memoria y reloj fijo.

Detalle completo en [`docs/arquitectura.md`](docs/arquitectura.md) y decisiones en [`docs/decisiones.md`](docs/decisiones.md).

### Base de datos: hoy y alcance de `main`

- **Hoy (`main`):** SQLite local embebido, un archivo por equipo, alineado al alcance de la propuesta académica.
- **Versión extendida:** la variante con PostgreSQL dual, réplica Outbox hacia Supabase, motor DIAN UBL 2.1 y fidelización por puntos se conserva respaldada en la rama `completo`.
- **Concurrencia multi-terminal:** deuda técnica conocida para despliegues con varias cajas; documentada en [`docs/decisiones.md`](docs/decisiones.md) y [`docs/despliegue.md`](docs/despliegue.md).

## Paradigmas, SOLID, GRASP y diseño UI

- **Arquitectura y patrones:** 4 capas con Inversión de Dependencias + **MVVM** en GUI; **Repository + Unit of Work** (`Supermercado.Entidades/Contratos/`), **Strategy** (`IEstrategiaSalida` → `Supermercado.Logica/Inventario/SalidaFefo.cs:5`) y **Null Object** (`Supermercado.Logica/ExtensionesLogica.cs:82-84`).
- **Paradigmas:**
  - **POO:** jerarquías polimórficas con invariantes en `Producto`/`ProductoPesado` (`Supermercado.Entidades/Productos/Producto.cs:6`, `ProductoPesado.cs:6-14`), `Pago` y `Impuesto`.
  - **Funcional / Declarativo:** `record` inmutables (`SolicitudVenta.cs:5-7`, `IWidget.cs:31-36`) y transformaciones LINQ (`Venta.cs:27-35`).
  - **Reactivo / Eventos:** `[ObservableProperty]` y `[RelayCommand]` (`ShellViewModel.cs:27-39`, `DashboardViewModel.cs:157-202`).
  - **Asíncrono (`async`/`await`):** acceso a SQLite, APIs y refresco de interfaz sin bloquear el hilo de UI.
- **SOLID:**
  - **S:** responsabilidades aisladas (`Validador`, `SalidaFefo`, `ParserCodigoBalanza`, vistas sin lógica).
  - **O:** extensibilidad por `IWidget` (`ExtensionesLogica.cs:86-94`) y subclases de `Impuesto`/`Pago`.
  - **L:** `Venta.AgregarProducto` (`Venta.cs:37-42`) y `TotalPagado` (`Venta.cs:29`) operan sobre abstracciones base sin condicionales de tipo.
  - **I:** interfaces segregadas por consumidor (`ExtensionesLogica.cs:38-74`, `IContratosInventario.cs:6-29`).
  - **D:** `Logica` depende de contratos en `Entidades`, y `GUI` de contratos en `Logica`.
- **GRASP:** *Information Expert* (`Venta.cs:27-35`, `ProductoPesado.cs:12-13`), *Creator* (`Venta.cs:41`), *Controller* (`ServicioVentas`, `PosViewModel`), *Pure Fabrication* (`SalidaFefo`, `HasheadorBCrypt`, `Validador`), *Protected Variations* (`ITrmClient`, `IConsultaCodigoBarras`, `IImpresorTicket`) y *Polymorphism*.
- **UI (Avalonia 12 + Semi.Avalonia):** bindings compilados (`x:DataType`), `ViewLocator` (`Supermercado.GUI/App.axaml:9-11`), tokens de diseño en 5 diccionarios (`Primitives`, `Geometry`, `Typography`, `Semantic`, `SemiBridge` en `App.axaml:13-22`), scopes aislados por operación vía `IServiceScopeFactory` (`App.axaml.cs:88-98`) y gráficos de barras verticales nativos en `DashboardView.axaml:176-209`.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Windows 10+ o Linux x64
- Opcional: Rider, Visual Studio 2022+ o VS Code con C# Dev Kit

## Cómo ejecutar

```bash
git clone https://github.com/Brimx/SIGS.git
cd SIGS

dotnet restore Supermercado.slnx
dotnet build Supermercado.slnx
dotnet run --project Supermercado.GUI
```

Para cargar la base de datos con catálogo, productos, lotes, turnos, ventas y los 4 usuarios de demostración:

```bash
dotnet run --project Supermercado.GUI -- --demo
```

La base de datos se crea sola en la carpeta de datos del usuario:

- Windows: `%LOCALAPPDATA%\SIGS\sigs.db`
- Linux: `~/.local/share/SIGS/sigs.db`

En el primer arranque se siembran roles, cargos, impuestos, una caja y los parámetros. El usuario inicial es `admin`; la clave se pasa a `ServicioInicializacion.InicializarAsync` desde la capa que arranque la aplicación y se marca para cambio obligatorio.

## Pruebas

```bash
dotnet test Supermercado.Pruebas/Supermercado.Pruebas.csproj
```

Estado actual: **355 pruebas en verde**. Cubren impuestos, dinero, ventas, FEFO, arqueo, nómina, autenticación, permisos, sesión, parámetros, TRM, clientes, GUI (incluidos gráficos de dashboard), validaciones de formulario, PDF e integración con SQLite. Ver [`docs/plan-pruebas.md`](docs/plan-pruebas.md).

## Estructura del repositorio

```text
SIGS/
├── Supermercado.Entidades/   Dominio y contratos
├── Supermercado.Datos/       EF Core, repositorios, semilla
├── Supermercado.Logica/      Casos de uso y reglas
├── Supermercado.GUI/         Avalonia (shell inicial)
├── Supermercado.Pruebas/     xUnit
├── docs/                     Documentación del proyecto
├── scripts/                  Utilidades de mantenimiento
└── .github/workflows/        Integración continua
```

## Roles y permisos

| Rol | Permisos iniciales |
| :-- | :-- |
| **Admin** | Todos |
| **Gerente** | Reportes, finanzas, personal, inventario, anular ventas |
| **Cajero** | Vender, abrir/cerrar turno |
| **Bodeguero** | Inventario, ajustes, reabastecimiento, productos |

La matriz completa, incluida la verificación por método, está en [`docs/matriz-permisos.md`](docs/matriz-permisos.md).

## Parámetros

Los valores operativos viven en la tabla `Parametro` y se editan desde la aplicación:

`seguridad.max_intentos`, `inventario.dias_alerta`, `caja.tolerancia_arqueo`, `nomina.*`, `integraciones.*`, `puntos.*`, `tributario.responsable_iva` y `dian.*`.

Detalle y valores iniciales en [`docs/diccionario-datos.md`](docs/diccionario-datos.md#tabla-parametro).

## Estado del proyecto

| Bloque | Estado |
| :-- | :-- |
| Dominio y reglas de negocio | Completo |
| Persistencia SQLite + semilla | Completo (modo `--demo` disponible) |
| Seguridad, permisos y auditoría | Implementado con hallazgos abiertos (ver [`docs/pendientes-ui.md`](docs/pendientes-ui.md)) |
| Pruebas | Completo (355) |
| Documentación | Completo |
| GUI Avalonia | Completo (POS, Turno, Clientes, Inventario, Compras, Personal, Contabilidad, Dashboard con gráficos nativos, Configuración) |
| PDF con QuestPDF y ESC/POS | Implementado (ticket, nómina y reporte de ventas) |
| Concurrencia multi-terminal | Deuda técnica documentada |

Requisito por requisito en [`docs/requisitos-trazabilidad.md`](docs/requisitos-trazabilidad.md). Los pendientes que la GUI debe cubrir están en [`docs/pendientes-ui.md`](docs/pendientes-ui.md).

## Hoja de ruta

1. Migraciones EF incrementales y endurecimiento de concurrencia/secretos para entornos multi-terminal (ver [`docs/pendientes-ui.md`](docs/pendientes-ui.md)).
2. Verificación de correo con un proveedor real (Reacher u homólogo).
3. Funcionalidades empresariales (DIAN UBL 2.1, Outbox/Supabase, PostgreSQL dual y puntos) disponibles en la rama `completo`.

## Documentación

| Documento | Contenido |
| :-- | :-- |
| [Arquitectura](docs/arquitectura.md) | Capas, DI, flujos y evolución de la base de datos |
| [Diccionario de datos](docs/diccionario-datos.md) | Entidades, tablas, campos, índices y parámetros |
| [Catálogo de código](docs/catalogo-codigo.md) | Módulos, clases, constructores, métodos, DTOs y DI |
| [Convenciones](docs/convenciones.md) | Nombres, reglas de capa, dinero, reloj, commits |
| [Glosario](docs/glosario.md) | FEFO, TRM, arqueo, merma, TPH y demás |
| [Matriz de permisos](docs/matriz-permisos.md) | Rol × permiso y permiso exigido por método |
| [Modelo E-R](docs/modelo-er.md) | Diagramas Mermaid y correspondencia entidad → tabla |
| [Trazabilidad](docs/requisitos-trazabilidad.md) | RF-01…RF-24 y requisitos no funcionales |
| [Manual de usuario](docs/manual-usuario.md) | Operación por perfil y primer arranque |
| [Plan de pruebas](docs/plan-pruebas.md) | Cobertura actual y pendientes |
| [Despliegue](docs/despliegue.md) | Desarrollo con SQLite y producción con PostgreSQL |
| [Decisiones](docs/decisiones.md) | ADR del proyecto |
| [Pendientes UI](docs/pendientes-ui.md) | Handoff backend → interfaz y contrato de integración |
| [Actualización de la propuesta](docs/actualizacion-propuesta.md) | Texto listo para el documento y las slides |

## Contribuir

Lee [`CONTRIBUTING.md`](CONTRIBUTING.md) y [`docs/convenciones.md`](docs/convenciones.md). Ramas por módulo, commits convencionales y PR con pruebas verdes.

## Licencia

MIT. Ver [`LICENSE`](LICENSE).

Hecho por Cristian Paba, Santiago Montenegro y Lenin Ospino — Programación III, Universidad Popular del Cesar.
