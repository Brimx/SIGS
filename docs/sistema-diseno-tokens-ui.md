# Sistema de Diseño, Tokens y UX — SIGS (`Supermercado.GUI`)

Documento técnico del sistema de diseño visual, arquitectura de tokens en tres capas, estados de pantalla, ergonomía física de retail y cumplimiento regulatorio implementado en **SIGS** bajo **.NET 10** y **Avalonia 12.1.4**.

---

## 1. Arquitectura de Tokens en Tres Capas

La interfaz no declara colores, fuentes ni medidas "mágicas" o fijas en las vistas (`.axaml`). Todos los controles consumen tokens mediante `{DynamicResource}` o `{StaticResource}` organizados en tres capas desacopladas en `Supermercado.GUI/Resources/`:

```
[ Capa 1: Primitives.axaml ]  -> Valores cromáticos crudos (#HEX) en escalas Slate, Warm, Brand, Alert
           │
           ▼
[ Capa 2: Semantic.axaml ]    -> Pinceles semánticos reactivos al ThemeVariant (Light, Default/Dark, Warm)
           │
[ Capa 2B: Typography.axaml]  -> Familias empaquetadas localmente, escala modular y pesos WCAG 2.2
[ Capa 2C: Geometry.axaml ]   -> Radios de borde normalizados, espaciados en múltiplos de 4/8 y sombras
           │
           ▼
[ Capa 3: Controles y Vistas] -> Semi.Avalonia Theme + Estilos locales consumiendo tokens semánticos
```

---

## 2. Catálogo Completo de Tokens

### A. Capa 1: Colores Primitivos (`Primitives.axaml`)

| Grupo / Clave | Valor HEX | Rol y Significado Visual |
|---|---|---|
| `ColorSlate50` – `ColorSlate400` | `#F8FAFC` a `#94A3B8` | Fondos claros fríos, bordes sutiles y textos atenuados. |
| `ColorSlate500` – `ColorSlate800` | `#64748B` a `#1E293B` | Superficies oscuras elevadas, textos secundarios en dark mode. |
| `ColorSlate900` / `ColorSlate950` | `#0F172A` / `#020617` | Fondo estructural principal en modo oscuro y overlays modales. |
| `ColorWarm50` / `ColorWarm100` | `#FAF8F5` / `#F2EFE9` | **Base marfil / hueso cálido anti-fatiga visual para turnos de caja prolongados.** |
| `ColorWarm200` – `ColorWarm500` | `#E8E4DC` a `#8C8477` | Bordes suaves y superficies hover en modo cálido. |
| `ColorWarm700` / `ColorWarm900` | `#57534E` / `#292524` | Texto con alto contraste (≥11:1) sin la dureza del negro puro (`#000000`). |
| `ColorBrand500` – `ColorBrand700`| `#0EA5E9` a `#0369A1` | Azul retail / petróleo institucional (acciones principales, focos). |
| `ColorSuccess50` / `500` / `700` | `#F0FDF4` / `#22C55E` / `#15803D` | Pagos exitosos, stock óptimo, folios DIAN vigentes. |
| `ColorWarning50` / `500` / `700` | `#FFFBEB` / `#F59E0B` / `#B45309` | Lotes próximos a vencer, mermas, caja sin arquear. |
| `ColorError50` / `500` / `700`   | `#FEF2F2` / `#EF4444` / `#B91C1C` | Faltante de dinero, producto bloqueado, descuadre fuera de tolerancia. |

### B. Capa 2: Pinceles Semánticos (`Semantic.axaml`)

Configurados mediante `ResourceDictionary.ThemeDictionaries`:

