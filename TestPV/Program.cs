
internal static class Program
{

    [PositronicEntry]
    private static void Main()
    {
        var test = PositronicVariable<double>.GetOrCreate();

        Console.WriteLine($"The test is {test}");

        test.Assign(2);
    }
}
