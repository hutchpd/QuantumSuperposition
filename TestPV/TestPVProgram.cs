using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", 0);

        Console.WriteLine($"The antivals are {antival} ");
        antival = antival + 2;
        antival.Assign(10); // was: antival |= 10; (now bitwise OR)
    }
}