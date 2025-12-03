using PositronicVariables.Attributes;
using PositronicVariables.Variables;

internal static class Program
{
    // Entry point into the timeline. Begins *after* knowing the result.
    // Notice there is no explicit for loop here.
    [DontPanic]
    internal static void Main()
    {
        var antival = AntiVal.GetOrCreate<Double>();

        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 2;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}
