# Glosario

| Término | Definición |
| :-- | :-- |
| **Arqueo** | Conteo del efectivo al cerrar un turno y comparación con el esperado. |
| **BCrypt** | Algoritmo de hash con salt usado para claves y PIN. |
| **Bodega** | Depósito de reserva; el stock no está disponible para venta directa. |
| **Caja** | Punto de atención con uno o más turnos. |
| **Cargo** | Puesto organizacional del empleado (Admin, Gerente, Cajero, Bodeguero). |
| **Cliente** | Persona registrada para el programa de puntos; sin cliente, la venta es a consumidor final. |
| **Consumidor final** | Adquirente genérico usado cuando no hay cliente o este no autorizó el tratamiento de datos. |
| **CUDE** | Código Único de Documento Equivalente; hash SHA-384 que identifica el documento POS. |
| **Ciclo de vida DI** | Tiempo que vive un servicio en el contenedor: singleton, scoped o transient. |
| **DIAN** | Dirección de Impuestos y Aduanas Nacionales; autoridad de la facturación y del documento equivalente. |
| **Documento Equivalente POS** | Comprobante electrónico de venta con CUDE, UBL y QR; en este repositorio se genera localmente. |
| **Descuento FEFO** | First Expired, First Out: sale primero el lote que vence antes. |
| **Diferencia de arqueo** | `EfectivoContado − EfectivoEsperado`; positiva es sobrante, negativa faltante. |
| **Dirty read / carrera** | Dos operaciones concurrentes leen el mismo estado y una pisa a la otra. |
| **Estante** | Ubicación de venta; su stock se descuenta al vender. |
| **FEFO** | Ver *Descuento FEFO*. |
| **GS1 / código de balanza** | EAN-13 con prefijo 20–29 que codifica PLU y gramos. |
| **Habeas Data** | Autorización del cliente para tratar sus datos personales (Ley 1581). |
| **Idempotencia** | Propiedad de una operación de aplicarse una sola vez aunque se repita la solicitud. |
| **IVA incluido** | El precio de góndola ya contiene el impuesto; el sistema lo extrae, no lo suma. |
| **Lote** | Cantidad de un producto con número, fecha de ingreso, vencimiento y costo. |
| **Merma** | Pérdida de mercancía por daño, vencimiento o error; descuenta stock y deja registro. |
| **Movimiento** | Registro de ingreso, traslado, venta, devolución, merma o ajuste. |
| **Nómina** | Cálculo mensual de salario, auxilio y deducciones de un empleado. |
| **Outbox** | Patrón que escribe el evento de negocio en la misma transacción local y lo despacha después. |
| **Parámetro** | Valor operativo editable en base de datos (`seguridad.max_intentos`, etc.). |
| **Puntos** | Saldo de fidelización del cliente; se acumula por compra y se redime como pago. |
| **Puntos básicos** | Unidad de porcentaje: 10000 = 100 %, 1900 = 19 %, 400 = 4 %. |
| **Reabastecimiento** | Solicitud de mover mercancía de bodega a estante cuando el estante baja del mínimo. |
| **RLS** | Row Level Security: políticas por fila de PostgreSQL/Supabase. |
| **Rol** | Conjunto de permisos asignado a un empleado. |
| **Sesión** | Usuario autenticado en memoria durante la ejecución. |
| **Scoped** | Una instancia por solicitud o alcance de DI. |
| **Singleton** | Una sola instancia durante toda la vida del proceso. |
| **Stock dual** | Separación de existencias entre estante (venta) y bodega (reserva). |
| **Tolerancia de arqueo** | Diferencia aceptada, en pesos, antes de marcar el cierre como descuadrado. |
| **TPH** | Table Per Hierarchy: una tabla para una jerarquía, con columna `Discriminator`. |
| **TRM** | Tasa Representativa del Mercado; se consulta a una API y se guarda respaldo. |
| **Turno** | Periodo entre apertura y cierre de una caja, con efectivo inicial y final. |
| **UBL** | Universal Business Language; estándar XML usado por la DIAN (versión 2.1). |
| **Unidad de trabajo** | Abstracción sobre `SaveChanges`; agrupa todos los cambios en una transacción. |
| **Venta anulada** | Venta completada que se revierte con permiso, motivo, reintegro de stock y auditoría. |
| **xmin** | Columna de sistema de PostgreSQL usada como token de concurrencia optimista. |
