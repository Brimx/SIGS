# Glosario

| Término | Definición |
| :-- | :-- |
| **Arqueo** | Conteo del efectivo al cerrar un turno y comparación con el esperado. |
| **BCrypt** | Algoritmo de hash con salt usado para claves y PIN. |
| **Bodega** | Depósito de reserva; el stock no está disponible para venta directa. |
| **Caja** | Punto de atención con uno o más turnos. |
| **Cargo** | Puesto organizacional del empleado; no debe confundirse con el rol, que define permisos. |
| **Cliente** | Persona registrada en el sistema; sin cliente, la venta es a consumidor final. |
| **Consolidado fiscal** | Cálculo local de `ContabilidadDianViewModel`: consolidado de ventas, desglose de IVA por tarifas C/M/J y exportación a CSV (`Declaracion_Fiscal_DIAN_*.csv`) y PDF. No emite ni transmite documentos a la DIAN. |
| **Consumidor final** | Adquirente genérico usado cuando no hay cliente o este no autorizó el tratamiento de datos. |
| **CUDE** | Retirado de `main`; conservado en rama `completo`. Código Único de Documento Equivalente; no se genera en la versión actual. |
| **Ciclo de vida DI** | Tiempo que vive un servicio en el contenedor: singleton, scoped o transient. |
| **DIAN** | Dirección de Impuestos y Aduanas Nacionales; autoridad tributaria. En `main` solo se calcula el consolidado fiscal local, sin emisión ni transmisión. |
| **Documento Equivalente POS** | Retirado de `main`; conservado en rama `completo`. En su lugar hay un consolidado fiscal local (CSV/PDF de ventas), sin emisión, firma ni transmisión. |
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
| **Outbox** | Retirado de `main`; conservado en rama `completo`. No hay eventos ni sincronización en producción: la persistencia es solo SQLite. |
| **Parámetro** | Valor operativo editable en base de datos (`seguridad.max_intentos`, etc.). |
| **Puntos** | Retirado de `main`; conservado en rama `completo`. No hay saldo de fidelización ni pago con puntos en la versión actual. |
| **Puntos básicos** | Unidad de porcentaje: 10000 = 100 %, 1900 = 19 %, 400 = 4 %. |
| **Reabastecimiento** | Solicitud de mover mercancía de bodega a estante cuando el estante baja del mínimo. |
| **RLS** | Row Level Security: políticas por fila de PostgreSQL/Supabase. Objetivo futuro fuera del alcance de `main`; conservado en rama `completo`. |
| **Rol** | Conjunto de permisos asignado a un empleado. En `main` son exactamente cuatro: `Admin`, `Gerente`, `Cajero` y `Bodeguero`. |
| **Sesión** | Usuario autenticado en memoria durante la ejecución. |
| **Scoped** | Una instancia por solicitud o alcance de DI. |
| **Singleton** | Una sola instancia durante toda la vida del proceso. |
| **Stock dual** | Separación de existencias entre estante (venta) y bodega (reserva). |
| **Tolerancia de arqueo** | Diferencia aceptada, en pesos, antes de marcar el cierre como descuadrado. |
| **TPH** | Table Per Hierarchy: una tabla para una jerarquía, con columna `Discriminator`. |
| **TRM** | Tasa Representativa del Mercado; se consulta a una API y se guarda respaldo. |
| **Turno** | Periodo entre apertura y cierre de una caja, con efectivo inicial y final. |
| **UBL** | Retirado de `main`; conservado en rama `completo`. Universal Business Language, estándar XML para facturación; en `main` no se generan documentos UBL. |
| **Unidad de trabajo** | Retirado de `main`; conservado en rama `completo`. En `main` los cambios se guardan con `SigsDbContext` (`SaveChanges` y `SaveChangesAsync`). |
| **Venta anulada** | Venta completada que se revierte con permiso, motivo, reintegro de stock y auditoría. |
| **xmin** | Token de concurrencia optimista planteado para la evolución multi-terminal; objetivo futuro fuera del alcance de `main`, conservado en rama `completo`. No implementado. |
