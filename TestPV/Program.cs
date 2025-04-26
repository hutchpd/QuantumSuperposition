
internal static class Program
{


    private static void Main()
    {
        PositronicStartup.Initialise();
        MainLogic();
    }

    [PositronicEntry]
    private static void MainLogic()
    {
        var antival = AntiVal.GetOrCreate<Double>();

        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 3;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}