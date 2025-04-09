## Working with Complex Numbers

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
