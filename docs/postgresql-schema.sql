-- ============================================================================
-- SIGS — Esquema DDL Canónico de Producción para PostgreSQL 17+
-- ============================================================================
-- Convenciones aplicadas:
--   1. Formato limpio: cada columna, restricción y clave en su propia línea indentada.
--   2. Integridad referencial: ON DELETE RESTRICT en todas las FK para prevenir efectos dominó.
--   3. Identificadores de transacción: tipo nativo UUID (gen_random_uuid()).
--   4. Tipos semánticos:
--        - Fechas/horas operativas: TIMESTAMPTZ
--        - Fechas calendario:       DATE
--        - Dinero (COP sin float):  BIGINT
--        - Teléfonos y NITs:        VARCHAR(20) / VARCHAR(30)
--        - Porcentajes (pb):        INTEGER (100 pb = 1 %)
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ----------------------------------------------------------------------------
-- 1. Seguridad, Personal y Estructura Organizacional
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS cargos (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(80) NOT NULL UNIQUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS roles (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(80) NOT NULL UNIQUE,
    permisos BIGINT NOT NULL DEFAULT 0,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS empleados (
    id SERIAL PRIMARY KEY,
    documento VARCHAR(30) NOT NULL UNIQUE,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    correo_verificado BOOLEAN NOT NULL DEFAULT FALSE,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_ingreso DATE NOT NULL,
    salario_base BIGINT NOT NULL CHECK (salario_base >= 0),
    cargo_id INT NOT NULL,
    rol_id INT NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_empleados_cargo
        FOREIGN KEY (cargo_id)
        REFERENCES cargos (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_empleados_rol
        FOREIGN KEY (rol_id)
        REFERENCES roles (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    nombre_usuario VARCHAR(50) NOT NULL UNIQUE,
    hash_clave VARCHAR(255) NOT NULL,
    hash_pin VARCHAR(255) NULL,
    debe_cambiar_clave BOOLEAN NOT NULL DEFAULT FALSE,
    bloqueado BOOLEAN NOT NULL DEFAULT FALSE,
    intentos_fallidos INT NOT NULL DEFAULT 0,
    empleado_id INT NOT NULL UNIQUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_usuarios_empleado
        FOREIGN KEY (empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS intentos_acceso (
    id SERIAL PRIMARY KEY,
    nombre_usuario VARCHAR(50) NOT NULL,
    exitoso BOOLEAN NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ip_origen VARCHAR(45) NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ----------------------------------------------------------------------------
-- 2. Clientes y Fidelización (Ley 1581 Habeas Data)
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS clientes (
    id SERIAL PRIMARY KEY,
    documento VARCHAR(30) NOT NULL UNIQUE,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    telefono VARCHAR(20) NULL,
    correo VARCHAR(150) NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    puntos_acumulados INT NOT NULL DEFAULT 0 CHECK (puntos_acumulados >= 0),
    autoriza_tratamiento_datos BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_autorizacion TIMESTAMPTZ NULL,
    terminal_autorizacion VARCHAR(50) NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ----------------------------------------------------------------------------
-- 3. Catálogo, Impuestos e Inventario FEFO
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS categorias (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS proveedores (
    id SERIAL PRIMARY KEY,
    nit VARCHAR(30) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    telefono VARCHAR(20) NULL,
    correo VARCHAR(150) NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS impuestos (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(80) NOT NULL,
    puntos_basicos INT NOT NULL CHECK (puntos_basicos >= 0),
    discriminator VARCHAR(50) NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS productos (
    id SERIAL PRIMARY KEY,
    codigo_barras VARCHAR(50) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    precio_venta BIGINT NOT NULL CHECK (precio_venta >= 0),
    stock_minimo_estante INT NOT NULL DEFAULT 0 CHECK (stock_minimo_estante >= 0),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    categoria_id INT NOT NULL,
    proveedor_id INT NOT NULL,
    impuesto_id INT NOT NULL,
    discriminator VARCHAR(50) NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_productos_categoria
        FOREIGN KEY (categoria_id)
        REFERENCES categorias (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_productos_proveedor
        FOREIGN KEY (proveedor_id)
        REFERENCES proveedores (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_productos_impuesto
        FOREIGN KEY (impuesto_id)
        REFERENCES impuestos (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS lotes (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    numero_lote VARCHAR(50) NOT NULL,
    fecha_ingreso DATE NOT NULL,
    fecha_vencimiento DATE NOT NULL,
    cantidad_ingresada INT NOT NULL CHECK (cantidad_ingresada >= 0),
    costo_unitario BIGINT NOT NULL CHECK (costo_unitario >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_lotes_producto_numero
        UNIQUE (producto_id, numero_lote),
    CONSTRAINT fk_lotes_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS stock_estante (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    lote_id INT NOT NULL UNIQUE,
    cantidad INT NOT NULL DEFAULT 0 CHECK (cantidad >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_stock_estante_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_stock_estante_lote
        FOREIGN KEY (lote_id)
        REFERENCES lotes (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS stock_bodega (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    lote_id INT NOT NULL UNIQUE,
    cantidad INT NOT NULL DEFAULT 0 CHECK (cantidad >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_stock_bodega_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_stock_bodega_lote
        FOREIGN KEY (lote_id)
        REFERENCES lotes (id)
        ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 4. Operación de Caja y Turnos POS
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS cajas (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL UNIQUE,
    activa BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS turnos_caja (
    id SERIAL PRIMARY KEY,
    caja_id INT NOT NULL,
    empleado_id INT NOT NULL,
    fecha_apertura TIMESTAMPTZ NOT NULL,
    fecha_cierre TIMESTAMPTZ NULL,
    efectivo_inicial BIGINT NOT NULL CHECK (efectivo_inicial >= 0),
    efectivo_esperado BIGINT NOT NULL DEFAULT 0,
    efectivo_contado BIGINT NOT NULL DEFAULT 0,
    estado SMALLINT NOT NULL DEFAULT 1,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_turnos_caja_caja
        FOREIGN KEY (caja_id)
        REFERENCES cajas (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_turnos_caja_empleado
        FOREIGN KEY (empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS retiros_caja (
    id SERIAL PRIMARY KEY,
    turno_caja_id INT NOT NULL,
    monto BIGINT NOT NULL CHECK (monto > 0),
    motivo VARCHAR(200) NOT NULL,
    autorizado_por_empleado_id INT NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_retiros_caja_turno
        FOREIGN KEY (turno_caja_id)
        REFERENCES turnos_caja (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_retiros_caja_autorizador
        FOREIGN KEY (autorizado_por_empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 5. Ventas, Pagos Mixtos y POS Electrónico DIAN
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS ventas (
    id SERIAL PRIMARY KEY,
    clave_idempotencia UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    turno_caja_id INT NOT NULL,
    cliente_id INT NULL,
    total BIGINT NOT NULL CHECK (total >= 0),
    total_iva BIGINT NOT NULL DEFAULT 0 CHECK (total_iva >= 0),
    estado SMALLINT NOT NULL DEFAULT 1,
    motivo_anulacion VARCHAR(250) NULL,
    puntos_ganados INT NOT NULL DEFAULT 0 CHECK (puntos_ganados >= 0),
    puntos_redimidos INT NOT NULL DEFAULT 0 CHECK (puntos_redimidos >= 0),
    documento_adquirente VARCHAR(30) NOT NULL DEFAULT '222222222222',
    nombre_adquirente VARCHAR(150) NOT NULL DEFAULT 'CONSUMIDOR FINAL',
    cude VARCHAR(120) NULL,
    cadena_qr TEXT NULL,
    prefijo_dian VARCHAR(10) NULL,
    consecutivo_dian BIGINT NULL,
    fecha_validacion_dian TIMESTAMPTZ NULL,
    xml_ubl TEXT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ventas_turno
        FOREIGN KEY (turno_caja_id)
        REFERENCES turnos_caja (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_ventas_cliente
        FOREIGN KEY (cliente_id)
        REFERENCES clientes (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS detalles_venta (
    id SERIAL PRIMARY KEY,
    venta_id INT NOT NULL,
    producto_id INT NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    precio_unitario BIGINT NOT NULL CHECK (precio_unitario >= 0),
    subtotal BIGINT NOT NULL CHECK (subtotal >= 0),
    puntos_basicos_iva INT NOT NULL DEFAULT 0 CHECK (puntos_basicos_iva >= 0),
    iva BIGINT NOT NULL DEFAULT 0 CHECK (iva >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_detalles_venta_venta
        FOREIGN KEY (venta_id)
        REFERENCES ventas (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_detalles_venta_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS pagos (
    id SERIAL PRIMARY KEY,
    venta_id INT NOT NULL,
    monto BIGINT NOT NULL CHECK (monto > 0),
    discriminator VARCHAR(50) NOT NULL,
    ultimos_digitos VARCHAR(4) NULL,
    referencia VARCHAR(100) NULL,
    puntos INT NULL CHECK (puntos IS NULL OR puntos >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_pagos_venta
        FOREIGN KEY (venta_id)
        REFERENCES ventas (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS ventas_en_espera (
    id SERIAL PRIMARY KEY,
    identificador UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    turno_caja_id INT NOT NULL,
    cajero_id INT NOT NULL,
    nombre_cliente_temporal VARCHAR(100) NOT NULL DEFAULT 'Cliente en espera',
    canasta_json TEXT NOT NULL,
    total_estimado BIGINT NOT NULL DEFAULT 0 CHECK (total_estimado >= 0),
    fecha_pausa TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    recuperada BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ventas_en_espera_turno
        FOREIGN KEY (turno_caja_id)
        REFERENCES turnos_caja (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_ventas_en_espera_cajero
        FOREIGN KEY (cajero_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 6. Trazabilidad: Movimientos, Mermas y Reabastecimiento
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS movimientos_inventario (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    lote_id INT NOT NULL,
    venta_id INT NULL,
    tipo SMALLINT NOT NULL,
    cantidad INT NOT NULL,
    motivo VARCHAR(200) NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_movimientos_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_movimientos_lote
        FOREIGN KEY (lote_id)
        REFERENCES lotes (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_movimientos_venta
        FOREIGN KEY (venta_id)
        REFERENCES ventas (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS mermas (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    lote_id INT NOT NULL,
    cantidad INT NOT NULL CHECK (cantidad > 0),
    motivo VARCHAR(250) NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_mermas_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT,
    CONSTRAINT fk_mermas_lote
        FOREIGN KEY (lote_id)
        REFERENCES lotes (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS reabastecimientos (
    id SERIAL PRIMARY KEY,
    producto_id INT NOT NULL,
    cantidad_solicitada INT NOT NULL CHECK (cantidad_solicitada > 0),
    estado SMALLINT NOT NULL DEFAULT 1,
    fecha_solicitud TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_atencion TIMESTAMPTZ NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_reabastecimientos_producto
        FOREIGN KEY (producto_id)
        REFERENCES productos (id)
        ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 7. Finanzas, Nómina, Auditoría y Configuración
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS gastos (
    id SERIAL PRIMARY KEY,
    monto BIGINT NOT NULL CHECK (monto > 0),
    descripcion VARCHAR(250) NOT NULL,
    categoria VARCHAR(80) NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    registrado_por_empleado_id INT NOT NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_gastos_empleado
        FOREIGN KEY (registrado_por_empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS nominas (
    id SERIAL PRIMARY KEY,
    empleado_id INT NOT NULL,
    periodo_inicio DATE NOT NULL,
    periodo_fin DATE NOT NULL,
    dias_trabajados INT NOT NULL CHECK (dias_trabajados >= 0),
    salario_base BIGINT NOT NULL CHECK (salario_base >= 0),
    auxilio_transporte BIGINT NOT NULL DEFAULT 0 CHECK (auxilio_transporte >= 0),
    horas_extras BIGINT NOT NULL DEFAULT 0 CHECK (horas_extras >= 0),
    deduccion_salud BIGINT NOT NULL DEFAULT 0 CHECK (deduccion_salud >= 0),
    deduccion_pension BIGINT NOT NULL DEFAULT 0 CHECK (deduccion_pension >= 0),
    total_devengado BIGINT NOT NULL CHECK (total_devengado >= 0),
    total_deducciones BIGINT NOT NULL CHECK (total_deducciones >= 0),
    neto_pagado BIGINT NOT NULL CHECK (neto_pagado >= 0),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_nominas_empleado
        FOREIGN KEY (empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS registro_auditoria (
    id SERIAL PRIMARY KEY,
    empleado_id INT NOT NULL,
    modulo VARCHAR(80) NOT NULL,
    accion VARCHAR(100) NOT NULL,
    detalle TEXT NOT NULL,
    fecha TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_auditoria_empleado
        FOREIGN KEY (empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS parametros (
    id SERIAL PRIMARY KEY,
    clave VARCHAR(100) NOT NULL UNIQUE,
    valor TEXT NOT NULL,
    descripcion VARCHAR(250) NULL,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS configuracion_widgets (
    id SERIAL PRIMARY KEY,
    empleado_id INT NOT NULL,
    widget_id VARCHAR(80) NOT NULL,
    posicion INT NOT NULL DEFAULT 0,
    visible BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_widgets_empleado_widget
        UNIQUE (empleado_id, widget_id),
    CONSTRAINT fk_widgets_empleado
        FOREIGN KEY (empleado_id)
        REFERENCES empleados (id)
        ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------
-- 8. Patrón Outbox para Réplica Externa Asíncrona (Supabase / Neon)
-- ----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS eventos_outbox (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    identificador UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    tipo_evento VARCHAR(80) NOT NULL,
    entidad_id VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    procesado BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_procesado TIMESTAMPTZ NULL,
    creado_en TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_eventos_outbox_pendientes
    ON eventos_outbox (procesado, id);
