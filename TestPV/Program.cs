internal static class Program
{
    private static void Main()
    {
        using (new PositronicSimulation(() =>
        {
            var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
            Console.WriteLine($"The antival is {antival}");
            var val = -1 * antival;
            Console.WriteLine($"The value is {val}");
            antival.Assign(val);
        })){}
    }
}