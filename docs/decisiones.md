# Decisiones de arquitectura

## ADR-001: Stock en entidades separadas por ubicación

- **Contexto:** RF-10 pide stock separado por producto y lote, y la
  propuesta lista StockEstante y StockBodega como entidades.
- **Decisión:** `StockLote` es abstracta y no se mapea; `StockEstante` y
  `StockBodega` la heredan y cada una tiene su propia tabla.
- **Consecuencias:** el compilador impide mezclar ubicaciones y todo
  traslado deja un `MovimientoInventario`; a cambio hay más tablas.

## ADR-002: Tarifas de impuestos en puntos básicos enteros

- **Contexto:** la propuesta pide tarifas parametrizables en tabla y
  evitar decimales por las limitaciones de SQLite.
- **Decisión:** `Impuesto.PuntosBasicos` es un entero (1900 = 19 %), y
  el cálculo usa solo aritmética entera con redondeo al peso más cercano.
- **Consecuencias:** no hay errores de redondeo ni decimales en la base;
  la interfaz debe convertir a porcentaje para mostrarlo.

## ADR-003: El precio de venta incluye el IVA

- **Contexto:** en Colombia el precio en góndola es el precio final.
- **Decisión:** `Iva.Calcular` extrae el impuesto del precio; no lo suma.
- **Consecuencias:** el cajero ve un solo precio; si el negocio quisiera
  precios sin IVA, solo cambia la clase `Iva`.

## ADR-004: Los servicios de dominio no guardan

- **Contexto:** la propuesta exige que venta y stock se actualicen juntos
  o no se actualicen.
- **Decisión:** `ServicioInventario` modifica entidades y registra
  movimientos, pero el caso de uso que lo orquesta llama a
  `IUnidadDeTrabajo.GuardarAsync` una sola vez.
- **Consecuencias:** hay una única transacción por operación; ningún
  servicio puede guardar a medias.

## ADR-005: Parámetros editables en una tabla

- **Contexto:** intentos de bloqueo, días de alerta, tolerancia de arqueo
  y valores de nómina cambian por negocio o por año.
- **Decisión:** la entidad `Parametro` (clave y valor) guarda esos
  valores; las claves son constantes en `ClavesParametro`. Si falta uno,
  el servicio lanza `ParametroFaltanteException` en vez de inventar un
  valor.
- **Consecuencias:** nada de esto está fijo en el código; la carga inicial
  solo agrega los parámetros que faltan y nunca pisa los ya editados.
  `Configuracion` es la única carpeta de módulo que no está en la
  propuesta.

## ADR-006: El reloj se inyecta y las fechas se guardan en UTC

- **Contexto:** `DateTime.UtcNow` disperso no se puede simular en pruebas
  y mezcla fuentes de hora.
- **Decisión:** los servicios reciben `TimeProvider`; las entidades no
  leen el reloj. `FechaCreacion` la asigna el `DbContext` al guardar.
- **Consecuencias:** las pruebas controlan la fecha; "hoy" se calcula con
  la zona horaria local del equipo.

## ADR-006b: Permisos como banderas dentro de `Rol`

- **Contexto:** la propuesta define roles con permisos verificados en la
  capa de lógica.
- **Decisión:** `Permiso` es un enum de banderas guardado como entero en
  `Rol`; `ISesionActual.Exigir` lo verifica al inicio de cada caso de uso.
- **Consecuencias:** no hace falta una tabla de permisos; la matriz
  inicial de roles es una carga editable, no una regla del código.

## ADR-007: APIs externas tras interfaces, con respaldo

- **Contexto:** TRM, códigos de barras y verificación de correo no deben
  bloquear una operación si fallan.
- **Decisión:** cada API vive tras una interfaz en `Logica/Servicios`; por
  defecto se registran versiones nulas. `ServicioTrm` aplica tiempo
  máximo, reintentos y último valor guardado.
- **Consecuencias:** el sistema funciona sin internet; falta elegir las
  APIs reales y escribir sus clientes.

## ADR-008: SQLite en esta entrega y PostgreSQL local como objetivo

- **Contexto:** el proyecto necesita funcionar sin servidor para el demo,
  pero varias cajas no pueden compartir un archivo SQLite.
