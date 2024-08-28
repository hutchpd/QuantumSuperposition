using System.Collections.Generic;
using System.Linq;

internal static partial class Program
{
    public static void Main()
    {
        for (int i = 1; i <= 100; i++)
        {
            if (IsPrime(i))
            {
                Console.WriteLine(i + " is prime!");
            }
        }

        for (int i = 10; i <= 10; i++)
            Console.WriteLine("Factors " + Factors(i).ToString());

        Console.WriteLine("min vlaue of 3, 5, 8 is " + MinValue(new[] { 3, 5, 8 }));

        // Wait for user input before closing the console
        Console.WriteLine("Press any key to close the window...");
        Console.ReadKey();
    }

    public static bool IsPrime(int i)
    {
        var divisors = new QuBit<int>(Enumerable.Range(2, i > 2 ? i - 2 : 1));
        return (i % divisors).All(); 
    }


    public static Eigenstates<int> Factors(int v)
    {

        var divisors = new Eigenstates<int>(Enumerable.Range(1, v));
        return v % divisors == 0;

    }

    public static int MinValue(IEnumerable<int> range)
    {

        var minval = new Eigenstates<int>(range);
        var test = minval.Any() <= minval.All();
        return test.ToValues().First();

    }
}