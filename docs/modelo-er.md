# Modelo entidad-relación

Los diagramas reflejan el modelo que EF Core crea hoy sobre SQLite. Las relaciones sin línea explícita existen como identificador, pero no como FK configurada; se validan en los servicios (ver brechas al final).

## Productos

```mermaid
erDiagram
    Categoria ||--o{ Producto : agrupa
    Proveedor ||--o{ Producto : suministra
    Impuesto  ||--o{ Producto : grava
    Producto  ||--o{ Lote : "tiene lotes"

    Categoria {
        int Id PK
        string Nombre
    }
    Proveedor {
        int Id PK
        string Nombre
        string Nit
        string Telefono
        string Correo
    }
    Producto {
        int Id PK
        string Nombre
        string CodigoBarras UK
        long PrecioVenta
        int StockMinimoEstante
        bool Activo
        int CategoriaId FK
        int ProveedorId FK
        int ImpuestoId FK
        string Discriminator
    }
    Lote {
        int Id PK
        int ProductoId FK
        string NumeroLote
        date FechaIngreso
        date FechaVencimiento
        int CantidadIngresada
        long CostoUnitario
    }
```

`Producto` es TPH: `ProductoUnitario` y `ProductoPesado` comparten la tabla `Productos` con `Discriminator`. El índice único es `(ProductoId, NumeroLote)` en `Lotes`.

## Inventario

```mermaid
erDiagram
    Lote ||--o| StockEstante : "stock en estante"
    Lote ||--o| StockBodega : "stock en bodega"
    Lote ||--o{ MovimientoInventario : registra
    Venta ||--o{ MovimientoInventario : origina
    Producto ||--o{ Merma : "pierde"
    Producto ||--o{ Reabastecimiento : solicita

    StockEstante {
        int Id PK
        int ProductoId
        int LoteId FK,UK
        int Cantidad
    }
    StockBodega {
        int Id PK
        int ProductoId
        int LoteId FK,UK
        int Cantidad
    }
    MovimientoInventario {
        int Id PK
        int ProductoId
        int LoteId FK
        int VentaId FK
        smallint Tipo
        int Cantidad
        datetime Fecha
        string Motivo
    }
    Merma {
        int Id PK
        int ProductoId
        int LoteId
        int Cantidad
        string Motivo
        datetime Fecha
    }
    Reabastecimiento {
        int Id PK
        int ProductoId
        int CantidadSolicitada
        smallint Estado
        datetime FechaSolicitud
        datetime FechaAtencion
    }
```

`StockEstante` y `StockBodega` son tablas independientes con el mismo contrato (`StockLote`). Cada lote aparece como máximo una vez por ubicación.

## Ventas y caja

```mermaid
erDiagram
    Caja ||--o{ TurnoCaja : "abre turnos"
    Empleado ||--o{ TurnoCaja : "opera"
    TurnoCaja ||--o{ Venta : registra
    Venta ||--|{ DetalleVenta : contiene
    Venta ||--o{ Pago : cobra
    Producto ||--o{ DetalleVenta : vendido

    Caja {
        int Id PK
        string Nombre
        bool Activa
    }
    TurnoCaja {
        int Id PK
        int CajaId FK
        int EmpleadoId FK
        datetime FechaApertura
        datetime FechaCierre
        long EfectivoInicial
        long EfectivoEsperado
        long EfectivoContado
        smallint Estado
    }
    Venta {
        int Id PK
        datetime Fecha
        int TurnoCajaId
        smallint Estado
        string MotivoAnulacion
    }
    DetalleVenta {
        int Id PK
        int VentaId FK
        int ProductoId FK
        int Cantidad
        long PrecioUnitario
        long Subtotal
        int PuntosBasicosIva
        long Iva
    }
    Pago {
        int Id PK
        int VentaId FK
        long Monto
        string Discriminator
        string UltimosDigitos
        string Referencia
    }
```

`Pago` es TPH: `PagoEfectivo`, `PagoTarjeta` y `PagoTransferencia` comparten la tabla `Pago`. `DetalleVenta` congela precio, subtotal y puntos básicos de IVA al momento de la venta.

## Personal y seguridad

