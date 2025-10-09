using QuantumSuperposition;
//This program introduces the idea of positronic variables — variables that propagate their assigned values backwards through time.
// To the programmer, they behave like ordinary variables, but their final assignments retroactively determine their earlier reads, ensuring temporal consistency.
// In this example, an AntiVal is read before it is assigned. Yet when the program completes, the assignment travels back through execution, shaping its own past. The result is a self-consistent — or, when impossible, superposed - outcome that “just works,” even when cause and effect seem reversed.
// the output is expected to be:
// The antival is any(0, 1, 2)
// The value is any(1, 2, 0)
// (ordering does not matter)
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