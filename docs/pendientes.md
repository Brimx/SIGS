# Pendientes Reales del Proyecto SIGS

> Documento de control de deuda técnica, estado de backend y pendientes de interfaz gráfica (UI).
> Última actualización: **2026-11-02** tras ampliación de backend RBAC y flujos de caja (335 pruebas en verde).

---

## 1. Estado del Backend: Capacidades Listas para Conectar a la UI

El backend cuenta con contratos y casos de uso testeados para que la UI los consuma directamente sin necesidad de refactorizar la lógica de negocio:

| Módulo / Capacidad | Servicio / Contrato | Estado en Backend | Detalle |
| :-- | :-- | :--: | :-- |
| **Pagos Mixtos Simultáneos** | `IServicioVentas.RegistrarVentaAsync` | ✅ **Listo** | `SolicitudVenta` acepta múltiples `PagoEfectivo`, `PagoTarjeta`, `PagoTransferencia` y `PagoPuntos`. El backend valida que la suma cubra el total. |
| **Arqueo a Ciegas (Cierre Z)** | `IServicioCaja.CerrarTurnoAsync` | ✅ **Listo** | El cajero ingresa `efectivoContado`; el sistema calcula internamente el esperado, la diferencia y evalúa la tolerancia. |
| **Apertura de Caja con Base** | `IServicioCaja.AbrirTurnoAsync` | ✅ **Listo** | Exige base inicial en efectivo obligatoria antes de permitir cualquier facturación. |
| **Retiros Parciales (Recogidas)** | `IServicioCaja.RegistrarRetiroAsync` | ✅ **Listo** | Registra egresos de efectivo hacia caja fuerte durante el turno, valida fondos disponibles y ajusta el efectivo esperado del arqueo. |
| **Venta en Espera (Suspenso)** | `IServicioVentasEnEspera` | ✅ **Listo** | Suspende carritos en curso (`SuspenderVentaAsync`), lista pendientes por caja y recupera líneas deserializadas (`RecuperarVentaAsync`). |
| **Autorización Supervisor en Caliente** | `IServicioAutenticacion.ValidarSupervisorAsync` | ✅ **Listo** | Valida PIN o clave de supervisor para autorizar operaciones restringidas sin destruir ni alterar la sesión del cajero activo. |
| **RBAC Ampliado (18 Permisos)** | `Permiso`, `Rol`, `IServicioEstructura` | ✅ **Listo** | Banderas añadidas: `AutorizarOperacionPos`, `GestionarCompras`, `GestionarContabilidad`, `MantenimientoSistema`. Roles canónicos sembrados: Admin, Gerente, Supervisor, Cajero, Bodeguero, Compras, Contador, Soporte. |
| **Facturación DIAN y Balanzas** | `ServicioDian`, `ParserCodigoBalanza` | ✅ **Listo** | Resolución 000165/2023, CUDE SHA-384, XML UBL 2.1 y lectura de códigos pesables con prefijos 20–29. |
| **Clientes y Puntos** | `ServicioClientes`, `ServicioVentas` | ✅ **Listo** | Acumulación, redención atómica y consentimiento Habeas Data (Ley 1581 de 2012). |

---

## 2. Pendientes Reales de Interfaz Gráfica (UI / Avalonia)

Estos son los pendientes que faltan por implementar en `Supermercado.GUI`:

### P0 — Críticos de Operación en Caja (POS)

1. **Selector de Pagos Mixtos en POS:**
   * *Ubicación:* `Supermercado.GUI/ViewModels/PosViewModel.cs:136` y vista XAML.
   * *Situación:* Actualmente solo permite seleccionar un método a la vez (`MetodoPagoSeleccionado`).
   * *Acción:* Permitir agregar múltiples renglones de pago (ej. $30.000 Efectivo + $50.000 Tarjeta) y enviarlos en la lista `Pagos` de `SolicitudVenta`.

2. **Modal de Autorización de Supervisor (PIN en Caliente):**
   * *Ubicación:* Diálogo modal flotante en POS.
   * *Situación:* Al anular una venta o línea, el sistema falla si el usuario activo no es Admin/Supervisor.
   * *Acción:* Abrir modal *"Autorización de Supervisor"*, invocar `IServicioAutenticacion.ValidarSupervisorAsync(user, pin, Permiso.AutorizarOperacionPos)` y proceder con la anulación si la validación es exitosa.

