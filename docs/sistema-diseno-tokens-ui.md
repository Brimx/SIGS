# Sistema de Diseño, Tokens y UX — SIGS (`Supermercado.GUI`)

Documento técnico del sistema de diseño visual, arquitectura de tokens en tres capas, estados de pantalla, ergonomía física de retail y cumplimiento regulatorio implementado en **SIGS** bajo **.NET 10** y **Avalonia 12.1.4**.

---

## 1. Arquitectura de Tokens en Tres Capas

La interfaz no declara colores, fuentes ni medidas "mágicas" o fijas en las vistas (`.axaml`). Todos los controles consumen tokens mediante `{DynamicResource}` o `{StaticResource}` organizados en tres capas desacopladas en `Supermercado.GUI/Resources/`:

```
[ Capa 1: Primitives.axaml ]  -> Valores cromáticos crudos (#HEX) en escalas Slate, Warm, Brand, Alert, HC
           │
           ▼
[ Capa 2: Semantic.axaml ]    -> Pinceles semánticos reactivos al ThemeVariant (Light, Dark, AltoContraste)
           │
[ Capa 2B: Typography.axaml]  -> Familias empaquetadas localmente, escala modular y pesos WCAG 2.2
[ Capa 2C: Geometry.axaml ]   -> Radios de borde normalizados, espaciados en múltiplos de 4/8 y sombras
           │
           ▼
[ Capa 2D: SemiBridge.axaml ] -> Claves finales que consumen los ControlTheme de Semi, mapeadas a Primitives
           │
           ▼
[ Capa 3: Controles y Vistas] -> Semi.Avalonia Theme + Controles.axaml (solo layout y clases propias)
```

`SemiBridge.axaml` es la única capa que habla el vocabulario de Semi (`ButtonSolidPrimaryBackground`,
`ComboBoxSelectorBackground`, `DataGridColumnHeaderBackground`…). Las vistas nunca usan esas claves: usan
`Theme="{DynamicResource SolidButton}"` + `Classes="Primary|Success|Warning|Danger"`.

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

Configurados mediante `ResourceDictionary.ThemeDictionaries` con tres variantes: `Light`, `Dark` y la clave
personalizada `{x:Static tema:TemaSigs.AltoContraste}` (hereda de `Dark` para los controles de Semi sin override).

**Regla de dos roles.** Un mismo color no sirve como relleno y como texto: `#22C55E` con blanco encima da 2.28:1.
Por eso cada familia tiene dos tokens:

| Rol | Tokens | Uso | Requisito |
|---|---|---|---|
| **Relleno** | `BrushPrimary`, `BrushSuccess`, `BrushWarning`, `BrushError` | Fondo de botones sólidos y badges | ≥ 4.5:1 contra `BrushTextOnPrimary` (`#FFFFFF`) |
| **Texto** | `BrushPrimaryText`, `BrushSuccessText`, `BrushWarningText`, `BrushErrorText` | Texto e iconos sobre superficie | ≥ 4.5:1 contra `BrushBackground` y `BrushSurface` |

| Token Semántico (`SolidColorBrush`) | Claro / Warm Light | Oscuro / Slate | Alto contraste | Rol en Interfaz |
|---|---|---|---|---|
| `BrushBackground` | `#FAF8F5` (Warm50) | `#0F172A` (Slate900) | `#000000` (HcBlack) | Fondo principal de la ventana y vistas. |
| `BrushSurface` | `#FFFFFF` | `#1E293B` (Slate800) | `ColorHcSurface` | Tarjetas, contenedores, barras de herramientas. |
| `BrushSurfaceRaised` | `#FFFFFF` | `#334155` (Slate700) | `ColorHcSurfaceRaised` | Modales, diálogos y menús desplegables. |
| `BrushSurfaceSubtle` | `#F2EFE9` (Warm100) | `#1E293B` (Slate800) | `ColorHcSurface` | Fondos de subcabeceras y bloques informativos. |
| `BrushSurfaceHover` | `#E8E4DC` (Warm200) | `#334155` (Slate700) | `ColorHcSurfaceRaised` | Hover en filas de grillas y botones secundarios. |
| `BrushSurfaceSelected` | `ColorBrand100` | `ColorBrand900` | `ColorHcPrimaryPressed` | Fila o tarjeta seleccionada. |
| `BrushBorderSubtle` | `#D9D3C7` (Warm300) | `#334155` (Slate700) | `ColorHcBorder` | Líneas divisorias de tablas y marcos tenues. |
| `BrushBorderStrong` | `#6F675B` (Warm600) | `#94A3B8` (Slate400) | `ColorHcBorder` | Bordes de inputs activos con foco. |
| `BrushFocusRing` | `ColorBrand600` | `ColorBrand300` | `ColorHcWarningText` | Anillo de foco visible en teclado. |
| `BrushTextPrimary` | `#292524` (Warm900) | `#F8FAFC` (Slate50) | `#FFFFFF` | Títulos, totales, datos críticos. |
| `BrushTextSecondary` | `#57534E` (Warm700) | `#CBD5E1` (Slate300) | `#FFFFFF` | Etiquetas de campo, subtítulos, metadata. |
| `BrushTextMuted` | `#6F675B` (Warm600) | `#94A3B8` (Slate400) | `ColorHcTextMuted` | Atajos de teclado (`F1`–`F5`), textos explicativos. |
| `BrushTextOnPrimary` | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | Texto sobre rellenos de color. |
| `BrushPrimary` | `#0369A1` (Brand700) | `#075985` (Brand800) | `ColorHcPrimary` | Relleno del botón principal de cobro. |
| `BrushPrimaryText` | `#0369A1` (Brand700) | `#7DD3FC` (Brand300) | `ColorHcPrimaryText` | Texto e iconos de acento sobre superficie. |
| `BrushPrimarySubtle` | `#E0F2FE` (Brand100) | `#0C4A6E` (Brand900) | `#0A2A5E` | Visor del total a pagar, badges de módulo. |
| `BrushSuccess` / `Text` / `Subtle` | `#15803D` / `#15803D` / `#F0FDF4` | `#15803D` / `#86EFAC` / `#052E16` | `ColorHc*` | Confirmar pago, devuelta al cliente. |
| `BrushWarning` / `Text` / `Subtle` | `#B45309` / `#B45309` / `#FFFBEB` | `#B45309` / `#FCD34D` / `#451A03` | `ColorHc*` | Baja de merma, caja cerrada. |
| `BrushError` / `Text` / `Subtle` | `#B91C1C` / `#B91C1C` / `#FEF2F2` | `#B91C1C` / `#FCA5A5` / `#450A0A` | `ColorHc*` | Faltante en efectivo, cerrar sesión. |
| `BrushOverlayScrim` / `Strong` | `#99020617` / `#B3020617` | igual | `#CC000000` / `#E6000000` | Velo de modales y overlays del POS. |
| `BrushOnPrimarySubtle` | `#33FFFFFF` | `#33FFFFFF` | `#33FFFFFF` | Separadores sobre relleno de color. |
| `BrushTicketPaper` / `Border` / `Ink` | `#FAF8F5` / `#D9D3C7` / `#1C1917` | igual | igual | Tirilla impresa: siempre clara, no depende del tema. |