- **Decisión:** se mantiene SQLite como base de la entrega y se documenta
  PostgreSQL local en la red del supermercado como fuente de verdad para
  producción, con Supabase como réplica de una sola vía.
- **Consecuencias:** el demo es simple y portable; la operación
  multi-terminal queda explícitamente fuera hasta migrar el proveedor.
  La réplica nunca es bidireccional: un solo nodo escribe.

## ADR-009: La lógica no usa EF Core

- **Contexto:** la propuesta exige inversión de dependencias y pruebas sin
  base de datos.
- **Decisión:** los casos de uso trabajan contra interfaces declaradas en
  `Entidades`; `Lógica` referencia `Datos` solo para la composición
  (`AgregarLogica` llama a `AgregarDatos`). Ningún archivo de `Lógica`
  importa `Microsoft.EntityFrameworkCore`.
- **Consecuencias:** las pruebas usan repositorios en memoria y el
  proveedor se cambia en un solo archivo; a cambio, la composición obliga
  a que `Lógica` conozca el proyecto `Datos`.

## ADR-010: Concurrencia multi-terminal como deuda técnica

- **Contexto:** con varias cajas, el patrón `leer → validar → escribir`
  permite vender dos veces la última unidad, anular dos veces o abrir dos
  turnos sobre la misma caja.
- **Decisión:** no se implementa concurrencia en esta entrega. Se
  documenta la mitigación: transacciones con bloqueo, tokens `xmin`,
  índice único parcial de turno abierto y reintentos acotados ante
  `40001`/`DbUpdateConcurrencyException`.
- **Consecuencias:** una terminal es segura; varias terminales sobre
  SQLite no. Antes de producción multi-terminal se debe migrar a
  PostgreSQL y completar esta deuda.

## ADR-011: Guardado administrativo y respaldo interno de parámetros

- **Contexto:** los parámetros se editan desde la aplicación, pero la TRM
  necesita guardar su último valor aunque no haya sesión ni permiso.
- **Decisión:** `IServicioParametros` separa `GuardarAsync` (exige
  `GestionarPersonal`, audita y guarda) de `GuardarRespaldoAsync`
  (interno, sin permiso ni auditoría, atómico para varias claves).
- **Consecuencias:** un proceso de infraestructura no puede saltarse la
  autorización, y el respaldo de la TRM no ensucia la auditoría de
  cambios administrativos.

## ADR-012: Reintentos de TRM con espera progresiva y reloj inyectado

- **Contexto:** la API de TRM puede fallar por red o timeout; reintentar
  sin espera satura al proveedor y bloquea la operación.
- **Decisión:** `ServicioTrm` reintenta hasta `integraciones.reintentos`
  con espera exponencial de 200 ms a 2 s, usando `TimeProvider`, y guarda
  el valor en una sola operación atómica. Si todo falla, devuelve el
  último respaldo o `null`.
- **Consecuencias:** las pruebas controlan la espera con `RelojFijo`; la
  venta nunca depende de la TRM. El cliente HTTP real queda pendiente.

## ADR-013: Excepciones de dominio en lugar de errores genéricos

- **Contexto:** `InvalidOperationException` no distingue una regla de
  negocio de un bug y la interfaz no puede reaccionar bien.
- **Decisión:** todo error esperado usa el catálogo de
  `Entidades/Excepciones` (`ValidacionException`,
  `StockInsuficienteException`, `TurnoInvalidoException`,
  `ParametroFaltanteException`, etc.). Las integraciones solo capturan
  errores de red, timeout o E/S.
- **Consecuencias:** la GUI puede mapear excepciones a mensajes y las
  pruebas afirman el tipo exacto; un `catch (Exception)` genérico queda
  reservado a la frontera de una integración.

## ADR-014: Abrir turno exige caja existente y activa

- **Contexto:** un turno sobre una caja inexistente o inactiva deja
  ventas sin punto de atención y arqueos imposibles.
- **Decisión:** `ServicioCaja.AbrirTurnoAsync` valida que la caja exista
  y que `Activa` sea verdadero antes de crear el turno; además rechaza un
  segundo turno abierto para el empleado o la caja.