```mermaid
erDiagram
    Cargo ||--o{ Empleado : clasifica
    Rol ||--o{ Empleado : autoriza
    Empleado ||--o| Usuario : "tiene credenciales"
    Empleado ||--o{ RegistroAuditoria : "deja rastro"

    Cargo {
        int Id PK
        string Nombre
    }
    Rol {
        int Id PK
        string Nombre UK
        long Permisos
    }
    Empleado {
        int Id PK
        string Documento UK
        string Nombres
        string Apellidos
        string Correo
        bool CorreoVerificado
        bool Activo
        date FechaIngreso
        long SalarioBase
        int CargoId FK
        int RolId FK
    }
    Usuario {
        int Id PK
        string NombreUsuario UK
        string HashClave
        string HashPin
        bool DebeCambiarClave
        bool Bloqueado
        int IntentosFallidos
        int EmpleadoId FK
    }
    RegistroAuditoria {
        int Id PK
        datetime Fecha
        int EmpleadoId
        smallint Accion
        string Entidad
        int EntidadId
        string Detalle
    }
    IntentoAcceso {
        int Id PK
        string NombreUsuario
        datetime Fecha
        bool Exitoso
        bool UsoPin
    }
```

`IntentoAcceso` no tiene FK: guarda el nombre usado, exista o no el usuario, para no perder evidencia de intentos contra cuentas inexistentes.

## Finanzas

```mermaid
erDiagram
    Impuesto ||--o{ Producto : tarifa
    Empleado ||--o{ Nomina : liquida
    Empleado ||--o{ Gasto : registra

    Impuesto {
        int Id PK
        string Nombre
        int PuntosBasicos
        bool Activo
        string Discriminator
        long BaseMinima
    }
    Nomina {
        int Id PK
        int EmpleadoId
        int Anio
        int Mes
        long SalarioBase
        long AuxilioTransporte
        long DeduccionSalud
        long DeduccionPension
        long Neto
        datetime FechaCalculo
    }
    Gasto {
        int Id PK
        string Descripcion
        long Monto
        datetime Fecha
        int RegistradoPorEmpleadoId
    }
```

`Impuesto` es TPH: `Iva` y `Retencion`. `BaseMinima` solo aplica a retención; en IVA queda en 0.

## Dashboards y configuración

```mermaid
erDiagram
    Empleado ||--o{ ConfiguracionWidget : personaliza
    Parametro {
        int Id PK
        string Clave UK
        string Valor
        string Descripcion
    }
    ConfiguracionWidget {
        int Id PK
        int EmpleadoId
        string WidgetId
        int Posicion
        bool Visible
    }
```

`(EmpleadoId, WidgetId)` es único. `Parametro` es una tabla clave-valor sin FK; su significado vive en `ClavesParametro`.

## Correspondencia entidad → tabla

| Entidad | Tabla | TPH |
| :-- | :-- | :-- |
| `Producto` / `ProductoUnitario` / `ProductoPesado` | `Productos` | Sí |
| `Impuesto` / `Iva` / `Retencion` | `Impuestos` | Sí |
| `Pago` / `PagoEfectivo` / `PagoTarjeta` / `PagoTransferencia` | `Pago` | Sí |
| `StockEstante` / `StockBodega` | Tablas propias | No |
| Resto de entidades | Nombre del DbSet | No |

## Brechas del modelo

1. **Sin FK en identificadores sueltos:** `ProductoId` en `StockEstante`, `StockBodega`, `MovimientoInventario`, `Merma` y `Reabastecimiento`; `EmpleadoId` en `Gasto`, `RegistroAuditoria`, `Nomina`, `IntentoAcceso` y `ConfiguracionWidget`. Se validan en servicios.
2. **Sin token de concurrencia:** no hay `xmin` ni `RowVersion` en stock, venta ni turno.
3. **Sin índice parcial de turno abierto:** la regla “una caja, un turno abierto” se valida en `ServicioCaja`, no en la base.
4. **`Venta.TurnoCajaId` sin navegación:** se guarda el identificador, pero no hay FK configurada.
5. **SQLite no impone longitudes ni tipos.** El esquema objetivo de PostgreSQL está en [`diccionario-datos.md`](diccionario-datos.md).

Estas brechas son deuda técnica documentada; las tres primeras se abordan en la migración a PostgreSQL descrita en [`arquitectura.md`](arquitectura.md#concurrencia-multi-terminal-deuda-técnica).
