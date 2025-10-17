using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;

public static class AntiVal
{
    /// <summary>
    /// This method hunts down an existing PositronicVariable of type T in the current PositronicAmbient. If it finds one, it returns it. If not, it conjures up a new one, ensuring that your quest for quantum variables is never in vain.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PositronicVariable<T> GetOrCreate<T>()
        where T : IComparable<T>
    {
        if (!PositronicAmbient.IsInitialized)
        {
            InitialiseDefaultRuntime();
        }

        PositronicVariable<T> v = PositronicVariable<T>.GetOrCreate(PositronicAmbient.Current);

        return v;
    }

    private static bool PositronicAmbientIsUninitialized()
    {
        return PositronicAmbient.Current == null;
    }

    /// <summary>
    /// If the runtime hasn't been initialized, we gently whisper it into existence like a haunted lullaby. 
    /// </summary>
    private static void InitialiseDefaultRuntime()
    {
        if (PositronicAmbient.IsInitialized)
        {
            return;
        }

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddPositronicRuntime());
        PositronicAmbient.InitialiseWith(hostBuilder);
    }
}
