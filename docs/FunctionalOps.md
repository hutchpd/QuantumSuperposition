## Functional & LINQ Operations

### Conditional Transformation without Collapse

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

### Chaining LINQ Operations

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
