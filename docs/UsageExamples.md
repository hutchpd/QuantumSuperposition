## Usage Examples

### Prime Number Checking  
Want to find primes without making your code look like a cryptography dissertation?
```csharp
static bool IsPrime(int number)
{
    var divisors = new QuBit<int>(Enumerable.Range(2, number - 2));
    return (number % divisors).EvaluateAll();
    // True if number is indivisible by any in divisors → therefore prime
}

for (int i = 1; i <= 100; i++)
{
    if (IsPrime(i))
        Console.WriteLine($"{i} is prime!");
}
```

### Finding Factors  
You can treat divisors as states and filter by computed results:
```csharp
static Eigenstates<int> Factors(int number)
{
    var candidates = new Eigenstates<int>(Enumerable.Range(1, number), x => number % x);
    return candidates == 0; // Give me the ones that divide cleanly
}
```

### Minimum Value Calculation  
Think of this like a quantum game show where only the smallest contestant survives:
```csharp
static int MinValue(IEnumerable<int> numbers)
{
    var eigen = new Eigenstates<int>(numbers);
    var result = eigen.Any() <= eigen.All(); // anyone less than or equal to everyone
    return result.ToValues().First();
}
```

### Complex Number Arithmetic with QuBit and Eigenstates

#### Performing Arithmetic Operations on Complex QuBit States

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using QuantumSuperposition;

public class ComplexArithmeticExample
{
    public static void Main()
    {
        var qubit = new QuBit<Complex>(
            new List<Complex> { new Complex(1, 2), new Complex(3, 4) },
            new ComplexOperators()
        );
        var scalar = new Complex(1, 1);
        var resultQuBit = qubit + scalar;

        Console.WriteLine("After addition:");
        foreach (var state in resultQuBit.States)
            Console.WriteLine(state);

        var qubitA = new QuBit<Complex>(
            new List<Complex> { new Complex(2, 0), new Complex(1, 1) },
            new ComplexOperators()
        );
        var qubitB = new QuBit<Complex>(
            new List<Complex> { new Complex(3, 0), new Complex(0, 2) },
            new ComplexOperators()
        );

        var multiplied = qubitA * qubitB;
        Console.WriteLine("After multiplication:");
        foreach (var state in multiplied.States)
            Console.WriteLine(state);
    }
}
```

#### Weighted Superpositions and Weight Normalization

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using QuantumSuperposition;

public class WeightedQuBitExample
{
    public static void Main()
    {
        var weightedItems = new[]
        {
            (new Complex(1, 0), (Complex)0.5),
            (new Complex(2, 0), (Complex)1.0)
        };

        var qubit = new QuBit<Complex>(weightedItems, new ComplexOperators());
        qubit.Append(new Complex(1, 0));
        qubit.NormaliseWeights();

        Console.WriteLine("Weighted and normalized values:");
        foreach (var (value, weight) in qubit.ToWeightedValues())
            Console.WriteLine($"State: {value}, Weight: {weight}");
    }
}
```

### Functional & LINQ-Style Operations

#### Conditional Transformation without Collapse

```csharp
using System;
using QuantumSuperposition;

public class ConditionalTransformationExample
{
    public static void Main()
    {
        var qubit = new QuBit<int>(new[] { 1, 2, 3 });

        var transformed = qubit.Conditional(
            (value, weight) => value % 2 == 0,
            qb => qb.Select(x => x * 10),
            qb => qb.Select(x => x + 5)
        );

        Console.WriteLine("Transformed states:");
        foreach (var state in transformed.States)
            Console.WriteLine(state);
    }
}
```

#### Chaining LINQ Operations

```csharp
using System;
using QuantumSuperposition;

public class LinqChainExample
{
    public static void Main()
    {
        var qubit = new QuBit<int>(new[] { 1, 2 });

        var result = qubit.Select(x => x + 1)
                          .Where(x => x > 2)
                          .SelectMany(x => new QuBit<int>(new[] { x * 3, x * 4 }));

        Console.WriteLine("Final states after chained LINQ operations:");
        foreach (var state in result.States)
            Console.WriteLine(state);
    }
}
```
