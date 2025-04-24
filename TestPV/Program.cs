
internal static class Program
{

    [PositronicEntry]
    private static void Main()
    {
        var antival = PositronicVariable<double>.GetOrCreate();

        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 3;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);

    }
}
