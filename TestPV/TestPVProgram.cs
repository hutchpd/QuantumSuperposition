using QuantumSuperposition;

internal static class TestPVProgram
{

    [PositronicEntry]
    internal static void Main()
    {
        var antival = AntiVal.GetOrCreate<Double>();

        Console.WriteLine($"The antival is {antival}");
        var val = (antival + 1) % 3;
        Console.WriteLine($"The value is {val}");
        antival.Assign(val);
    }
}


//internal static class SelfSolvingPuzzle
//{
//    [PositronicEntry]
//    internal static void Main()
//    {
//        // Create a positronic variable that doesn't know its value yet.
//        var mysteryNumber = AntiVal.GetOrCreate<int>();

//        // We declare that the result of doubling this mystery number must equal 10.
//        var doubled = mysteryNumber * 2;

//        Console.WriteLine($"The mystery number is: {mysteryNumber}");
//        Console.WriteLine($"When doubled, it becomes: {doubled}");

//        // Here's the punchline: assign the known output
//        doubled.Assign(10);
//    }
//}