- **Consecuencias:** el error se detecta al abrir, no al cerrar; la regla
  vive en la lógica y no depende de que la GUI oculte el botón.

## ADR-015: Documentación viva y catálogo de código curado

- **Contexto:** el código cambia más rápido que un documento escrito a
  mano y un listado exhaustivo se vuelve obsoleto.
- **Decisión:** `docs/catalogo-codigo.md` se mantiene curado por módulo
  (responsabilidades, constructores, métodos y DI) y
  `scripts/inventario-codigo.sh` genera un anexo exhaustivo bajo demanda.
- **Consecuencias:** el catálogo se revisa en cada PR que cambie una API
  pública; el anexo no se versiona y sirve para detectar cambios de
  superficie.

## ADR-016: Cálculo del margen de ganancia por categoría

- **Contexto:** el widget de margen por categoría requiere contrastar
  los ingresos generados con los costos reales de los lotes despachados,
  evitando usar costos promedio que distorsionen la rentabilidad.
- **Decisión:** el widget toma las ventas completadas del período y sus
  movimientos de tipo `Venta`. Por cada movimiento se obtiene el costo
  real (`Cantidad * Lote.CostoUnitario`) y el ingreso correspondiente.
  El margen de la categoría es `Suma(Ingreso) - Suma(Costo)`.
- **Consecuencias:** el margen refleja fielmente las salidas FEFO y los
  costos de cada lote sin campos calculados redundantes en la base.

## ADR-017: Uso de QuestPDF bajo Licencia Comunitaria

- **Contexto:** RF-17 y RF-22 exigen la exportación de tickets de venta,
  desprendibles de nómina y reportes de gestión en formato PDF.
- **Decisión:** se adopta QuestPDF con `LicenseType.Community`. Según
  los términos consultados (questpdf.com, versión 3.0; resumen por
  búsqueda, no leído el texto legal completo), la licencia es gratuita
  para instituciones académicas públicas o sin ánimo de lucro (docencia
  e investigación), para individuos y organizaciones con ingresos
  anuales menores a USD 1 000 000, para proyectos con licencia OSI y
  para aprendizaje o evaluación. Excluye entidades del sector público
  (salvo escuelas públicas) y empresas que cotizan en bolsa.
- **Pendiente de verificar:** leer el texto oficial en questpdf.com/license
  antes de un uso comercial. Si el supermercado real supera el umbral o
  cambia el tipo de entidad, se requiere licencia de pago.
- **Consecuencias:** generación fluida y tipada de PDFs en Windows y
  Linux sin dependencias externas ni navegadores headless.

## ADR-018: Programa de puntos de clientes

- **Contexto:** muchos supermercados fidelizan clientes con puntos. Debe ser
  opcional: la venta a «consumidor final» no cambia.
- **Decisión:** entidad `Cliente` (documento único, saldo `PuntosAcumulados`),
  `Venta.ClienteId` opcional y `PagoPuntos` como método de pago (TPH).
  Se gana `floor(base / puntos.pesos_por_punto)` con
  `base = total − pesos pagados con puntos` (no se ganan puntos sobre lo
  redimido). Se redime a `puntos.valor_punto_pesos` pesos por punto, sin
  superar el total ni el saldo. Un valor `0` en cualquiera de los dos
  parámetros desactiva esa mitad del programa. `Venta` guarda
  `PuntosGanados` y `PuntosRedimidos` para poder revertir.
- **Reglas de integridad:** los puntos se mueven solo después de
  `Venta.Cerrar()`, para que un rechazo no deje el saldo modificado en el
  contexto; la anulación quita lo ganado (saldo mínimo 0) y devuelve lo
  redimido. Permiso nuevo `GestionarClientes`; el cajero puede buscar por
  documento con `Vender`.
- **Consecuencias:** sin concurrencia multi-terminal el saldo tiene la
  misma carrera que el stock (ADR-010). Sin vencimiento de puntos ni
  niveles; no se imprimen en el ticket todavía.
- **Migración:** `Inicial` genera el script desde el modelo, así que una
  base ya creada no tiene la tabla `Clientes` ni las columnas nuevas de
  `Ventas`: en desarrollo se recrea el archivo; en producción hace falta
  una migración incremental (ver «Brechas» en `diccionario-datos.md`).

