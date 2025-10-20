using PositronicVariables.Attributes;
using PositronicVariables.Variables;
using static PositronicVariables.Variables.QSugar;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var antival = PositronicVariable<int>.GetOrCreate("antival", 0);

        Console.WriteLine($"The antivals are {antival} ");
        antival = antival + 2;
        antival <<= Q(10);
    }
}