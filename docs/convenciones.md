# Convenciones

## Nombres

| Elemento | Convención | Ejemplo |
| :-- | :-- | :-- |
| Proyectos | `Supermercado.<Capa>` | `Supermercado.Logica` |
| Clases | PascalCase, sustantivo | `ServicioVentas`, `TurnoCaja` |
| Interfaces | `I` + PascalCase | `IServicioVentas`, `IRepositorio<T>` |
| Servicios | `Servicio` + módulo | `ServicioInventario` |
| Repositorios | `<Entidad>Repositorio` | `VentaRepositorio` |
| Widgets | `<Nombre>Widget` | `VentasDelDiaWidget` |
| Excepciones | `<Motivo>Exception` | `StockInsuficienteException` |
| Métodos asíncronos | Sufijo `Async` | `RegistrarVentaAsync` |
| Propiedades y tipos | PascalCase | `PrecioVenta`, `EstadoVenta` |
| Parámetros y locales | camelCase | `productoId`, `filas` |
| Constantes | PascalCase | `GramosPorKilo` |
| Campos privados | `_camelCase` | `_siguienteId` |
| Tablas y columnas | PascalCase singular/plural según DbSet | `Productos`, `VentaId` |

El dominio se escribe en **español** (`Venta`, `Empleado`, `Reabastecimiento`). Los términos técnicos universales se mantienen en inglés cuando no hay traducción natural (`DbContext`, `Widget`, `Async`).

## Reglas de capa

1. **Entidades no conocen EF Core, Avalonia ni servicios.** Solo dominio y validaciones propias.
2. **Nada de lógica de negocio en la GUI.** La vista llama a un servicio de Lógica.
3. **La GUI no referencia Datos.** Solo `AgregarLogica()`.
4. **Los servicios de dominio no guardan.** El caso de uso llama `IUnidadDeTrabajo.GuardarAsync` una sola vez.
5. **Los permisos se verifican en Lógica**, no ocultando botones.
6. **Las APIs externas viven tras una interfaz.** Las fuentes abiertas sin credenciales (TRM, código de barras) se activan por defecto y se pueden apagar; las que requieren secreto o proveedor (correo, Supabase) solo se registran si están configuradas.

## Dinero y porcentajes

- Todo monto es `long` en pesos colombianos. Nunca `double` ni `decimal` en la base.
- Los porcentajes son `int` en puntos básicos: `1900 = 19 %`, `400 = 4 %`.
- Use `Dinero.DividirRedondeando` y `Dinero.Porcentaje`; no replique la fórmula.
- El IVA se **extrae** de un precio que ya lo incluye.
- Los productos pesados reciben gramos y dividen entre `1000` con redondeo.

## Fechas y reloj

- Inyecte `TimeProvider`; no use `DateTime.UtcNow` ni `DateTime.Today`.
- Use `reloj.AhoraUtc()` para timestamps y `reloj.HoyLocal()` para vencimientos.
- `FechaCreacion` la asigna `SigsDbContext`; no la fije a mano.
- Guarde `DateTime` en UTC y `DateOnly` para fechas sin hora.

## Errores

Use el catálogo unificado en `Supermercado.Entidades/Excepciones/ExcepcionesDominio.cs`:

| Situación | Excepción |
| :-- | :-- |
| Regla de negocio | `ValidacionException` |
| Venta/pago inválido | `VentaInvalidaException` |
| Stock | `StockInsuficienteException` |
| Turno | `TurnoInvalidoException` |
| Permiso | `AccesoDenegadoException` |
| Login | `CredencialesInvalidasException` / `CuentaBloqueadaException` |
| Sesión | `SesionNoIniciadaException` / `DebeCambiarClaveException` |
| Parámetro | `ParametroFaltanteException` |
| Integración | `ServicioNoConfiguradoException` |

No capture `Exception` genérica salvo en la frontera de una integración, y solo para errores de red/timeout. Un `catch (Exception)` que silencia un bug propio está prohibido.

## Datos personales y DIAN

- No imprima datos del cliente si `AutorizaTratamientoDatos` es falso; use `Venta.DocumentoConsumidorFinal` y `NombreConsumidorFinal`.
- La autorización se registra con fecha y terminal (`AutorizarHabeasData`); la revocación no borra el historial.
- Los secretos de integración (`supabase_key`, `trm_token`, `resend_token`, `dian.clave_tecnica`) no se versionan; van en configuración o parámetros locales.
- Los campos DIAN (`Cude`, `XmlUbl`, `CadenaQr`) se generan localmente; no los trate como comprobante válido hasta completar transmisión y firma.
- El avance del consecutivo DIAN debe quedar en la misma transacción que la venta; hoy es una deuda conocida (ADR-022).

## Pruebas

- Un archivo por área (`VentasTests`, `ServicioCajaTests`, …).
- Nombres en español que describan la regla: `AnularDosVeces_SoloDevuelveElStockUnaVez`.
- Use `RelojFijo` y repositorios en memoria; no toque la base real.
- Cada prueba debe poder correr sola y no depender del orden.
- Toda corrección de un bug relevante agrega una prueba que lo reproduzca.

## Comentarios

- Comente **por qué**, no **qué**. El código ya dice qué hace.
- Los comentarios van en español y en una sola línea cuando sea posible.
- No deje `TODO` sin dueño ni fecha; si es deuda, documéntela en `docs/decisiones.md`.

## Git

### Ramas

```text
main
feat/<modulo>-<tema>
fix/<modulo>-<tema>
docs/<tema>
test/<tema>
ci/<tema>
```

### Commits

Formato convencional, en español, imperativo y corto:

```text
feat(ventas): reembolsar anuladas en el arqueo
fix(inventario): impedir stock negativo en el reparto
test(seguridad): cubrir bloqueo por empleado inactivo
docs(adr): documentar PostgreSQL local y Supabase
ci: compilar y probar en GitHub Actions
```

Prefijos usados: `feat`, `fix`, `refactor`, `test`, `docs`, `ci`, `chore`.

### Pull request

- Una rama por bloque lógico.
- Descripción con qué cambia, por qué y cómo se probó.
- `dotnet build` sin advertencias y `dotnet test` en verde.
- Si toca el modelo, actualice `docs/diccionario-datos.md` y `docs/modelo-er.md`.
- Si toca permisos, actualice `docs/matriz-permisos.md`.
- Si toca una integración, actualice `docs/arquitectura.md`.

## Formato

- 4 espacios, sin tabs.
- Llaves en línea nueva (estilo Allman), como el resto del repositorio.
- Líneas de hasta ~88 caracteres cuando sea razonable.
- `var` cuando el tipo es evidente; tipo explícito cuando aclara.
- Una clase pública por archivo con el mismo nombre del archivo. En contratos de persistencia, implementaciones de repositorios, excepciones de dominio, widgets y módulos de soporte/presentación, se permite y prefiere agrupar tipos estrechamente cohesivos en archivos temáticos unificados (`IContratos*.cs`, `Repositorios*.cs`, `ExcepcionesDominio.cs`, `Widgets.cs`, `ConfiguracionYTema.cs`, `ImpresionTicket.cs`, `ClientesExternos.cs`, `ServiciosSoporte.cs`) para mantener un árbol de proyecto limpio y navegable.
- `sealed` en las hojas de una jerarquía TPH.
