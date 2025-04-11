    internal static class Program
    {
        private static void Main()
        {
            var antival = PositronicVariable<int>.GetOrCreate("antival", -1);
            Console.WriteLine($"The antival is {antival}");
            var val = -1 * antival;
            Console.WriteLine($"The value is {val}");
            antival.Assign(val);
        }
}


//internal static class Program
//{
//    private static void Main()
//    {
//        // Create (or retrieve) a positronic variable holding a string.
//        // Note: We use PositronicVariableRef<string> because string is a reference type.
//        var antival = PositronicVariableRef<string>.GetOrCreate("antival", "Alpha");

//        Console.WriteLine($"The antival is {antival}");

//        // Reverse the string (mimicking an "inversion" like -1 * antival for numbers).
//        var reversed = new string(antival.ToValues().First().Reverse().ToArray());
//        Console.WriteLine($"The reversed antival is {reversed}");

//        // Assign the reversed string back to the positronic variable.
//        antival.Assign(reversed);
//    }
//}
