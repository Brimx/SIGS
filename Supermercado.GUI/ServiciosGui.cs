using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using Supermercado.Entidades.Configuracion;
using Supermercado.Entidades.Productos;
using Supermercado.Entidades.Ventas;
using Supermercado.Logica.Reportes;
using Supermercado.Logica.Seguridad;

namespace Supermercado.GUI.Servicios.Tema
{
    /// <summary>
    /// Variantes visuales soportadas por SIGS. El modo claro es cálido (anti-fatiga para turnos
    /// largos de caja), el oscuro usa la escala Slate y el alto contraste prioriza legibilidad.
    /// </summary>
    public static class TemaSigs
    {
        public const string ClaveClaro = "Claro";
        public const string ClaveOscuro = "Oscuro";
        public const string ClaveAltoContraste = "AltoContraste";

        public static readonly ThemeVariant AltoContraste =
            new(ClaveAltoContraste, ThemeVariant.Dark);

        public static string[] Claves { get; } = [ClaveClaro, ClaveOscuro, ClaveAltoContraste];

        public static ThemeVariant Resolver(string? clave) => clave switch
        {
            ClaveOscuro => ThemeVariant.Dark,
            ClaveAltoContraste => AltoContraste,
            _ => ThemeVariant.Light,
        };

        public static string ClaveDe(ThemeVariant variante)
        {
            if (variante == ThemeVariant.Light) return ClaveClaro;
            if (variante == ThemeVariant.Dark) return ClaveOscuro;
            if (variante == AltoContraste) return ClaveAltoContraste;
            return ClaveClaro;
        }

        public static string Siguiente(string? clave)
        {
            var indice = Array.IndexOf(Claves, clave);
            return Claves[(indice + 1) % Claves.Length];
        }

        public static void Aplicar(string? clave) => Aplicar(Resolver(clave));

        public static void Aplicar(ThemeVariant variante)
        {
            if (Application.Current is { } app)
                app.RequestedThemeVariant = variante;
        }
    }
}

namespace Supermercado.GUI.Servicios.Preferencias
{
    using Tema;

    public interface IServicioPreferencias
    {
        string LeerTema();
        void GuardarTema(string clave);
    }

    public sealed class ServicioPreferencias : IServicioPreferencias
    {
        private static readonly JsonSerializerOptions Opciones = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly string _ruta;

        public ServicioPreferencias(string? ruta = null)
        {
            _ruta = ruta ?? RutaPorDefecto();
        }

        public static string RutaPorDefecto() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIGS",
            "preferencias.json");

        public string LeerTema()
        {
            try
            {
                if (!File.Exists(_ruta))
                    return TemaSigs.ClaveClaro;

                var preferencias = JsonSerializer.Deserialize<Preferencias>(File.ReadAllText(_ruta), Opciones);
                return preferencias?.Tema is { } tema && TemaSigs.Claves.Contains(tema)
                    ? tema
                    : TemaSigs.ClaveClaro;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return TemaSigs.ClaveClaro;
            }
        }

        public void GuardarTema(string clave)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
                var temporal = _ruta + ".tmp";
                File.WriteAllText(temporal, JsonSerializer.Serialize(new Preferencias { Tema = clave }, Opciones));
                File.Move(temporal, _ruta, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        private sealed class Preferencias
        {
            public string? Tema { get; set; }
        }
    }
}

namespace Supermercado.GUI.Servicios.Legal
{
    public interface IServicioTextosLegales
    {
        string PoliticaPrivacidad();
        string TerminosServicio();
    }

    public sealed class ServicioTextosLegales : IServicioTextosLegales
    {
        private const string Base = "avares://Supermercado.GUI/Assets/Legal/";

        public string PoliticaPrivacidad() => Leer("politica-privacidad.txt");

        public string TerminosServicio() => Leer("terminos-servicio.txt");

