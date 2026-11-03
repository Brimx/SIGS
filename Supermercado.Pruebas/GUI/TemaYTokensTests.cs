using System.Globalization;
using System.Xml.Linq;
using Supermercado.GUI.Servicios.Legal;
using Supermercado.GUI.Servicios.Preferencias;
using Supermercado.GUI.Servicios.Tema;
using Supermercado.GUI.ViewModels;

namespace Supermercado.Pruebas.GUI;

/// <summary>
/// Verifica los tokens leyendo los .axaml como XML: no requiere inicializar Avalonia.
/// </summary>
public class TemaYTokensTests
{
    private const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly string Raiz = BuscarRaiz();

    // ControlThemes de Semi, fuera de nuestros diccionarios.
    private static readonly HashSet<string> ClavesDeSemi = ["SolidButton", "OutlineButton", "BorderlessButton"];

    private static string BuscarRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Supermercado.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
    }

    private static XDocument Cargar(string rutaRelativa) =>
        XDocument.Load(Path.Combine(Raiz, rutaRelativa));

    private static string? Clave(XElement e) => e.Attribute(XName.Get("Key", XamlNs))?.Value;

    private static Dictionary<string, string> ColoresPrimitivos()
    {
        var doc = Cargar("Supermercado.GUI/Resources/Primitives.axaml");
        return doc.Descendants()
            .Where(e => e.Name.LocalName == "Color" && Clave(e) is not null)
            .ToDictionary(e => Clave(e)!, e => e.Value.Trim());
    }

    private static Dictionary<string, string> Brushes(string tema)
    {
        // Clave de la app → clave del diccionario; alto contraste se declara con {x:Static}.
        var esperada = tema switch
        {
            TemaSigs.ClaveOscuro => "Dark",
            TemaSigs.ClaveAltoContraste => TemaSigs.ClaveAltoContraste,
            _ => "Light",
        };

        var doc = Cargar("Supermercado.GUI/Resources/Semantic.axaml");
        var diccionario = doc.Descendants()
            .First(e => e.Name.LocalName == "ResourceDictionary" && Clave(e)?.Contains(esperada) == true);

        var resultado = new Dictionary<string, string>();
        foreach (var brush in diccionario.Descendants().Where(e => e.Name.LocalName == "SolidColorBrush"))
        {
            if (Clave(brush) is not { } clave)
                continue;

            var color = brush.Attribute("Color")?.Value ?? string.Empty;
            if (color.StartsWith("{StaticResource ", StringComparison.Ordinal))
                color = color["{StaticResource ".Length..].TrimEnd('}');

            resultado[clave] = color;
        }

        return resultado;
    }

    private static string Hex(string clave, Dictionary<string, string> brushes, Dictionary<string, string> primitivos) =>
        brushes[clave].StartsWith('#') ? brushes[clave] : primitivos[brushes[clave]];

    private static double Luminancia(string hex)
    {
        var c = hex.TrimStart('#');
        if (c.Length == 8)
            c = c[2..];

        var r = byte.Parse(c[..2], NumberStyles.HexNumber) / 255.0;
        var g = byte.Parse(c[2..4], NumberStyles.HexNumber) / 255.0;
        var b = byte.Parse(c[4..6], NumberStyles.HexNumber) / 255.0;

        static double Lineal(double v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

        return 0.2126 * Lineal(r) + 0.7152 * Lineal(g) + 0.0722 * Lineal(b);
    }

    private static double Contraste(string a, string b)
    {
        var la = Luminancia(a);
        var lb = Luminancia(b);
        var (mayor, menor) = la > lb ? (la, lb) : (lb, la);
        return (mayor + 0.05) / (menor + 0.05);
    }

    [Fact]
    public void Toda_clave_referenciada_existe()
    {
        var definidas = new HashSet<string>(ClavesDeSemi);
        foreach (var archivo in Directory.GetFiles(Path.Combine(Raiz, "Supermercado.GUI/Resources"), "*.axaml"))
        {
            foreach (var e in XDocument.Load(archivo).Descendants())
            {
                if (Clave(e) is { } clave)
                    definidas.Add(clave);
            }
        }

        var referenciadas = new HashSet<string>();
        var vistas = Directory.GetFiles(Path.Combine(Raiz, "Supermercado.GUI/Views"), "*.axaml")
            .Append(Path.Combine(Raiz, "Supermercado.GUI/Resources/Controles.axaml"));

        foreach (var archivo in vistas)
        {
            var texto = File.ReadAllText(archivo);
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(texto, @"\{(?:Dynamic|Static)Resource ([A-Za-z0-9_]+)\}"))
            {
                referenciadas.Add(m.Groups[1].Value);
            }
        }

        var faltantes = referenciadas.Except(definidas).OrderBy(x => x).ToList();
        Assert.True(faltantes.Count == 0, $"Claves sin definir: {string.Join(", ", faltantes)}");
    }

    [Theory]
    [InlineData(TemaSigs.ClaveClaro)]
    [InlineData(TemaSigs.ClaveOscuro)]
    [InlineData(TemaSigs.ClaveAltoContraste)]
    public void Texto_sobre_relleno_cumple_AA(string tema)
    {
        var brushes = Brushes(tema);
        var primitivos = ColoresPrimitivos();

        foreach (var relleno in new[] { "BrushPrimary", "BrushSuccess", "BrushWarning", "BrushError" })
        {
            var ratio = Contraste(Hex("BrushTextOnPrimary", brushes, primitivos), Hex(relleno, brushes, primitivos));
            Assert.True(ratio >= 4.5, $"{tema}: BrushTextOnPrimary sobre {relleno} = {ratio:F2}");
        }
    }

    [Theory]
    [InlineData(TemaSigs.ClaveClaro)]
    [InlineData(TemaSigs.ClaveOscuro)]
    [InlineData(TemaSigs.ClaveAltoContraste)]
    public void Texto_sobre_superficie_cumple_AA(string tema)
    {
        var brushes = Brushes(tema);
        var primitivos = ColoresPrimitivos();

        foreach (var texto in new[] { "BrushTextPrimary", "BrushTextSecondary", "BrushTextMuted" })
        {
            foreach (var fondo in new[] { "BrushBackground", "BrushSurface" })
            {
                var ratio = Contraste(Hex(texto, brushes, primitivos), Hex(fondo, brushes, primitivos));
                Assert.True(ratio >= 4.5, $"{tema}: {texto} sobre {fondo} = {ratio:F2}");
            }
        }
    }

    [Fact]
    public void Ciclo_de_temas_es_completo()
    {
        Assert.Equal(TemaSigs.ClaveOscuro, TemaSigs.Siguiente(TemaSigs.ClaveClaro));
        Assert.Equal(TemaSigs.ClaveAltoContraste, TemaSigs.Siguiente(TemaSigs.ClaveOscuro));
        Assert.Equal(TemaSigs.ClaveClaro, TemaSigs.Siguiente(TemaSigs.ClaveAltoContraste));
        Assert.Equal(TemaSigs.ClaveClaro, TemaSigs.Siguiente("desconocido"));
    }

    [Fact]
    public void Preferencias_roundtrip_y_fallback()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "sigs-prefs-" + Guid.NewGuid().ToString("N"));
        var ruta = Path.Combine(carpeta, "preferencias.json");

        try
        {
            var servicio = new ServicioPreferencias(ruta);
            Assert.Equal(TemaSigs.ClaveClaro, servicio.LeerTema());

            servicio.GuardarTema(TemaSigs.ClaveOscuro);
            Assert.Equal(TemaSigs.ClaveOscuro, servicio.LeerTema());

            File.WriteAllText(ruta, "{ no es json");
            Assert.Equal(TemaSigs.ClaveClaro, servicio.LeerTema());

            File.WriteAllText(ruta, """{"tema":"Inexistente"}""");
            Assert.Equal(TemaSigs.ClaveClaro, servicio.LeerTema());
        }
        finally
        {
            if (Directory.Exists(carpeta))
                Directory.Delete(carpeta, recursive: true);
        }
    }

    [Fact]
    public void Textos_legales_contienen_requisitos_minimos()
    {
        var privacidad = File.ReadAllText(Path.Combine(Raiz, "Supermercado.GUI/Assets/Legal/politica-privacidad.txt"));
        var terminos = File.ReadAllText(Path.Combine(Raiz, "Supermercado.GUI/Assets/Legal/terminos-servicio.txt"));

        Assert.Contains("Ley 1581", privacidad);
        Assert.Contains("no recopila", privacidad, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DIAN", privacidad);
        Assert.Contains("000165", terminos);
        Assert.Contains("DIAN", terminos);
    }

    [Fact]
    public void Configuracion_aplica_y_persiste_el_tema()
    {
        var preferencias = new PreferenciasFalsas();
        var vm = new ConfiguracionViewModel(preferencias, new LegalesFalsos());

        vm.SeleccionarTemaCommand.Execute(TemaSigs.ClaveAltoContraste);

        Assert.Equal(TemaSigs.ClaveAltoContraste, vm.TemaActual);
        Assert.True(vm.EsAltoContraste);
        Assert.Equal(TemaSigs.ClaveAltoContraste, preferencias.Tema);
        Assert.False(vm.EsClaro);
    }

    [Fact]
    public void MainViewModel_expone_configuracion_para_cualquier_rol()
    {
        var main = new MainViewModel();

        Assert.NotNull(main.ConfiguracionVm);
        Assert.False(main.MostrarConfiguracion);

        main.AbrirConfiguracionCommand.Execute(null);
        Assert.True(main.MostrarConfiguracion);

        main.CerrarConfiguracionCommand.Execute(null);
        Assert.False(main.MostrarConfiguracion);
    }

    private sealed class PreferenciasFalsas : IServicioPreferencias
    {
        public string? Tema { get; private set; }

        public string LeerTema() => Tema ?? TemaSigs.ClaveClaro;

        public void GuardarTema(string clave) => Tema = clave;
    }

    private sealed class LegalesFalsos : IServicioTextosLegales
    {
        public string PoliticaPrivacidad() => "Ley 1581 · no recopila · DIAN";

        public string TerminosServicio() => "Resolución 000165 · DIAN";
    }
}
