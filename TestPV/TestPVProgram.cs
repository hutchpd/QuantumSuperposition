using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", 0);
        Console.WriteLine($"The antival is {antival}"); // the result is printed
        antival.State = antival.State + 1; // We add one
        antival.State = 10; // the start of the program
    }
}
