using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", 0);

        Console.WriteLine($"The antivals are {antival} "); // the result is printed
        antival.State = antival.State + 2;
        antival.Scalar = 10; // second variable incremented

    }
}