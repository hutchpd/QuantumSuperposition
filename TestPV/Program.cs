internal static class Program
{
    private static void Main()
    {
        // Reset the runtime state to ensure a clean start. 
        PositronicRuntime.Instance.Reset();

        // Run the convergence loop, which will repeatedly call MainLogic until convergence.
        PositronicVariable<int>.RunConvergenceLoop(MainLogic);

    }

    private static void MainLogic()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
        Console.WriteLine($"The antival is {antival}");
        var val = -1 * antival;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}
