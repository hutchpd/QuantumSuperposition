## Usage Examples

## Required Namespaces

These examples demonstrate basic and advanced usage of the `QuBit<T>` class and related quantum simulation features. Make sure to import the correct namespaces, for these examples we use.

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;


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

```
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
```
