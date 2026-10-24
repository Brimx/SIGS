# Glosario

| Término | Definición |
| :-- | :-- |
| **Arqueo** | Conteo del efectivo al cerrar un turno y comparación con el esperado. |
| **BCrypt** | Algoritmo de hash con salt usado para claves y PIN. |
| **Bodega** | Depósito de reserva; el stock no está disponible para venta directa. |
| **Caja** | Punto de atención con uno o más turnos. |
| **Cargo** | Puesto organizacional del empleado (Admin, Gerente, Cajero, Bodeguero). |
| **Ciclo de vida DI** | Tiempo que vive un servicio en el contenedor: singleton, scoped o transient. |
| **Descuento FEFO** | First Expired, First Out: sale primero el lote que vence antes. |
| **Diferencia de arqueo** | `EfectivoContado − EfectivoEsperado`; positiva es sobrante, negativa faltante. |
| **Dirty read / carrera** | Dos operaciones concurrentes leen el mismo estado y una pisa a la otra. |
| **Estante** | Ubicación de venta; su stock se descuenta al vender. |
| **FEFO** | Ver *Descuento FEFO*. |
| **IVA incluido** | El precio de góndola ya contiene el impuesto; el sistema lo extrae, no lo suma. |
| **Lote** | Cantidad de un producto con número, fecha de ingreso, vencimiento y costo. |
| **Merma** | Pérdida de mercancía por daño, vencimiento o error; descuenta stock y deja registro. |
| **Movimiento** | Registro de ingreso, traslado, venta, devolución, merma o ajuste. |
| **Nómina** | Cálculo mensual de salario, auxilio y deducciones de un empleado. |
| **Parámetro** | Valor operativo editable en base de datos (`seguridad.max_intentos`, etc.). |
| **Puntos básicos** | Unidad de porcentaje: 10000 = 100 %, 1900 = 19 %, 400 = 4 %. |
| **Reabastecimiento** | Solicitud de mover mercancía de bodega a estante cuando el estante baja del mínimo. |
| **Rol** | Conjunto de permisos asignado a un empleado. |
| **Sesión** | Usuario autenticado en memoria durante la ejecución. |
| **Scoped** | Una instancia por solicitud o alcance de DI. |
| **Singleton** | Una sola instancia durante toda la vida del proceso. |
| **Stock dual** | Separación de existencias entre estante (venta) y bodega (reserva). |
| **Tolerancia de arqueo** | Diferencia aceptada, en pesos, antes de marcar el cierre como descuadrado. |
| **TPH** | Table Per Hierarchy: una tabla para una jerarquía, con columna `Discriminator`. |
| **TRM** | Tasa Representativa del Mercado; se consulta a una API y se guarda respaldo. |
| **Turno** | Periodo entre apertura y cierre de una caja, con efectivo inicial y final. |
| **Unidad de trabajo** | Abstracción sobre `SaveChanges`; agrupa todos los cambios en una transacción. |
| **Venta anulada** | Venta completada que se revierte con permiso, motivo, reintegro de stock y auditoría. |
| **xmin** | Columna de sistema de PostgreSQL usada como token de concurrencia optimista. |
