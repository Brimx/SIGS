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
| RF-06 | Verificar correo al registrar o recuperar | **Parcial** | `IVerificadorCorreo` + `VerificadorCorreoNulo`; falta cliente real (Reacher) |
| RF-07 | Gestionar productos, categorías, proveedores y códigos | **Parcial** | `ServicioProductos.RegistrarAsync` y búsqueda; categorías/proveedores solo existen como entidades y semilla, sin CRUD propio |
| RF-08 | Registrar lotes con vencimiento | **Implementado** | `ServicioProductos.RegistrarIngresoLoteAsync`, `Lote` |
| RF-09 | Consulta externa por código de barras y alta manual | **Parcial** | `IConsultaCodigoBarras` + `ConsultaCodigoBarrasNula`; el alta manual funciona |
| RF-10 | Stock separado estante/bodega por producto y lote | **Implementado** | `StockEstante`, `StockBodega`, `StockLote`, tablas separadas |
| RF-11 | Descontar del estante con FEFO | **Implementado** | `ServicioInventario.DescontarVentaAsync`, `SalidaFefo`, `PlanDeSalida` |
| RF-12 | Alerta bajo mínimo y reabastecimiento | **Implementado** | `ServicioAlertasInventario`, `ServicioReabastecimiento` |
| RF-13 | Alerta de vencimiento y mermas | **Implementado** | `ServicioAlertasInventario.ObtenerLotesPorVencerAsync`, `ServicioMermas` |
| RF-14 | Abrir turno con efectivo inicial | **Implementado** | `ServicioCaja.AbrirTurnoAsync`, `TurnoCaja.Abrir` |
| RF-15 | Ventas por código, cantidades y peso | **Implementado** | `ServicioVentas`, `DetalleVenta`, `ProductoPesado` |
| RF-16 | Pagos mixtos y cambio | **Implementado** | `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia`, `Venta.Cerrar` |
| RF-17 | Ticket PDF y anulaciones auditadas | **Parcial** | Anulación completa (`ServicioVentas.AnularVentaAsync`); PDF nulo (`GeneradorTicketNulo`) |
| RF-18 | Cierre de turno y arqueo | **Implementado** | `ServicioCaja.CerrarTurnoAsync`, `TurnoCaja.CalcularEfectivoEsperado` |
| RF-19 | IVA y retenciones parametrizables | **Implementado** | `Impuesto`, `Iva`, `Retencion`, `ServicioImpuestos` |
| RF-20 | Nómina básica | **Implementado** | `Nomina.Calcular`, `ServicioNomina` |
| RF-21 | Gastos e ingresos/egresos/margen | **Implementado** | `ServicioFinanzas` |
| RF-22 | Exportar tickets, desprendibles y reportes en PDF | **Pendiente** | `IGeneradorTicket` listo; falta QuestPDF y los reportes |
| RF-23 | Widgets con datos reales y disposición por usuario | **Parcial** | `ServicioDashboard` + 3 widgets; la GUI aún no los muestra |
| RF-24 | TRM con respaldo | **Implementado** (cliente real pendiente) | `ServicioTrm`, `ITrmClient`; por defecto `ClienteTrmNoConfigurado` |

Resumen: **16 implementados, 6 parciales, 1 pendiente**. Los parciales corresponden a integraciones externas, CRUD complementario y GUI/PDF, no a reglas del núcleo.

## Requisitos no funcionales

| Requisito | Estado | Cómo se cumple |
| :-- | :-- | :-- |
| Usabilidad en caja | **Pendiente (GUI)** | La lógica está lista; faltan pantallas y atajos |
| Disponibilidad sin internet | **Parcial** | Una terminal funciona sin red; varias cajas requieren PostgreSQL local (documentado) |
| Rendimiento < 1 s por venta | **No medido** | Sin prueba de carga; el camino es una sola transacción |
| Seguridad de credenciales | **Implementado** | BCrypt, bloqueo, permisos en lógica, sin secretos en código |
| Mantenibilidad | **Implementado** | Capas, contratos, 86 pruebas, documentación |
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
