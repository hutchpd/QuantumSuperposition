using System;
using System.Collections.Generic;
using System.Linq;

internal static class Program
{
    private static readonly IQuantumOperators<int> intOps = new IntOperators();

    public static void Main()
    {
        // Prime testing: using QuBit arithmetic.
        for (int i = 1; i <= 100; i++)
        {
            var divisors = new QuBit<int>(Enumerable.Range(2, i > 2 ? i - 2 : 1), intOps);
            if ((i % divisors).EvaluateAll())
                Console.WriteLine($"{i} is prime!");
        }

        // Factors: using Eigenstates with projection.
        // Here we project each candidate divisor 'x' to the computed value (v % x).
        // Then filtering by "== 0" will select keys where the remainder is 0.
        var factors = Factors(10);
        Console.WriteLine("Factors: " + factors.ToString());

        // MinValue: combining filtering operators.
        Console.WriteLine("min value of 3, 5, 8 is " + MinValue(new[] { 3, 5, 8 }));

        Console.WriteLine("Press any key to close...");
        Console.ReadKey();
    }

    public static Eigenstates<int> Factors(int v)
    {
        // Use the new projection constructor.
        var candidates = new Eigenstates<int>(Enumerable.Range(1, v), x => v % x, intOps);
        // Filter for keys whose projected value equals 0.
        return candidates == 0;
    }

    public static int MinValue(IEnumerable<int> range)
    {
        var values = new Eigenstates<int>(range, intOps);
        // Combine filtering operators.
        var filtered = values.Any() <= values.All();
        return filtered.ToValues().First();
    }
}
