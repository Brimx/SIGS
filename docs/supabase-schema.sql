-- Esquema para recepción de eventos Outbox desde SIGS hacia Supabase / Neon / PostgreSQL remoto.
-- Ejecutar en el SQL Editor de Supabase o Neon.
-- En Neon/PostgreSQL puro, reemplace el rol `anon` por el rol del cliente.
-- Las vistas usan `security_invoker` (PostgreSQL 15+).

-- 1. Tabla receptora de eventos Outbox
CREATE TABLE IF NOT EXISTS eventos_outbox (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_local INT NOT NULL,
    tipo_evento VARCHAR(80) NOT NULL,
    entidad_id VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    creado_en TIMESTAMPTZ NOT NULL,
    recibido_en TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_evento_local UNIQUE (id_local, tipo_evento)
);

CREATE INDEX IF NOT EXISTS idx_eventos_outbox_tipo ON eventos_outbox (tipo_evento);
CREATE INDEX IF NOT EXISTS idx_eventos_outbox_creado_en ON eventos_outbox (creado_en);

-- 2. Row Level Security: el cliente SIGS solo inserta; no lee ni borra.
--    Use la clave `anon`, nunca `service_role`, en la estación de caja.
ALTER TABLE eventos_outbox ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Permitir insercion de eventos" ON eventos_outbox;
DROP POLICY IF EXISTS "insertar_eventos_outbox" ON eventos_outbox;

CREATE POLICY "insertar_eventos_outbox" ON eventos_outbox
    FOR INSERT
    TO anon
    WITH CHECK (true);

-- La lectura remota (vistas) NO se abre a `anon` por defecto.
-- Hágalo desde un backend con `service_role` o cree una política SELECT
-- acotada al rol `authenticated` si necesita consultar desde la nube.

-- 3. Vistas analíticas convenientes para consulta remota

-- Vista de ventas completadas
CREATE OR REPLACE VIEW vista_ventas_remotas AS
SELECT 
    (payload->>'Id')::INT AS venta_id,
    (payload->>'Fecha')::TIMESTAMPTZ AS fecha,
    (payload->>'TurnoCajaId')::INT AS turno_caja_id,
    (payload->>'Total')::BIGINT AS total,
    (payload->>'TotalIva')::BIGINT AS total_iva,
    (payload->>'ClienteId')::INT AS cliente_id,
    (payload->>'PuntosGanados')::INT AS puntos_ganados,
    (payload->>'PuntosRedimidos')::INT AS puntos_redimidos,
    (payload->>'ClaveIdempotencia') AS clave_idempotencia,
    recibido_en
FROM eventos_outbox
WHERE tipo_evento = 'VentaCompletada';

ALTER VIEW vista_ventas_remotas SET (security_invoker = true);

-- Vista de anulaciones
CREATE OR REPLACE VIEW vista_anulaciones_remotas AS
SELECT 
    (payload->>'VentaId')::INT AS venta_id,
    payload->>'Motivo' AS motivo,
    (payload->>'FechaAnulacion')::TIMESTAMPTZ AS fecha_anulacion,
    recibido_en
FROM eventos_outbox
WHERE tipo_evento = 'VentaAnulada';

ALTER VIEW vista_anulaciones_remotas SET (security_invoker = true);
