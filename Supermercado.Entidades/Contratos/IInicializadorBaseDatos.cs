namespace Supermercado.Entidades.Contratos;

public interface IInicializadorBaseDatos
{
    Task CrearYSembrarAsync();
    Task CargarScriptPostgresAsync(string? sqlPersonalizado = null);
    bool EsMotorPostgres { get; }
}