        private static string Leer(string archivo)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(Base + archivo));
                using var lector = new StreamReader(stream);
                return lector.ReadToEnd();
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or UriFormatException)
            {
                return "No fue posible cargar el texto legal. Consulte con el administrador del sistema.";
            }
        }
    }
}

namespace Supermercado.GUI.Servicios.Impresion
{
    public interface IImpresorTicket
    {
        Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default);
        Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default);
        Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default);
        Task AbrirGavetaAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Adaptador de impresión y exportación en PDF de 80mm mediante QuestPDF
    /// para envío al correo del cliente, copia contable o auditoría de supervisión.
    /// </summary>
    public class ImpresorTicketPdf(
        IGeneradorTicket generadorTicket,
        ServicioParametros parametros) : IImpresorTicket
    {
        public string CarpetaExportacion { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIGS",
            "TicketsPdf");

        public Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default)
            => PlantillaTicketPos.GenerarTextoTirillaAsync(venta, parametros);

        public Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default)
            => generadorTicket.GenerarPdfAsync(venta);

        public async Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default)
        {
            Directory.CreateDirectory(CarpetaExportacion);
            var pdfBytes = await generadorTicket.GenerarPdfAsync(venta);

            var rutaPdf = Path.Combine(CarpetaExportacion, $"Ticket_POS{venta.Id}.pdf");

            await File.WriteAllBytesAsync(rutaPdf, pdfBytes, ct);
        }

        public Task AbrirGavetaAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }

    /// <summary>
    /// Adaptador de impresión térmica ESC/POS directa para rollo de 80mm (Epson TM, Star Micronics, Bixolon).
    /// Soporta comandos nativos de corte de papel, apertura de gaveta monedero y código QR bidimensional.
    /// </summary>
    public class ImpresorTicketEscPos(ServicioParametros parametros) : IImpresorTicket
    {
        // Secuencias estándar ESC/POS
        private static readonly byte[] CmdInicializar = [0x1B, 0x40]; // ESC @
        private static readonly byte[] CmdAvanceLineas = [0x1B, 0x64, 0x04]; // ESC d 4
        private static readonly byte[] CmdCortePapelTotal = [0x1D, 0x56, 0x00]; // GS V 0
        private static readonly byte[] CmdPulsoGaveta = [0x1B, 0x70, 0x00, 0x19, 0xFA]; // ESC p 0 25 250

        public string CarpetaSpool { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIGS",
            "Tickets");

        public Task<string> FormatearTirillaTextoAsync(Venta venta, CancellationToken ct = default)
            => PlantillaTicketPos.GenerarTextoTirillaAsync(venta, parametros);

        public async Task<byte[]> GenerarComandosEscPosAsync(Venta venta, CancellationToken ct = default)
        {
            var textoTirilla = await FormatearTirillaTextoAsync(venta, ct);
            using var ms = new MemoryStream();

            // 1. Inicializar impresora
            ms.Write(CmdInicializar);

            // 2. Cuerpo del tiquete en codificación Latin1 / CP858 compatible con impresoras térmicas
            var bytesTexto = Encoding.Latin1.GetBytes(textoTirilla);
            ms.Write(bytesTexto);

            // 3. Avance de papel y corte automático
            ms.Write(CmdAvanceLineas);
            ms.Write(CmdCortePapelTotal);

            // 4. Si hubo pago en efectivo, abrir gaveta monedero
            if (venta.EfectivoRecibido > 0)
            {
                ms.Write(CmdPulsoGaveta);
            }

            return ms.ToArray();
        }

        public async Task ImprimirTicketVentaAsync(Venta venta, CancellationToken ct = default)
        {
            Directory.CreateDirectory(CarpetaSpool);
            var texto = await FormatearTirillaTextoAsync(venta, ct);
            var bytesEscPos = await GenerarComandosEscPosAsync(venta, ct);

            var baseNombre = $"Ticket_POS{venta.Id}_{DateTime.Now:yyyyMMdd_HHmmss}";

            await File.WriteAllTextAsync(
                Path.Combine(CarpetaSpool, $"{baseNombre}.txt"),
                texto,
                Encoding.UTF8,
                ct);

            await File.WriteAllBytesAsync(
                Path.Combine(CarpetaSpool, $"{baseNombre}.escpos"),
                bytesEscPos,
                ct);
        }

        public async Task AbrirGavetaAsync(CancellationToken ct = default)
        {
            Directory.CreateDirectory(CarpetaSpool);
            var rutaPulso = Path.Combine(CarpetaSpool, "ultimo_pulso_gaveta.escpos");
            await File.WriteAllBytesAsync(rutaPulso, CmdPulsoGaveta, ct);
        }
    }

    /// <summary>
    /// Generador de tirilla de venta POS de 80mm (48 columnas).
    /// </summary>
    public static class PlantillaTicketPos
    {
        public const int AnchoColumnas = 48;
        private static readonly CultureInfo CulturaCo = new("es-CO");

        public static string ObtenerCodigoTarifa(int puntosBasicosIva) => puntosBasicosIva switch
        {
            0 => "C",
            500 => "M",
            1900 => "J",
            _ => "G"
        };

        public static async Task<string> GenerarTextoTirillaAsync(
            Venta venta,
            ServicioParametros parametros)
        {
            var nombreTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioNombre, "SUPERMERCADO SIGS S.A.S.");
            var nitTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioNit, "900.480.569-1");
            var direccionTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioDireccion, "CL 18 BIS 1 #36-04");
            var telefonoTienda = await ObtenerSeguroAsync(parametros, ClavesParametro.NegocioTelefono, "3009124523");

            var numTicket = $"POS-{venta.Id}";
            var fechaGen = venta.Fecha.ToLocalTime();

            var sb = new StringBuilder();

            // 1. Cabecera comercial
            sb.AppendLine(Centrar($"{nombreTienda.ToUpperInvariant()}  NIT {nitTienda}"));
            sb.AppendLine(Centrar($"DIRECCION: {direccionTienda}  TEL {telefonoTienda}"));
            sb.AppendLine(Separador('-'));

            sb.AppendLine(AlinearExtremos("TICKET DE VENTA POS N:", numTicket));
            sb.AppendLine($"FECHA: {fechaGen:yyyy-MM-dd HH:mm:ss}");

            var clienteNombre = !string.IsNullOrWhiteSpace(venta.NombreAdquirente)
                ? venta.NombreAdquirente
                : Venta.NombreConsumidorFinal;
            var clienteDoc = !string.IsNullOrWhiteSpace(venta.DocumentoAdquirente)
                ? venta.DocumentoAdquirente
                : Venta.DocumentoConsumidorFinal;

            sb.AppendLine($"CLIENTE: {Truncar(clienteNombre, 22),-22} ID: {clienteDoc}");
            sb.AppendLine($"DIRECCION: {Truncar(direccionTienda, 20),-20} FORMA PAGO: CONTADO");
            sb.AppendLine($"TOTAL ART: {venta.Detalles.Count}");
            sb.AppendLine(Separador('-'));

            // 2. Cabecera de columnas de detalle
            sb.AppendLine("#  CODIGO        DESCRIP          ID CANT   UM  VALOR/U  VALOR/T");
            sb.AppendLine(Separador('-'));

            var idx = 1;
            foreach (var d in venta.Detalles)
            {
                var codigo = Truncar(d.Producto?.CodigoBarras ?? d.ProductoId.ToString(), 13).PadRight(13);
                var descrip = Truncar(d.Producto?.Nombre ?? $"PROD {d.ProductoId}", 16).PadRight(16);
                var idTarifa = ObtenerCodigoTarifa(d.PuntosBasicosIva);

                string cantUm;
                if (d.Producto is ProductoPesado)
                {
                    var kg = d.Cantidad / 1000.0m;
                    cantUm = $"{kg.ToString("0.000", CulturaCo),6} KG";
                }
                else
                {
                    cantUm = $"{d.Cantidad,6} UN";
                }

                var valUnit = FormatearMonto(d.PrecioUnitario).PadLeft(8);
                var valTot = FormatearMonto(d.Subtotal).PadLeft(8);

                sb.AppendLine($"{idx,2} {codigo} {descrip} {idTarifa} {cantUm} {valUnit} {valTot}");
                idx++;
            }

            sb.AppendLine(Separador('-'));

            // 3. Totales y medios de pago
            sb.AppendLine(AlinearExtremos("TOTALES: *RECARGOS: 0  DESCUENTOS: -0", $"TOTAL: {FormatearMonto(venta.Total)}"));
            foreach (var pago in venta.Pagos)
            {
                sb.AppendLine(AlinearExtremos($"MEDIO/PAGO: {pago.Metodo}:", FormatearMonto(pago.Monto)));
            }

            if (venta.Cambio > 0)
            {
                sb.AppendLine(AlinearExtremos("CAMBIO / DEVUELTA:", FormatearMonto(venta.Cambio)));
            }

            sb.AppendLine(AlinearExtremos("Total:", FormatearMonto(venta.Total)));
            sb.AppendLine(Separador('-'));

            // 4. Discriminación de impuestos por tarifa (J: 19%, M: 5%, C: 0%)
            sb.AppendLine("DESCRIPCION            BASE/CANTIDAD     IVA/IMP CONSUMO");

            AgregarFilaImpuesto(sb, venta, 1900, "J:IVA 19%");
            AgregarFilaImpuesto(sb, venta, 500, "M:IVA 5%");
            AgregarFilaImpuesto(sb, venta, 0, "C:IVA 0%");

            sb.AppendLine(AlinearExtremos("TOTAL IMPUESTOS:", FormatearMonto(venta.TotalIva)));
            sb.AppendLine(Separador('='));
            sb.AppendLine(Centrar("¡GRACIAS POR SU COMPRA!"));

            return sb.ToString();
        }

        private static void AgregarFilaImpuesto(StringBuilder sb, Venta venta, int puntosBasicos, string etiqueta)
        {
            var detallesTarifa = venta.Detalles.Where(d => d.PuntosBasicosIva == puntosBasicos).ToList();
            var subtotalTarifa = detallesTarifa.Sum(d => d.Subtotal);
            var ivaTarifa = detallesTarifa.Sum(d => d.Iva);
            var baseGravable = Math.Max(0, subtotalTarifa - ivaTarifa);

            sb.AppendLine(
                $"{etiqueta,-20} {FormatearMonto(baseGravable),15} {FormatearMonto(ivaTarifa),19}");
        }

        private static string FormatearMonto(long pesos)
            => pesos.ToString("N0", CulturaCo);

        private static string Centrar(string texto)
        {
            if (string.IsNullOrEmpty(texto) || texto.Length >= AnchoColumnas)
                return texto;
            var pad = (AnchoColumnas - texto.Length) / 2;
            return texto.PadLeft(pad + texto.Length);
        }

        private static string AlinearExtremos(string izquierda, string derecha)
        {
            var espacio = AnchoColumnas - izquierda.Length - derecha.Length;
            return espacio > 0
                ? izquierda + new string(' ', espacio) + derecha
                : $"{izquierda} {derecha}";
        }

        private static string Truncar(string texto, int max)
            => texto.Length <= max ? texto : texto[..max];

        private static string Separador(char caracter)
            => new(caracter, AnchoColumnas);

        private static async Task<string> ObtenerSeguroAsync(
            ServicioParametros parametros,
            string clave,
            string porDefecto)
        {
            try
            {
                var valor = await parametros.ObtenerTextoAsync(clave);
                return string.IsNullOrWhiteSpace(valor) ? porDefecto : valor;
            }
            catch
            {
                return porDefecto;
            }
        }
    }
}
