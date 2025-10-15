using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class TestPVProgram
{

    [DontPanic]
    internal static void Main()
    {
        var antival = AntiVal.GetOrCreate<Double>();

        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 3;
        Console.WriteLine($"The value is {val}");
        antival.State = val;
    }
}