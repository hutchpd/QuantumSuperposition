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
