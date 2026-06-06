# Working with Complex Numbers

Complex numbers are where `QuantumSuperposition` stops being merely a library about many possible values and starts behaving like a quantum simulator with paperwork.

For generic `QuBit<T>` usage, complex numbers can simply be values. You can place them in superpositions, transform them, observe them, and do arithmetic over them.

For the physics-style layer, complex numbers are not decorative. They are the machinery. Amplitudes are complex. Gates are complex matrices. Phase lives in the angle. Interference appears when amplitudes are multiplied, summed, cancelled, and reinforced.

A probability is what you get after the universe has squared the magnitude and filed the report.

An amplitude is what existed before it was forced to simplify itself for management.

---

## Required namespaces

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
using QuantumSuperposition.Utilities;
```

---

## Complex numbers as ordinary values

You can use `Complex` as the value type for a `QuBit<T>`.

In this mode, a complex number is just the thing being superposed. It is not necessarily acting as a quantum amplitude. It is the payload.

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

This is ordinary arithmetic applied through the superposition machinery.

The values happen to be complex.

The library does not faint.

---

## Complex number superpositions

You can create a superposition of several complex values and operate across all possible pairings.

```csharp
var complexSuperposition1 = new QuBit<Complex>(
    new[]
    {
        new Complex(1, 2),
        new Complex(3, 4)
    });

var complexSuperposition2 = new QuBit<Complex>(
    new[]
    {
        new Complex(5, 6),
        new Complex(7, 8)
    });

var superpositionSum = complexSuperposition1 + complexSuperposition2;
var superpositionProduct = complexSuperposition1 * complexSuperposition2;

Console.WriteLine($"Superposition Sum: {superpositionSum}");
Console.WriteLine($"Superposition Product: {superpositionProduct}");
```

Each possible value participates in the operation.

No observation is required.

The calculation proceeds as though the values had the dignity to be definite, while quietly preserving the fact that they are not.

---

## Weighted complex superpositions

Complex numbers may also appear as weights.

This is where the distinction matters:

* `QuBit<Complex>` means the possible values are complex numbers.
* `Dictionary<T, Complex>` means the possible values have complex amplitudes.

The first is about the value.

The second is about the state.

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

The dictionary key is the possible value.

The dictionary value is the amplitude attached to that possibility.

This is a small syntax detail with large metaphysical consequences.

---

## Observing complex superpositions

Observation collapses the superposition to one value.

```csharp
var complexSuperposition = new QuBit<Complex>(
    new[]
    {
        new Complex(1, 2),
        new Complex(3, 4),
        new Complex(5, 6)
    });

var observedValue = complexSuperposition.Observe();

Console.WriteLine($"Observed Value: {observedValue}");
```

After observation, the system has selected a single value from the possible state.

The other possibilities do not get a farewell speech.

---

## Transforming complex values

You can apply ordinary `Complex` properties and functions through LINQ-style transforms.

```csharp
var complexSuperposition = new QuBit<Complex>(
    new[]
    {
        new Complex(1, 2),
        new Complex(3, 4)
    });

var magnitudeSuperposition = complexSuperposition.Select(c => c.Magnitude);
var phaseSuperposition = complexSuperposition.Select(c => c.Phase);

