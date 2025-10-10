using PositronicVariables.Attributes;
using PositronicVariables.Variables;
using PositronicVariables.Runtime;

internal static class Program
{
    [DontPanic]
    private static void Main()
    {
        var x = PositronicVariable<int>.GetOrCreate("x", 0);
        Console.WriteLine($"x is {x}");
        x.State = 10;
    }
}