`BrushTextTertiary` se eliminó: era idéntico a `BrushTextSecondary` y solo lo usaba `LoginView`.

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

## 3. Puente con Semi.Avalonia (`SemiBridge.axaml`)

Semi pinta los estados de sus controles directamente sobre el template
(`/template/ ContentPresenter#PART_ContentPresenter`). Un `Style` en `Button.Primario:pointerover` nunca gana esa
batalla: el resultado es el hover gris que motivó este rediseño. La solución es no reescribir estados, sino
alimentar las claves finales que Semi ya consume.

- **Qué se mapea:** `ButtonSolid*`, `ButtonOutline*`, `ButtonBorderless*`, `TextBox*`, `CheckBox*`, `ComboBox*`,
  `DataGrid*` (incluido `DataGridColumnHeader*`), `ScrollBar*`, `Flyout`, `MenuFlyout`, `ToolTip`,
  `HyperlinkButton*` y `DataValidationErrors*`, en las tres variantes.
- **Qué no funciona:** sobrescribir `SemiColor*`. Semi los resuelve con `{StaticResource}` dentro de sus propios
  diccionarios, así que un valor en `Application.Resources` llega tarde. El bridge usa claves finales.
- **Qué no se cubre:** controles que SIGS no usa. Añadir una clave solo cuando aparezca el control.
- **Vocabulario en las vistas:**

  ```xml
  <Button Classes="Primary" Theme="{DynamicResource SolidButton}" Content="Cobrar" />
  <Button Classes="Danger"  Theme="{DynamicResource SolidButton}" Content="Anular" />
  <Button Theme="{DynamicResource OutlineButton}" Content="Volver" />
  <Button Classes="Navegacion" Theme="{DynamicResource BorderlessButton}" Content="Inventario" />
  ```

  `{DynamicResource}`, no `{StaticResource}`: los `ControlTheme` viven en `Styles.Resources` de `SemiTheme` y no
  son alcanzables con `StaticResource` desde una vista.

`Controles.axaml` conserva solo lo que Semi no aporta: `Button.Navegacion` / `.NavegacionActiva` / `.Icono` /
`.DestructivoSuave` / `.Tarjeta` (layout y selección), `TextBlock.Titulo/.Etiqueta/.Ayuda/.Seccion/.Cifra` y
`Border.Tarjeta/.Scrim`. Los estilos de estado que apuntan al `Button` en lugar del template se eliminaron.

## 4. Temas, preferencias y Configuración

- **Ciclo de temas:** `Claro → Oscuro → AltoContraste → Claro` (`TemaSigs.Siguiente`). El botón de tema del shell
  cicla y persiste; la pantalla de Configuración permite elegir uno directamente.
- **Persistencia:** `IServicioPreferencias` guarda `{"tema":"Oscuro"}` en
  `~/.local/share/SIGS/preferencias.json` de forma atómica (`.tmp` + `File.Move`). La lectura es tolerante:
  archivo ausente, JSON corrupto o clave desconocida → `Claro`.
- **Arranque:** `App.axaml.cs` aplica el tema guardado antes de crear `MainWindow`, evitando el destello del tema
  por defecto. `App.axaml` ya no fija `RequestedThemeVariant`.
- **Configuración:** overlay compartido en `MainWindow.axaml`, accesible desde el login y desde el shell (pie del
  sidebar y barra compacta de cajero). Secciones: Apariencia, Legal y Acerca de. Visible para todos los roles;
  no expone configuración de negocio ni credenciales.
- **Textos legales:** `Assets/Legal/politica-privacidad.txt` y `terminos-servicio.txt`, leídos con
  `IServicioTextosLegales` desde `avares://`. Son borradores operativos sujetos a revisión jurídica.

## 5. Ergonomía de Hardware y Estados de Pantalla

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
