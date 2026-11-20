using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supermercado.GUI;
using Supermercado.Logica.Servicios;

namespace Supermercado.Pruebas;

public sealed class ComposicionTelegramTests
{
    [Fact]
    public void ConfiguracionServicios_ResuelveWorkerYClientesSinSecreto()
    {
        var servicios = new ServiceCollection();
        App.ConfigurarServicios(servicios);
        using var proveedor = servicios.BuildServiceProvider();

        var workers = proveedor.GetServices<IHostedService>().ToArray();
        var worker = Assert.Single(workers);

        Assert.IsType<TelegramWorker>(worker);
        Assert.IsAssignableFrom<IClienteTelegram>(
            proveedor.GetRequiredService<IClienteTelegram>());
        Assert.IsAssignableFrom<Asistente>(
            proveedor.GetRequiredService<Asistente>());
    }
}