Console.WriteLine($"Magnitude Superposition: {magnitudeSuperposition}");
Console.WriteLine($"Phase Superposition: {phaseSuperposition}");
```

This is useful when you want to move from complex values to derived real-valued properties while keeping the superposed structure intact.

Magnitude and phase are not observed results here.

They are transformed possibilities.

---

## Complex amplitudes in the physics layer

In the physics-style layer, a state vector is made of complex amplitudes.

A single-qubit state is usually written as:

```text
α|0⟩ + β|1⟩
```

`α` and `β` are amplitudes.

They are not probabilities.

To get the probability of observing a basis state, take the squared magnitude:

```text
probability = |amplitude|²
```

That distinction matters because amplitudes can carry phase.

Two amplitudes may have the same magnitude and therefore the same immediate observation probability, while still behaving differently after later gates.

The visible odds may match.

The future has not agreed to match.

---

## Phase and interference

Phase is the part of the amplitude that classical intuition tends to underestimate because it may not change the immediate measurement probability.

That does not make it harmless.

Relative phase changes how later operations combine amplitudes. A later gate can cause one path to cancel another, or reinforce it, because matrix multiplication sums complex amplitudes.

This is interference.

It is not metaphorical.

It is arithmetic with consequences.

---

## Interference example: Hadamard → Phase(π) → Hadamard

This example starts in `|0⟩`, applies a Hadamard, applies a phase of π to the `|1⟩` component, then applies another Hadamard.

The result is deterministic `|1⟩`.

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.Utilities;

Complex[] initial = new Complex[]
{
    Complex.One,
    Complex.Zero
}; // |0⟩

// Hadamard
Complex[,] h = QuantumGates.Hadamard.Matrix;

// After H: (|0⟩ + |1⟩) / sqrt(2)
Complex[] afterH = QuantumMathUtility<Complex>.ApplyMatrix(initial, h);

// Apply a phase of π to |1⟩: diagonal [1, -1]
Complex[,] phasePi = QuantumGates.Phase(Math.PI).Matrix;
Complex[] afterPhase = QuantumMathUtility<Complex>.ApplyMatrix(afterH, phasePi);

// Apply Hadamard again
Complex[] final = QuantumMathUtility<Complex>.ApplyMatrix(afterPhase, h);

// Inspect probabilities
double p0 = final[0].Magnitude * final[0].Magnitude;
double p1 = final[1].Magnitude * final[1].Magnitude;

Console.WriteLine($"p0={p0:E3}, p1={p1:E3}");
```

Expected result:

```text
p0 ≈ 0
p1 ≈ 1
```

The phase flip changes the sign of the `|1⟩` amplitude.

After the second Hadamard, the paths interfere destructively at `|0⟩` and constructively at `|1⟩`.

The probability did not move by being pushed.

It moved because the amplitudes were allowed to disagree in just the right way.

---

## Helpers used in the example

The example uses:

* `QuantumGates.Hadamard`
* `QuantumGates.Phase(Math.PI)`
* `QuantumMathUtility<Complex>.ApplyMatrix`

`QuantumGates` supplies the canonical matrices.

`QuantumMathUtility.ApplyMatrix` performs the matrix-vector multiplication over complex numbers.

That multiplication is where phase becomes interference.

That interference is where the simulator earns the word quantum.

---

## Common mistakes

### Treating amplitudes as probabilities

An amplitude is not a probability.

Use squared magnitude to derive probability.

```csharp
double probability = amplitude.Magnitude * amplitude.Magnitude;
```

The amplitude may be negative, imaginary, or have a phase that matters later. A probability is real, non-negative, and already has most of the interesting behaviour squeezed out of it.

### Ignoring relative phase

Global phase is not observable.

Relative phase is.

If every amplitude is multiplied by the same complex phase, the physical measurement probabilities do not change. If one branch receives a different phase from another, later gates may produce different interference.

Global phase is the universe changing its handwriting.

Relative phase is the universe changing the plot.

### Forgetting normalisation

Quantum states should be normalised so that the total probability sums to 1.

The physics layer normalises where appropriate after gate batches, but custom state construction should still be treated carefully.

If your amplitudes imply probabilities summing to something other than 1, the universe may continue, but it will do so with a raised eyebrow.

---

## Summary

Use complex numbers in two related ways:

* as ordinary `Complex` values inside `QuBit<Complex>`;
* as complex amplitudes attached to possible values or basis states.

The first lets complex numbers participate in generic superposition operations.

The second lets the quantum simulation layer represent phase, interference, and real state-vector behaviour.

Without complex amplitudes, you can model uncertainty.

With them, you can model the way uncertainty turns around later and recognises itself.