| Token Semántico (`SolidColorBrush`) | Modo Claro / Warm Light | Modo Oscuro (`Default`) | Rol en Interfaz |
|---|---|---|---|
| `BrushBackground` | `#FAF8F5` (Warm50) | `#0F172A` (Slate900) | Fondo principal de la ventana y vistas. |
| `BrushSurface` | `#FFFFFF` | `#1E293B` (Slate800) | Tarjetas, contenedores, barras de herramientas. |
| `BrushSurfaceRaised` | `#FFFFFF` | `#334155` (Slate700) | Modales, diálogos y menús desplegables. |
| `BrushSurfaceSubtle` | `#F2EFE9` (Warm100) | `#1E293B` (Slate800) | Fondos de subcabeceras y bloques informativos. |
| `BrushSurfaceHover` | `#E8E4DC` (Warm200) | `#334155` (Slate700) | Hover en filas de grillas y botones secundarios. |
| `BrushBorderSubtle` | `#D9D3C7` (Warm300) | `#334155` (Slate700) | Líneas divisorias de tablas y marcos tenues. |
| `BrushBorderStrong` | `#8C8477` (Warm500) | `#64748B` (Slate500) | Bordes de inputs activos con foco. |
| `BrushTextPrimary` | `#292524` (Warm900) | `#F8FAFC` (Slate50) | Títulos, totales, datos críticos (WCAG AAA ≥ 11:1). |
| `BrushTextSecondary` | `#57534E` (Warm700) | `#94A3B8` (Slate400) | Etiquetas de campo, subtítulos, metadata (≥ 4.5:1). |
| `BrushTextMuted` | `#8C8477` (Warm500) | `#64748B` (Slate500) | Atajos de teclado (`F1`–`F5`), textos explicativos. |
| `BrushTextOnPrimary` | `#FFFFFF` | `#FFFFFF` | Texto sobre botones primarios o fondos de acento. |
| `BrushPrimary` | `#0369A1` (Brand700) | `#0EA5E9` (Brand500) | Botón principal de cobro, pestañas activas. |
| `BrushPrimarySubtle` | `#E0F2FE` (Brand100) | `#0C4A6E` (Brand900) | Visor del total a pagar, badges de módulo. |
| `BrushSuccess` / `Subtle` | `#15803D` / `#F0FDF4` | `#22C55E` / `#052E16` | Botón confirmar pago, devuelta al cliente. |
| `BrushWarning` / `Subtle` | `#B45309` / `#FFFBEB` | `#F59E0B` / `#451A03` | Botón baja de merma, caja cerrada. |
| `BrushError` / `Subtle` | `#B91C1C` / `#FEF2F2` | `#EF4444` / `#450A0A` | Faltante en efectivo, botón cerrar sesión. |

### C. Capa 2B: Geometría y Elevación (`Geometry.axaml`)

- **Radios de Borde (`CornerRadius`):**
  - `RadiusNone`: `0` (filas de grilla de alta densidad).
  - `RadiusSmall`: `4px` (insignias, badges de estado, atajos).
  - `RadiusMedium`: `8px` (botones, inputs, tarjetas de métricas).
  - `RadiusLarge`: `12px` (contenedores modales y paneles estructurales).
  - `RadiusPill`: `9999px` (chips de estado de conexión y avatares).
- **Espaciados Modulares (`Thickness` en múltiplos de 4/8px):**
  - `Space2` (`2`), `Space4` (`4`), `Space8` (`8`), `Space12` (`12`), `Space16` (`16`), `Space24` (`24`), `Space32` (`32`).
  - `PaddingButton`: `16,10` (garantiza altura táctil mínima de 44px según WCAG 2.2).
  - `PaddingInput`: `12,8` (comodidad de lectura).
  - `PaddingDialog`: `24` (respiro visual en diálogos).
- **Sombras (`BoxShadows`):**
  - `ShadowElevation1`: `0 1 3 0 #1A000000` (tarjetas base).
  - `ShadowElevation2`: `0 4 6 -1 #26000000` (contenedor de login).
  - `ShadowElevation3`: `0 20 25 -5 #40000000` (modales overlay in-window).

### D. Capa 2C: Tipografía Institucional (`Typography.axaml`)

Empaquetada localmente en `Assets/Fonts/` para garantizar renderizado idéntico en Windows y Linux (evitando la saturación genérica de Inter):

1. **Marco y Títulos (`FontFamilyStackSans`):** `Stack Sans Headline` (pesos `SemiBold 600` y `Bold 700`).
2. **Lectura General y Formularios (`FontFamilyPlex`):** `IBM Plex Sans` (pesos `Regular 400`, `Medium 500` y `SemiBold 600`).
3. **Cifras, Tablas y Tirilla POS (`FontFamilyPlexCondensed`):** `IBM Plex Sans Condensed` con figuras tabulares uniformes (`+tnum`), evitando que el visor de totales o la grilla bailen al refrescarse los números.
4. **Escala de Tamaños:**
   - `FontSizeDisplay` (`32px`): Visor numérico del Total a Pagar en caja POS.
   - `FontSizeHeadline` (`24px`): Encabezados de ventana y títulos de login.
   - `FontSizeTitle` (`18px`): Títulos de tarjetas y modales.
   - `FontSizeSubtitle` (`15px`): Subtítulos y botones principales de acción.
   - `FontSizeBody` (`13px`): Texto de grilla, celdas y campos de captura.
   - `FontSizeCaption` (`11px`): Etiquetas secundarias, atajos de teclado y notas legales.

