using QuantumSuperposition.Core;
using QuantumSuperposition.Operators;
using QuantumSuperposition.QuantumSoup;

internal static class Program
{
    // Operator set for basic quantum arithmetic on integers.
    // Think of it as your dimensional toolbox for integer superpositions.
    private static readonly IQuantumOperators<int> intOps = new IntOperators();

    public static void Main()
    {
        // === PRIME DETECTION VIA QUANTUM RESIDUAL ECHOES ===
        // For each number from 1 to 100, we test primality by creating
        // a quantum superposition of all smaller potential divisors (2...n-1).
        //
        // Then, we simultaneously divide the number 'i' by *all* of these at once,
        // and evaluate whether *any* of the divisions produced a clean zero remainder.
        //
        // If *none* of the remainders were zero, we’ve just confirmed the number is prime.
        for (int i = 1; i <= 100; i++)
        {
            // Build a quantum state of all potential divisors.
            var divisors = new QuBit<int>(Enumerable.Range(2, i > 2 ? i - 2 : 1), intOps);

            // Collapse the quantum result of (i % all divisors), and check for zeros.
            // If all outcomes were non-zero, then 'i' has no divisors and is prime.
            if ((i % divisors).EvaluateAll())
                Console.WriteLine($"{i} is prime!");
        }

        // We extract the factors of 10 by projecting the modulo result (10 % x)
        // across all integers 1 to 10, and filtering the ones where result == 0.
        //
        // This is equivalent to asking the multiverse: "Which of you evenly divide 10?"
        var factors = Factors(10);
        Console.WriteLine("Factors: " + factors.ToString());

        // Using quantum comparison operators to find the minimum of multiple values
        // without sorting or iteration. It’s a logic circuit built from reality checks.
        Console.WriteLine("min value of 3, 5, 8 is " + MinValue(new[] { 3, 5, 8 }));

        Console.WriteLine("Press any key to close...");
        Console.ReadKey();
    }

    /// <summary>
    /// Projects the modulo (v % x) over a range of integers 1 to v.
    /// Returns all values x for which the remainder is 0 — i.e., the true factors of v.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Eigenstates<int> Factors(int v)
    {
        // Use the new projection constructor.
        var candidates = new Eigenstates<int>(Enumerable.Range(1, v), x => v % x, intOps);
        // Filter for keys whose projected value equals 0.
        return candidates == 0;
    }

    /// <summary>
    /// Finds the minimum value in a set using quantum filtering and composite logic.
    /// Avoids traditional min/max functions by constructing conditional relationships.
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public static int MinValue(IEnumerable<int> range)
    {
        var values = new Eigenstates<int>(range, intOps);
        // Combine filtering operators.
        var filtered = values.Any() <= values.All();
        return filtered.ToValues().First();
    }
}
