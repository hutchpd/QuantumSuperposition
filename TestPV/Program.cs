using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

using System;

internal static class Program
{
    private static PositronicVariable<int> antival;

    private static void Main()
    {
        // Reset the runtime state to ensure a clean start. 
        PositronicRuntime.Instance.Reset();

        // Initialize the positronic variable.
        antival = new PositronicVariable<int>(-1);

        // Run the convergence loop, which will repeatedly call MainLogic until convergence.
        PositronicVariable<int>.RunConvergenceLoop(MainLogic);

    }

    private static void MainLogic()
    {
        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 3;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}
