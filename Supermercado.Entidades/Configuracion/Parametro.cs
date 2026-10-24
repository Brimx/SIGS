using Supermercado.Entidades.Base;

namespace Supermercado.Entidades.Configuracion;

public class Parametro : EntidadBase
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