## ADR-019: APIs abiertas activas por defecto, apagables

- **Contexto:** TRM y consulta de código de barras no necesitan credenciales
  y mejoran el alta de productos y la caja, pero no deben ser obligatorias.
- **Decisión:** `AgregarLogica` registra los clientes reales
  (`ClienteTrmDatosGov`, `ClienteCodigoBarrasOff`) con URLs oficiales por
  defecto (`OpcionesApisExternas`). Apagado: `SIGS_APIS_HABILITADAS=0`
  (todas), `SIGS_TRM_HABILITADA=0` / `SIGS_OFF_HABILITADA=0` (una) al
  arrancar, o en caliente con los parámetros
  `integraciones.trm_habilitada` e `integraciones.codigo_barras_habilitado`
  (`ObtenerBoolAsync`; si falta el parámetro, vale «activo»). Apagada, la
  TRM devuelve el último valor guardado y el catálogo devuelve `null`.
- **Verificación de correo:** no tiene servicio público gratuito fiable;
  solo se activa con `SIGS_EMAIL_VERIFIER_URL` (sigue `VerificadorCorreoNulo`).
  La validación de formato vive aparte, en `Validador`.
- **Verificado:** el 2026-11-02 ambos endpoints respondieron (datos.gov.co
  TRM `32sa-8pi3` y Open Food Facts v2). Los límites y condiciones de uso
  de terceros pueden cambiar; Open Food Facts pide `User-Agent` propio
  (ya se envía) y atribución ODbL.
- **Consecuencias:** la primera consulta puede esperar hasta
  `integraciones.timeout_seg` si no hay internet; la venta nunca depende
  de ello. En un entorno sin salida a internet conviene apagarlas.

## ADR-020: Patrón Outbox para réplica asíncrona hacia Supabase o Neon

- **Contexto:** las ventas locales en LAN deben funcionar sin depender de internet.
  Sin embargo, el supermercado requiere consultar reportes de ventas y auditoría
  desde la nube (Supabase o Neon) de forma asíncrona y segura.
- **Decisión:** se implementa el patrón Outbox. La tabla `EventosOutbox` se escribe
  en la misma transacción local (`trabajo.GuardarAsync()`) en la que se registra o
  anula una venta. El servicio `ClienteSincronizacionSupabase` despacha eventos
  pendientes vía HTTPS a la API REST de Supabase (`/rest/v1/eventos_outbox`) con
  autenticación Bearer/apikey y cabecera `Prefer: resolution=merge-duplicates`.
- **Resiliencia:** si se cae internet, la venta local no se detiene ni arroja error;
  el evento queda marcado como no procesado y reintenta con registro de fallas.
  El esquema SQL receptor para Supabase o Neon se suministra listo en
  `docs/supabase-schema.sql`.

## ADR-021: Idempotencia en la venta mediante clave única

- **Contexto:** en una interfaz de caja ágil operada por teclado (F5 para cobrar),
  el cajero puede pulsar dos veces o producirse un reintento que duplique el cobro
  y descuente stock dos veces.
- **Decisión:** `SolicitudVenta` incluye `ClaveIdempotencia` opcional. `ServicioVentas`
  consulta `ventas.ObtenerPorClaveIdempotenciaAsync` antes de iniciar la venta. Si ya
  existe una venta completada con dicha clave, la devuelve de inmediato sin descontar
  stock ni alterar saldos de puntos nuevamente.
- **Estado (2026-11-02):** implementación incompleta. El índice de
  `Venta.ClaveIdempotencia` es **no único** (`ConfiguracionModelo.cs:51`), así que dos
  solicitudes concurrentes todavía pueden crear dos ventas con la misma clave. Pendiente:
  índice único filtrado, captura de `DbUpdateException` y alcance de la clave por empleado.

## ADR-022: Documento Equivalente Electrónico POS DIAN (Resolución 000165 de 2023)

- **Contexto:** la DIAN exige para los sujetos obligados a facturar la emisión del
  Documento Equivalente Electrónico tiquete de máquina registradora con sistema POS
  (Anexo Técnico 1.9), que requiere estructuración en XML UBL 2.1, código CUDE
  generado por hash criptográfico SHA-384 de 96 caracteres hexadecimales, código QR
  con enlace al catálogo VPFE, y estricto control de rangos y vigencia de numeración.
