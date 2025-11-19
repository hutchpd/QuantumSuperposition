## Usage Examples

## Required Namespaces

These examples demonstrate basic and advanced usage of the `QuBit<T>` class and related quantum simulation features. Make sure to import the correct namespaces.

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
```

### Basic Usage

```csharp
using QuantumSuperposition;

var qubit = new QuBit<int>(new[] { 1, 2, 3 });
Console.WriteLine(qubit.SampleWeighted());
```

### Arithmetic Operations

```csharp
using QuantumSuperposition;

var qubit1 = new QuBit<int>(new[] { 1, 2 });
var qubit2 = new QuBit<int>(new[] { 3, 4 });

var result = qubit1 + qubit2;
Console.WriteLine(result.SampleWeighted());
```

### Entanglement

```csharp
using QuantumSuperposition;

var qubit1 = new QuBit<int>(new[] { 1, 2 });
var qubit2 = new QuBit<int>(new[] { 3, 4 });

qubit1.EntangleWith(qubit2);
Console.WriteLine(qubit1.SampleWeighted());
Console.WriteLine(qubit2.SampleWeighted());
```

### Functional Operations

```csharp
using QuantumSuperposition;

var qubit = new QuBit<int>(new[] { 1, 2, 3 });
var result = qubit.Select(x => x * 2);
Console.WriteLine(result.SampleWeighted());
```

### Working with Complex Numbers

```csharp
using QuantumSuperposition;
using System.Numerics;

var qubit = new QuBit<Complex>(new[] { new Complex(1, 1), new Complex(2, 2) });
var result = qubit.Select(x => x * new Complex(2, 0));
Console.WriteLine(result.SampleWeighted());
```
### Prime Detection

```csharp
// For each number from 1 to 100 we test primality by creating
// a superposition of all smaller potential divisors (2...n-1).
// Then divide i by all at once and check if any remainder is zero.
for (int i = 1; i <= 100; i++)
{
    var divisors = new QuBit<int>(Enumerable.Range(2, i > 2 ? i - 2 : 1), intOps);
    if ((i % divisors).EvaluateAll())
        Console.WriteLine($"{i} is prime!");
}
```

### Factors of 10

```csharp
// Extract factors of 10 by projecting 10 % x across 1..10 and filtering for 0 results
var factors = Factors(10);
Console.WriteLine("Factors: " + factors.ToString());

public static Eigenstates<int> Factors(int v)
{
    var candidates = new Eigenstates<int>(Enumerable.Range(1, v), x => v % x, intOps);
    return candidates == 0;
}
```

### Min Value

```csharp
Console.WriteLine("min value of 3, 5, 8 is " + MinValue(new[] { 3, 5, 8 }));

public static int MinValue(IEnumerable<int> range)
{
    var values = new Eigenstates<int>(range, intOps);
    var filtered = values.Any() <= values.All();
    return filtered.ToValues().First();
}