---

## 3. Ergonomía de Hardware y Estados de Pantalla

### A. Punto de Venta (POS) — Operación de Alta Velocidad
- **Distribución 70% / 30%:**
  - Lado izquierdo: Lector de código de barras, selector de adquirente y `DataGrid` transaccional con virtualización.
  - Lado derecho: Visor de Total a Pagar a 32px en bloque destacado, desglose tributario DIAN (C, M, J) y botonera de función rápida con altura mínima de 44px.
- **Foco Persistente (*Keyboard Wedge*):**
  - Implementado mediante `vm.SolicitarFocoEscaneo += EnfocarLector` en `PosView.axaml.cs`.
  - Tras escanear, agregar un producto, cerrar un modal o borrar una línea, el cursor regresa de forma automática e inmediata al campo de escaneo sin requerir clic del ratón.
- **Atajos de Teclado Físicos:**
  - `F1`: Abrir modal de cobro y finalización de venta.
  - `F2`: Modificar cantidad o gramos del producto seleccionado.
  - `F3`: Búsqueda asistida en catálogo por nombre.
  - `F4` / `Delete`: Eliminar línea activa del ticket.
  - `F5`: Abrir formulario de cliente con validación de Habeas Data.
  - `Escape`: Cancelar y limpiar ticket actual.
  - `Enter`: Procesar escaneo / confirmar modal activo.

### B. Balanzas Electrónicas GS1 (Fruver y Carnicería)
- Soporta lectura de códigos de barras EAN-13 de 13 dígitos generados por balanzas electrónicas de pesaje:
  - Prefijo: `20` a `29` (medida variable).
  - PLU: Posiciones 2 a 6 (5 dígitos).
  - Peso: Posiciones 7 a 11 (5 dígitos en gramos, ej. `00185` = 185 g = `0.185 KG`).
  - Dígito de control: Posición 12.
- Al escanear (ej. `2820890001851`), el sistema localiza el producto pesado `20890`, asigna 185 gramos y calcula el subtotal exacto sin intervención manual.

### C. Impresión Dual (80mm Continuo)
1. **ESC/POS Crudo (`ImpresorTicketEscPos`):**
   - 48 columnas de ancho calibradas con la tirilla real de Tiendas Ara.
   - Secuencia nativa de inicialización `\x1B\x40`.
   - Generación de código QR 2D bidimensional mediante secuencias `GS ( k`.
   - Corte total de papel automático `\x1D\x56\x00`.
   - Pulso eléctrico a gaveta monedero `\x1B\x70\x00\x19\xFA` (solo cuando hay cobro en efectivo).
2. **PDF de 80mm (`ImpresorTicketPdf`):**
   - Generación vectorial continua en 80mm vía `IGeneradorTicket` (QuestPDF) para copias digitales, envío por correo o auditoría.

### D. Cumplimiento Legal y Tributario
- **Resolución DIAN 000165 de 2023:** Discriminación obligatoria de bases imponibles e impuestos en pantalla y tirilla:
  - Tarifa `C`: IVA 0% (Excluido / Exento).
  - Tarifa `M`: IVA 5% (Gravado).
  - Tarifa `J`: IVA 19% (Gravado).
  - Impresión de CUFE/CUDE SHA-384 (96 caracteres hex), TrackID y enlace oficial al catálogo VPFE.
- **Ley Estatutaria 1581 de 2012 (Habeas Data):**
  - Casilla obligatoria con texto legal en el alta de clientes.
  - Si el cliente no autoriza, el sistema fuerza de manera estricta el adquirente como `CONSUMIDOR FINAL (222222222222)`.
- **Dígito de Verificación DIAN (Módulo 11):**
  - Ponderación oficial para NITs de empresas mediante `ValidadorDian.CalcularDigitoVerificacion` con factores `[3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71]`.
