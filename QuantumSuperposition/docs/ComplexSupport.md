## Working with Complex Numbers

## Required Namespaces

Make sure to import the correct namespaces for these examples.

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
using QuantumSuperposition.Utilities;
```

QuantumSuperposition supports complex numbers for advanced quantum operations. This section guides you through using complex numbers in the library.

### Basic Complex Number Operations

You can perform basic arithmetic on complex numbers within a superposition. Here is an example:

```csharp
var complex1 = new QuBit<Complex>(new Complex(1, 2));
var complex2 = new QuBit<Complex>(new Complex(3, 4));

var sum = complex1 + complex2;
var difference = complex1 - complex2;
var product = complex1 * complex2;
var quotient = complex1 / complex2;

Console.WriteLine($"Sum: {sum}");
Console.WriteLine($"Difference: {difference}");
Console.WriteLine($"Product: {product}");
Console.WriteLine($"Quotient: {quotient}");
```

### Complex Number Superpositions

Create superpositions of complex numbers and operate on them:

```csharp
var complexSuperposition1 = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4) });
var complexSuperposition2 = new QuBit<Complex>(new[] { new Complex(5, 6), new Complex(7, 8) });

var superpositionSum = complexSuperposition1 + complexSuperposition2;
var superpositionProduct = complexSuperposition1 * complexSuperposition2;

Console.WriteLine($"Superposition Sum: {superpositionSum}");
Console.WriteLine($"Superposition Product: {superpositionProduct}");
```

### Weighted Complex Superpositions

Create weighted superpositions of complex numbers:

```csharp
var weightedComplexSuperposition = new QuBit<Complex>(
    new Dictionary<Complex, Complex>
    {
        { new Complex(1, 2), new Complex(0.6, 0.8) },
        { new Complex(3, 4), new Complex(0.3, 0.4) },
        { new Complex(5, 6), new Complex(0.1, 0.2) }
    });

var weightedSum = weightedComplexSuperposition + new Complex(1, 1);

Console.WriteLine($"Weighted Sum: {weightedSum}");
```

### Observing Complex Superpositions

Collapse a complex superposition to get a single value:

```csharp
var complexSuperposition = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4), new Complex(5, 6) });

var observedValue = complexSuperposition.Observe();

Console.WriteLine($"Observed Value: {observedValue}");
```

### Complex Number Functions

Apply complex number functions to superpositions:

```csharp
var complexSuperposition = new QuBit<Complex>(new[] { new Complex(1, 2), new Complex(3, 4) });

var magnitudeSuperposition = complexSuperposition.Select(c => c.Magnitude);
var phaseSuperposition = complexSuperposition.Select(c => c.Phase);

Console.WriteLine($"Magnitude Superposition: {magnitudeSuperposition}");
Console.WriteLine($"Phase Superposition: {phaseSuperposition}");

```

### Interference example: Hadamard ? Phase(?) ? Hadamard

This short example demonstrates destructive and constructive interference implemented by complex phases and matrix multiplication.

```csharp
// Example: Hadamard -> Phase(pi) -> Hadamard gives deterministic |1> outcome
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.Utilities;

Complex[] initial = new Complex[] { Complex.One, Complex.Zero }; // |0>

// Hadamard
Complex[,] H = QuantumGates.Hadamard.Matrix;

// After H: (|0> + |1>) / sqrt(2)
Complex[] afterH = QuantumMathUtility<Complex>.ApplyMatrix(initial, H);

// Apply a phase of pi to |1> (i.e. [1, -1] diagonal)
Complex[,] phasePi = QuantumGates.Phase(Math.PI).Matrix;
Complex[] afterPhase = QuantumMathUtility<Complex>.ApplyMatrix(afterH, phasePi);

// Apply Hadamard again
Complex[] final = QuantumMathUtility<Complex>.ApplyMatrix(afterPhase, H);

// Inspect probabilities
double p0 = final[0].Magnitude * final[0].Magnitude;
double p1 = final[1].Magnitude * final[1].Magnitude;

// Expect p0 ? 0, p1 ? 1 due to destructive interference on |0>.
Console.WriteLine($"p0={p0:E3}, p1={p1:E3}");
```

Explanation: the phase `-1` on the `|1>` component flips the sign so the two amplitude paths interfere destructively at `|0>` and constructively at `|1>`.

(Helpers used here: `QuantumGates` supplies `Hadamard` and `Phase`, and `QuantumMathUtility.ApplyMatrix` performs the complex matrix×vector multiplication.)
