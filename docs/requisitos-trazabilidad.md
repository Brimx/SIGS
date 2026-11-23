# Requisitos y trazabilidad

Fuente: propuesta del proyecto (§6 y §7). Estado a la fecha de esta entrega, alineado con el código actual de `main` y con `docs/plan-pruebas.md` (443 pruebas).

## Requisitos funcionales

| Código | Requisito | Estado | Dónde vive |
| :-- | :-- | :-- | :-- |
| RF-01 | Login con usuario y contraseña; PIN para cajero | **Implementado** | `ServicioAutenticacion`, `HasheadorBCrypt`, `SesionActual` |
| RF-02 | Bloqueo tras intentos fallidos | **Implementado** | `Usuario.RegistrarFallo`, `ServicioAutenticacion`, `seguridad.max_intentos` |
| RF-03 | Restringir funciones por rol | **Implementado** | `Permiso` (`Ninguno` + 18 banderas), `Rol.Tiene`, `SesionActual.Exigir`; 4 roles: `Admin`, `Gerente`, `Cajero`, `Bodeguero` (`Supermercado.Entidades/Personal.cs`, `Supermercado.Datos/Semillas.cs`) |
| RF-04 | Auditoría de acciones relevantes | **Implementado** | `ServicioAuditoria`, `RegistroAuditoria`, `AccionAuditoria` |
| RF-05 | Crear, editar y desactivar empleados | **Implementado** | `ServicioEmpleados` |
| RF-06 | Verificar correo al registrar o recuperar | **Parcial** | `IVerificadorCorreo`; `ClienteVerificadorCorreoHttp` solo se activa con `SIGS_EMAIL_VERIFIER_URL`; por defecto `VerificadorCorreoNulo` |
| RF-07 | Gestionar productos, categorías, proveedores y códigos | **Implementado** (GUI pendiente) | `ServicioProductos`, `ServicioMantenimientoProductos`, `ServicioCatalogos`, `ServicioBusquedaProductos`; CRUD de categorías y proveedores |
| RF-08 | Registrar lotes con vencimiento | **Implementado** | `ServicioProductos.RegistrarIngresoLoteAsync`, `Lote` |
| RF-09 | Consulta externa por código de barras y alta manual | **Implementado** (GUI pendiente) | `IConsultaCodigoBarras` + `ClienteCodigoBarrasOff` (Open Food Facts, activo por defecto y apagable con `SIGS_APIS_HABILITADAS` / `SIGS_OFF_HABILITADA`, ADR-019); el alta manual funciona |
| RF-10 | Stock separado estante/bodega por producto y lote | **Implementado** | `StockEstante`, `StockBodega`, `StockLote`, tablas separadas |
| RF-11 | Descontar del estante con FEFO | **Implementado** | `ServicioInventario.DescontarVentaAsync`, `SalidaFefo`, `PlanDeSalida` |
| RF-12 | Alerta bajo mínimo y reabastecimiento | **Implementado** | `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| RF-13 | Alerta de vencimiento y mermas | **Implementado** | `ServicioAlertasInventario.ObtenerLotesPorVencerAsync`, `ServicioMermas` |
| RF-14 | Abrir turno con efectivo inicial | **Implementado** | `ServicioCaja.AbrirTurnoAsync`, `TurnoCaja.Abrir` |
| RF-15 | Ventas por código, cantidades y peso | **Implementado** | `ServicioVentas`, `DetalleVenta`, `ProductoPesado` |
| RF-16 | Pagos mixtos y cambio | **Implementado** | `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia`, `Venta.Cerrar`; no hay `PagoPuntos` en `main` |
| RF-17 | Ticket PDF y anulaciones auditadas | **Implementado** | Anulación completa (`ServicioVentas.AnularVentaAsync`) y ticket con `GeneradorTicketQuestPdf` |
| RF-18 | Cierre de turno y arqueo | **Implementado** | `ServicioCaja.CerrarTurnoAsync`, `TurnoCaja.CalcularEfectivoEsperado` |
| RF-19 | IVA y retenciones parametrizables | **Implementado** | `Impuesto`, `Iva`, `Retencion`, `ServicioImpuestos` |
| RF-20 | Nómina básica | **Implementado** | `Nomina.Calcular`, `ServicioNomina` |
| RF-21 | Gastos e ingresos/egresos/margen | **Implementado** | `ServicioFinanzas` |
| RF-22 | Exportar tickets, desprendibles y reportes en PDF | **Implementado** | `GeneradorTicketQuestPdf`, `ServicioReportesQuestPdf` (nómina y ventas) |
| RF-23 | Widgets con datos reales y disposición por usuario | **Parcial** | `ServicioDashboard` y los `IWidget` registrados en `AgregarLogica`; `DashboardView` muestra y permite organizar las tarjetas, sin LiveCharts2 |
| RF-24 | TRM con respaldo | **Implementado** | `ServicioTrm`, `ClienteTrmDatosGov`, `ServicioParametros.GuardarRespaldoAsync`; interruptores `SIGS_APIS_HABILITADAS` / `SIGS_TRM_HABILITADA` en `OpcionesApisExternas.DesdeEntorno()` (ADR-019) |
| RF-25* | Clientes con puntos: acumular y redimir en la venta | **Retirado de `main`; conservado en rama `completo`** | `GestionarClientes` existe como permiso, pero no hay `PagoPuntos`, `PuntosAcumulados`, `PuntosGanados`, `PuntosRedimidos` ni parámetros `puntos.*`. ADR-018 es historia |
| RF-26* | Validar formato de datos de formularios en backend | **Implementado** (GUI pendiente) | `Validador` (incluye `CalcularDigitoVerificacionNit`); aplicado en empleados, proveedores y clientes |
| RF-27* | Documento Equivalente POS DIAN (CUDE, UBL y QR) | **Retirado de `main`; conservado en rama `completo`** | No hay `IServicioDian`, `ServicioDian`, `GeneradorCudeDian`, `GeneradorUblDian`, CUDE, UBL, QR ni parámetros `dian.*`. Queda consolidado fiscal local en `ContabilidadDianViewModel`, desglose IVA C/M/J, CSV/PDF, `PlantillaTicketPos.ObtenerCodigoTarifa`, DV de NIT en `Validador.CalcularDigitoVerificacionNit`, textos legales y datos de adquirente; sin emisión, firma ni transmisión. ADR-022 es historia |

\* RF-25, RF-26 y RF-27 son extensiones posteriores a la propuesta original. RF-25 y RF-27 están retirados de `main` y conservados en rama `completo`; RF-26 sigue vigente. Estado vigente del cuadro: **23 implementados, 2 parciales, 2 retirados**. Los parciales son RF-06 (verificación de correo real) y RF-23 (capa visual con LiveCharts2). El conteo de pruebas es **443**, según `docs/plan-pruebas.md`.

## Requisitos no funcionales

| Requisito | Estado | Cómo se cumple |
| :-- | :-- | :-- |
| Usabilidad en caja | **Parcial** | Existen auto-enfoque del lector, atajos `F1`–`F5`, tema persistido y overlay de Configuración (ADR-026 y ADR-028); faltan pruebas y flujos de GUI |
| Disponibilidad sin internet | **Parcial** | `main` solo usa SQLite (`UseSqlite` en `Supermercado.Datos/Contexto.cs`) y una terminal funciona sin red. La operación multi-terminal es objetivo futuro en rama `completo` (ADR-008, ADR-010 y ADR-027) |
| Rendimiento < 1 s por venta | **No medido** | Sin prueba de carga; el guardado usa `RepositorioBase.GuardarAsync` (`Db.SaveChangesAsync`), sin unidad de trabajo |
| Seguridad de credenciales | **Parcial** | BCrypt (`IHasheadorClave`), bloqueo y permisos (`SesionActual.Exigir`); quedan revisar escalada de privilegios y credenciales demo (ver [`pendientes-ui.md`](pendientes-ui.md)). No hay `PagoPuntos` en `main` |
| Mantenibilidad | **Implementado** | Capas, repositorios concretos con dos contratos (`IUsuarioRepositorio`, `IVentaRepositorio`), 443 pruebas (`docs/plan-pruebas.md`) y documentación |
| Integridad del dinero | **Implementado** | `long` en pesos y puntos básicos enteros |
| Portabilidad Windows/Linux | **Implementado** | .NET 10, Avalonia, rutas por `LocalApplicationData` |
| Trazabilidad | **Implementado** | Auditoría de login, anulación, inventario, gastos, tarifas y parámetros |

## Decisiones del documento pendientes de confirmar

| Tema | Estado | Nota |
| :-- | :-- | :-- |
| .NET Framework 4.8 vs .NET 10 | **Resuelto en el código** | El repositorio usa .NET 10; falta confirmar con la rúbrica |
| Framework de interfaz | **Resuelto en el código** | Avalonia 12; falta confirmar restricción docente |
| Reacher en el primer demo | **Pendiente** | Se deja `VerificadorCorreoNulo`; el alta continúa sin verificar |
| Base de datos central | **Retirado de `main`; objetivo futuro en rama `completo`** | `main` solo usa SQLite; no hay Npgsql/`UseNpgsql`, Supabase ni Docker (ADR-008 y ADR-027) |
| Concurrencia multi-terminal | **Deuda documentada** | `xmin`, bloqueos y reintentos planificados sobre el objetivo futuro de PostgreSQL en rama `completo` (ADR-010) |

## Cómo mantener esta tabla

1. Al cerrar un RF, cambie el estado y el archivo de referencia.
2. Si un RF se parte, cree una fila hija (`RF-07a`, etc.) en una revisión del documento fuente.
3. No marque “Implementado” un requisito que dependa de una integración nula o de una pantalla inexistente.
4. Los requisitos retirados se conservan con el estado indicado en el cuadro; no se borren.