3. **Gestión de Ventas en Espera (Parked Sales) en UI:**
   * *Ubicación:* `PosViewModel.cs` y barra de herramientas POS (`F6` o botón "Pausar Venta").
   * *Acción:*
     * Botón "Poner en Espera": invoca `IServicioVentasEnEspera.SuspenderVentaAsync` y limpia el carrito.
     * Botón/Badge "Ventas en Espera (N)": despliega lista emergente de tickets pausados con hora y total; al seleccionar uno, llama `RecuperarVentaAsync` y recarga las líneas en el carrito.

4. **Retiro de Efectivo en Control de Turno:**
   * *Ubicación:* `CajaTurnoViewModel.cs`.
   * *Acción:* Botón y formulario para ingresar monto, motivo y PIN de supervisor, invocando `IServicioCaja.RegistrarRetiroAsync`.

---

### P1 — Shell, Acceso y Experiencia Adaptativa

1. **Filtrado Dinámico de Módulos según Rol:**
   * *Ubicación:* `ShellViewModel.cs:46-51` y `ShellView.axaml`.
   * *Situación:* Todas las pestañas (POS, Inventario, Turno, Clientes, Dashboard) se muestran para cualquier usuario autenticado.
   * *Acción:* Ocultar pestañas si el usuario no tiene los permisos correspondientes:
     * Si el rol es **Cajero puro**: bloquear la barra lateral y mostrar únicamente la interfaz POS en pantalla completa.
     * Si es **Bodeguero / Compras**: mostrar solo Inventario y Catálogos.
     * Si es **Contador**: mostrar reportes financieros y auditoría.

2. **Login Rápido con Teclado Numérico (Touch / POS):**
   * *Ubicación:* `LoginViewModel.cs` y `LoginView.axaml`.
   * *Situación:* Pide nombre de usuario por texto antes del PIN.
   * *Acción:* Presentar teclado numérico en pantalla táctil con entrada directa de PIN o código de empleado para inicio de turno en un solo paso.

---

### P2 — Módulos de Gestión (Back-Office)

1. **Pantalla de Administración de Personal y Roles:**
   * *Ubicación:* Nuevo módulo o pestaña en Back-Office.
   * *Acción:* Interfaz CRUD conectada a `IServicioEmpleados` e `IServicioEstructura` para crear empleados, asignar cargos y configurar las banderas de permisos de cada rol.

2. **Módulo de Compras y Proveedores:**
   * *Ubicación:* Extensión de Inventario o pestaña independiente.
   * *Acción:* Vista conectada a `IServicioCatalogos` y `IServicioReabastecimiento` para gestionar proveedores y sugeridos de compra automáticos según el stock mínimo.

3. **Exportación Fiscal / Contable:**
   * *Ubicación:* `DashboardViewModel` o nueva vista "Contabilidad".
   * *Acción:* Botón de exportación a CSV/Excel de ventas por tarifa de IVA (0%, 5%, 19%), retenciones y arqueos diarios para facilitar la declaración contable.

---

## 3. Deuda Técnica y Producción (Alcance Fuera de Aula)

Aspectos documentados que exceden el alcance de una entrega de curso pero que deben contemplarse en un despliegue real:

* **Concurrencia Multi-terminal:** SQLite opera como archivo local monolítico ([ADR-008](arquitectura.md)). Para múltiples cajas concurrentes en red local se requiere migrar a PostgreSQL centralizado con bloqueos transaccionales y tokens de concurrencia ([ADR-010](decisiones.md)).
* **Configuración Dinámica de Puertos de Impresión:** La impresión ESC/POS (`0x1D 0x56 0x00`) y apertura de gaveta funcionan por socket crudo o archivo; falta una pantalla de selección de puerto COM / USB / IP en caliente.
* **Cifrado de Secretos en Reposo:** Cifrar credenciales y claves API locales almacenadas en SQLite utilizando el almacén de credenciales del sistema operativo (*OS Keyring*).