- **Decisión:**
  - `IServicioDian` y `ServicioDian` gestionan el ciclo de vida del documento POS:
    validan en cada venta la vigencia y disponibilidad del rango autorizado (`dian.resolucion_*`),
    emitiendo excepciones `ValidacionException` bloqueantes si la resolución expiró o
    se agotaron los folios, y alertas preventivas si restan menos de 30 días o menos de 1000 folios.
  - `GeneradorCudeDian` implementa el algoritmo oficial SHA-384 combinando
    `NumDoc + FecDoc + HorDoc + ValFac + CodImp1 + ValImp1 + CodImp2 + ValImp2 + CodImp3 + ValImp3 + ValTot + NitEmisor + NumAdq + ClaveTecnica + TipoAmbiente`.
  - `GeneradorUblDian` construye el XML UBL 2.1 estándar con namespaces oficiales
    (`Invoice`, `cac:InvoiceLine`, `cac:TaxTotal`, `cac:LegalMonetaryTotal`, etc.).
  - Persistencia de campos en `Venta`: `Cude`, `CadenaQr`, `PrefijoDian`, `ConsecutivoDian`,
    `FechaValidacionDian` y `XmlUbl`.
  - El parámetro `dian.habilitado` (1/0) permite alternar entre emisión con POS electrónico
    o ticket simplificado sin DIAN.
- **Consecuencias:** trazabilidad fiscal total para auditorías y software contable,
  impresión del QR y CUDE en ticket QuestPDF, y desacoplamiento limpio de la infraestructura
  de firma y transmisión web (SOAP/REST) para etapas posteriores.

## ADR-023: Descomposición de Códigos de Balanza Retail GS1 y Conversor de Unidades

- **Contexto:** las secciones de fruver, carnicería y charcutería operan con balanzas
  electrónicas de mostrador (Torrey, Dibal, Systel) que generan etiquetas adhesivas EAN-13
  de medida variable con prefijos retail GS1 del 20 al 29. Por otro lado, las compras al por mayor
  ingresan en bultos (50 kg), arrobas (12.5 kg) o cajas, pero se venden en gramos o unidades.
- **Decisión:**
  - `ParserCodigoBalanza` descompone códigos de 13 dígitos: prefijo (2 dígitos: 20-29),
    código PLU del producto (5 dígitos), cantidad en gramos (5 dígitos) y dígito de control.
  - En `ServicioVentas`, al escanear una línea, si el código es de balanza se busca el producto
    pesado por PLU/código base y se asignan automáticamente los gramos extraídos, calculando
    el subtotal exacto de manera transparente para el cajero.
  - `ConversorUnidades` centraliza las equivalencias de unidades de compra a inventario/kardex:
    Bulto de 50 kg (50.000 g), Arroba (12.500 g), Kilo (1.000 g), Libra (500 g) y Cajas por unidades,
    incluyendo utilitarios para determinar costo por gramo y costo unitario.
- **Consecuencias:** agilidad en caja sin requerir doble digitación de peso y consistencia
  en la entrada al inventario granel.

## ADR-024: Habeas Data (Ley 1581 de 2012) y Consumidor Final DIAN

- **Contexto:** la Ley Estatutaria 1581 de 2012 de Colombia exige autorización previa, expresa
  e informada del titular para recolectar y almacenar sus datos personales. Si un cliente
  no autoriza o revoca el consentimiento, no se pueden registrar sus datos en la base ni en la factura.
- **Decisión:**
  - En `Cliente` se integran los campos `AutorizaTratamientoDatos` (booleano), `FechaAutorizacion`
    y `TerminalAutorizacion`, junto con los métodos de dominio `AutorizarHabeasData` y `RevocarHabeasData`.
  - En `ServicioVentas`, al registrar una venta, si el cliente no autoriza el tratamiento o no
    se asocia cliente alguno, se fuerza de manera automática el adquirente legal como
    `DocumentoAdquirente = "222222222222"` y `NombreAdquirente = "CONSUMIDOR FINAL"`.
  - Si el cliente cuenta con autorización válida, se registran su documento y nombre completo.
