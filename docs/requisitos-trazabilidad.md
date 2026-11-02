# Requisitos y trazabilidad

Fuente: propuesta del proyecto (§6 y §7). Estado a la fecha de esta entrega.

## Requisitos funcionales

| Código | Requisito | Estado | Dónde vive |
| :-- | :-- | :-- | :-- |
| RF-01 | Login con usuario y contraseña; PIN para cajero | **Implementado** | `ServicioAutenticacion`, `HasheadorBCrypt`, `SesionActual` |
| RF-02 | Bloqueo tras intentos fallidos | **Implementado** | `Usuario.RegistrarFallo`, `ServicioAutenticacion`, `seguridad.max_intentos` |
| RF-03 | Restringir funciones por rol | **Implementado** | `Permiso`, `Rol.Tiene`, `SesionActual.Exigir` |
| RF-04 | Auditoría de acciones relevantes | **Implementado** | `ServicioAuditoria`, `RegistroAuditoria`, `AccionAuditoria` |
| RF-05 | Crear, editar y desactivar empleados | **Implementado** | `ServicioEmpleados` |
| RF-06 | Verificar correo al registrar o recuperar | **Parcial** | `IVerificadorCorreo`; `ClienteVerificadorCorreoHttp` solo se activa con `SIGS_EMAIL_VERIFIER_URL`; por defecto `VerificadorCorreoNulo` |
| RF-07 | Gestionar productos, categorías, proveedores y códigos | **Implementado** (GUI pendiente) | `ServicioProductos`, `ServicioMantenimientoProductos`, `ServicioCatalogos`, `ServicioBusquedaProductos`; CRUD de categorías y proveedores |
| RF-08 | Registrar lotes con vencimiento | **Implementado** | `ServicioProductos.RegistrarIngresoLoteAsync`, `Lote` |
| RF-09 | Consulta externa por código de barras y alta manual | **Implementado** (GUI pendiente) | `IConsultaCodigoBarras` + `ClienteCodigoBarrasOff` (Open Food Facts, activo por defecto y apagable, ADR-019); el alta manual funciona |
| RF-10 | Stock separado estante/bodega por producto y lote | **Implementado** | `StockEstante`, `StockBodega`, `StockLote`, tablas separadas |
| RF-11 | Descontar del estante con FEFO | **Implementado** | `ServicioInventario.DescontarVentaAsync`, `SalidaFefo`, `PlanDeSalida` |
| RF-12 | Alerta bajo mínimo y reabastecimiento | **Implementado** | `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| RF-13 | Alerta de vencimiento y mermas | **Implementado** | `ServicioAlertasInventario.ObtenerLotesPorVencerAsync`, `ServicioMermas` |
| RF-14 | Abrir turno con efectivo inicial | **Implementado** | `ServicioCaja.AbrirTurnoAsync`, `TurnoCaja.Abrir` |
| RF-15 | Ventas por código, cantidades y peso | **Implementado** | `ServicioVentas`, `DetalleVenta`, `ProductoPesado` |
| RF-16 | Pagos mixtos y cambio | **Implementado** | `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia`, `Venta.Cerrar` |
| RF-17 | Ticket PDF y anulaciones auditadas | **Implementado** | Anulación completa (`ServicioVentas.AnularVentaAsync`) y ticket con `GeneradorTicketQuestPdf` |
| RF-18 | Cierre de turno y arqueo | **Implementado** | `ServicioCaja.CerrarTurnoAsync`, `TurnoCaja.CalcularEfectivoEsperado` |
| RF-19 | IVA y retenciones parametrizables | **Implementado** | `Impuesto`, `Iva`, `Retencion`, `ServicioImpuestos` |
| RF-20 | Nómina básica | **Implementado** | `Nomina.Calcular`, `ServicioNomina` |
| RF-21 | Gastos e ingresos/egresos/margen | **Implementado** | `ServicioFinanzas` |
| RF-22 | Exportar tickets, desprendibles y reportes en PDF | **Implementado** | `GeneradorTicketQuestPdf`, `ServicioReportesQuestPdf` (nómina y ventas) |
| RF-23 | Widgets con datos reales y disposición por usuario | **Parcial** | `ServicioDashboard` + 9 widgets; la GUI aún no los muestra |
| RF-25* | Clientes con puntos: acumular y redimir en la venta | **Implementado** (GUI pendiente) | `ServicioClientes`, `ServicioVentas`, `Cliente`, `PagoPuntos`; ADR-018 |
| RF-26* | Validar formato de datos de formularios en backend | **Implementado** (GUI pendiente) | `Validador`; aplicado en empleados, proveedores y clientes |
| RF-24 | TRM con respaldo | **Implementado** | `ServicioTrm`, `ClienteTrmDatosGov` activo por defecto y apagable (ADR-019) |
| RF-27* | Documento Equivalente POS DIAN (CUDE, UBL y QR) | **En progreso** | `ServicioDian`, `GeneradorCudeDian`, `GeneradorUblDian`; sin transmisión ni firma |

\* RF-25, RF-26 y RF-27 son extensiones posteriores a la propuesta original. Resumen del cuadro original: **22 implementados, 2 parciales, 0 pendientes**. Los parciales son RF-06 (verificación de correo real) y RF-23 (GUI de widgets); el resto de integraciones y reportes ya tiene implementación. RF-27 no cuenta porque está en progreso.

## Requisitos no funcionales

| Requisito | Estado | Cómo se cumple |
| :-- | :-- | :-- |
| Usabilidad en caja | **Pendiente (GUI)** | La lógica está lista; faltan pantallas y atajos |
| Disponibilidad sin internet | **Parcial** | Una terminal funciona sin red; varias cajas requieren PostgreSQL local (documentado) |
| Rendimiento < 1 s por venta | **No medido** | Sin prueba de carga; el camino es una sola transacción |
| Seguridad de credenciales | **Parcial** | BCrypt, bloqueo y permisos en lógica; pendientes escalada de privilegios, `PagoPuntos` fabricado y credenciales demo (ver [`pendientes-ui.md`](pendientes-ui.md)) |
| Mantenibilidad | **Implementado** | Capas, contratos, 261 pruebas, documentación |
| Integridad del dinero | **Implementado** | `long` en pesos y puntos básicos enteros |
| Portabilidad Windows/Linux | **Implementado** | .NET 10, Avalonia, rutas por `LocalApplicationData` |
| Trazabilidad | **Implementado** | Auditoría de login, anulación, inventario, gastos, tarifas y parámetros |

## Decisiones del documento pendientes de confirmar

| Tema | Estado | Nota |
| :-- | :-- | :-- |
| .NET Framework 4.8 vs .NET 10 | **Resuelto en el código** | El repositorio usa .NET 10; falta confirmar con la rúbrica |
| Framework de interfaz | **Resuelto en el código** | Avalonia 12; falta confirmar restricción docente |
| Reacher en el primer demo | **Pendiente** | Se deja `VerificadorCorreoNulo`; el alta continúa sin verificar |
| Base de datos central | **Decidido, no implementado** | PostgreSQL local + Supabase réplica de una vía |
| Concurrencia multi-terminal | **Deuda documentada** | `xmin`, bloqueos y reintentos planificados |

## Cómo mantener esta tabla

1. Al cerrar un RF, cambie el estado y el archivo de referencia.
2. Si un RF se parte, cree una fila hija (`RF-07a`, etc.) en una revisión del documento fuente.
3. No marque “Implementado” un requisito que dependa de una integración nula o de una pantalla inexistente.
