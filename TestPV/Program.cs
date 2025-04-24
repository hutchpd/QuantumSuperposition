
internal static class Program
{

    [PositronicEntry]
    private static void Main()
    {
        var temperature = PositronicVariable<double>.GetOrCreate();

        Console.WriteLine($"The temperature in c is {temperature}");

        temperature.Assign(temperature + 1);
        temperature.Assign(temperature + 1);
        temperature.Assign(10);

    }
}