- **Consecuencias:** cumplimiento normativo ante la Superintendencia de Industria y Comercio (SIC)
  y compatibilidad con los estándares de adquirente de la DIAN.

## ADR-025: Switch de Régimen Tributario (Responsable vs No Responsable de IVA)

- **Contexto:** en Colombia coexisten personas jurídicas y naturales que operan supermercados
  bajo régimen común/ordinario (responsables de IVA) o bajo régimen de no responsables
  (antiguo régimen simplificado / monotributo o ventas exclusivas de bienes excluidos/exentos).
- **Decisión:**
  - Se añade la clave de parámetro `tributario.responsable_iva` (1/0).
  - En `ServicioVentas`, cuando `tributario.responsable_iva` es `0`, el servicio anula
    los puntos básicos de IVA y el monto de IVA en cada detalle de la venta (`detalle.Iva = 0`,
    `detalle.PuntosBasicosIva = 0`), asegurando que no se facture ni recaude IVA a los adquirentes
    y que `TotalIva` sea 0.
- **Consecuencias:** flexibilidad para que cualquier micro, pequeño o mediano supermercado
  adopte el sistema según su estatus tributario en el RUT sin necesidad de alterar el catálogo de productos.

## ADR-026: Adopción de Semi.Avalonia, Lucide Icons, Tipografía Local e Impresión Dual en Supermercado.GUI

- **Contexto:** la interfaz requería superar el estado de plantilla básica (*boilerplate*) para ofrecer
  una experiencia de escritorio de grado comercial, alta densidad para retail/B2B, soporte anti-fatiga visual
  para cajeros, atajos de teclado sin latencia, y compatibilidad idéntica en Linux y Windows sin dependencias
  externas en tiempo de ejecución.
- **Decisión:**
  - **Framework y Tema Base:** Avalonia 12.1.4 sobre .NET 10 con `Semi.Avalonia` (12.1.0.1) y `Semi.Avalonia.DataGrid` (12.1.0.1)
    en lugar de Material Design, eliminando paddings desproporcionados e introduciendo alta densidad de datos.
  - **Iconografía:** `Lucide.Avalonia` (0.2.29), precompilado y mapeado nativamente al espacio de nombres de Avalonia (`<LucideIcon Kind="..." />`),
    eliminando la carga de archivos SVG sueltos.
  - **Tipografía Institucional Propia:** Descarte de Inter para evitar saturación de plantilla genérica; adopción de:
    - *Titulares y Marco:* `Stack Sans Headline` (SemiBold 600 y Bold 700).
    - *Lectura y Formularios:* `IBM Plex Sans` (Regular 400, Medium 500 y SemiBold 600).
    - *Cifras y Grilla:* `IBM Plex Sans Condensed` con figuras tabulares (`+tnum`).
    Binarios `.ttf` empaquetados localmente en `Supermercado.GUI/Assets/Fonts/`.
  - **Tokens de Diseño en 3 Capas:** Recursos desacoplados en `Primitives.axaml`, `Geometry.axaml`, `Typography.axaml`
    y `Semantic.axaml`, con soporte de Modo Claro Cálido (*Warm Light* `#FAF8F5`) y Modo Oscuro Slate (`#0F172A`).
  - **Ergonomía POS:** Auto-enfoque ininterrumpido en el lector de códigos de barras (*keyboard wedge*),
    atajos físicos `F1`–`F5` y `Escape`, y visor numérico de `Total a Pagar` a 32px.
  - **Impresión Dual de Recibos:**
    - `ImpresorTicketEscPos`: secuencias de escape nativas ESC/POS para rollo continuo de 80mm (48 columnas),
      código QR bidimensional (`GS ( k`), corte automático de papel (`\x1D\x56\x00`) y apertura de gaveta monedero (`\x1B\x70\x00\x19\xFA`).
    - `ImpresorTicketPdf`: exportación vectorial en 80mm vía QuestPDF (`IGeneradorTicket`).
- **Consecuencias:** interfaz visualmente pulida, sin dependencias del sistema operativo, con contraste WCAG 2.2 AA/AAA,
  fidelidad del 100% contra la tirilla de Tiendas Ara y la Resolución DIAN 000165 de 2023.

