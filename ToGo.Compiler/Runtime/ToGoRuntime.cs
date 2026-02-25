using Microsoft.Extensions.Hosting;
using PositronicVariables.DependencyInjection;
using PositronicVariables.Runtime;
using PositronicVariables.Variables;
using System;

namespace ToGo.Compiler.Runtime;

public static class ToGoRuntime
{
    public static void EnsureInitialised()
    {
        if (PositronicAmbient.IsInitialized)
        {
            return;
        }

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddPositronicRuntime<int>(b => { }));

        PositronicAmbient.InitialiseWith(hostBuilder);
    }

    public static PositronicVariable<int> CreateAntival(string name, int initialValue = 0)
    {
        EnsureInitialised();
        return PositronicVariable<int>.GetOrCreate(name, initialValue, PositronicAmbient.Current);
    }

    public static void Print(PositronicVariable<int> v)
    {
        ArgumentNullException.ThrowIfNull(v);
        Console.WriteLine(v.GetCurrentQBit().ToString());
    }

    public static void Print(int value)
    {
        Console.WriteLine(value);
    }

    public static void Print(QuantumSuperposition.QuantumSoup.QuBit<int> qb)
    {
        ArgumentNullException.ThrowIfNull(qb);
        Console.WriteLine(qb.ToString());
    }
}
