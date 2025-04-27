using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuantumSuperposition.DependencyInjection;

public static class PositronicStartup
{
    public static void Initialise()
    {
        if (PositronicAmbient.Current != null)
            return;

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddPositronicRuntime())
            .Build();

        PositronicAmbient.Current = host.Services.GetRequiredService<IPositronicRuntime>();
    }
}
