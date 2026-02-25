using PositronicVariables.Engine;
using PositronicVariables.Engine.Coordinator;
using PositronicVariables.Runtime;
using System;

namespace ToGo.Compiler.Runtime;

public static class TimeRunner
{
    public static void Run(Action convergenceCode)
    {
        ArgumentNullException.ThrowIfNull(convergenceCode);

        ToGoRuntime.EnsureInitialised();

        var services = PositronicAmbient.Services;
        var engine = (IImprobabilityEngine<int>?)services.GetService(typeof(IImprobabilityEngine<int>));
        if (engine is null)
        {
            throw new InvalidOperationException("Positronic runtime is missing IImprobabilityEngine<int>. Ensure runtime initialisation uses AddPositronicRuntime<int>().");
        }

        // If the DI pipeline provided a ConvergenceCoordinator, the engine will enqueue and return.
        // We then flush to ensure convergence has completed.
        engine.Run(convergenceCode);

        var coord = (ConvergenceCoordinator?)services.GetService(typeof(ConvergenceCoordinator));
        coord?.FlushAsync().GetAwaiter().GetResult();
    }
}
