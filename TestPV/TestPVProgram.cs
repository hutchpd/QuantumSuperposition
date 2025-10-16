using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
        Console.WriteLine($"The antival is {antival}");
        var val = -1 * antival;
        Console.WriteLine($"The value is {val}");
        antival.State = val; // feeds back
    }
}
