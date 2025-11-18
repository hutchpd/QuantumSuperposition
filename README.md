# Positronic .NET Playground

**QuantumSuperposition** and **PositronicVariables**
*Subtle multiverse engineering for .NET developers who should probably know better.*

This repository contains two related .NET libraries for modelling uncertainty, multi-state computation, quantum-style reasoning, and reversible temporal logic. They are independent but complementary. Together they allow you to build anything from toy superpositions to full multi-qubit circuits to recursive timeline-driven networks.

Both libraries are production grade, fully testable, deterministic where required, and light on mysticism.

---

## Explainer Video

A ten minute overview introducing the project, the motivation behind it, and some working examples:
[https://www.youtube.com/watch?v=bQ9JxqP5kBQ](https://www.youtube.com/watch?v=bQ9JxqP5kBQ)

---

# Included Libraries

## QuantumSuperposition

[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)

*A strongly typed multiverse engine and quantum simulation toolkit.*

QuantumSuperposition is a comprehensive library for modelling values that exist in multiple possible states simultaneously. It supports both generic, type-safe superpositions and physics-style multi-qubit systems. The goal is to make multi-valued logic, probabilistic reasoning, and quantum-inspired computation feel like ordinary C#.

This is a summary of its actual capabilities.

### Generic Superposition Layer

Designed for expressive programming over many potential values. Features include:

* `QuBit<T>` and `Eigenstates<T>` with complex amplitude weighting
* Weighted, unweighted or arbitrary superpositions
* Arithmetic across states
* Comparisons, conditions, filtering
* LINQ-style operations (`Select`, `Where`, `SelectMany`)
* Functional transforms via `p_func` and `p_op`
* Non-destructive sampling and basis changes
* Collapse replay, seeded randomness, and mockable collapse for deterministic tests

This layer is entirely independent of the physics simulation. It allows you to write declarative logic that reasons over possibilities.

### Physics-Style Quantum Engine

A full multi-qubit simulation environment, including:

#### Complex amplitudes and true tensor products

* Multi-qubit joint states built from `QuBit<int>` basis states
* Full complex-number weighting
* Normalisation and probability derivation
* Support utilities for matrix algebra

#### QuantumSystem

A container for actual qubit indices with:

* Multi-qubit state construction via `SetFromTensorProduct`
* Partial observation of specific qubit subsets
* Collapse propagation across entanglement groups
* Locking, diagnostics, and controlled collapse behaviour
* Safe and consistent management of entanglement networks

#### Entanglement Engine

QuantumSuperposition contains a richer entanglement model than many educational quantum libraries:

* Entanglement groups with labels and version tracking
* Propagation of collapse events through linked qubits
* Partial collapse staging
* Ability to inspect, freeze and debug entanglement relationships

This is aimed at developers who want more than a single EPR pair and a handwave.

#### Gate Model and Scheduling

The library includes an extendable gate framework:

* `QuantumGate`, `QuantumGates`, and `QuantumGateTools`
* Built-in primitives

  * Hadamard
  * Pauli-X
  * Root-NOT
  * T and T-dagger
  * Parametric RX
  * Multi-qubit controlled gates
  * Gate composition via `Then`
  * Gate inversion and matrix equality checks
  * Scheduling of gates on `QuantumSystem`

  * Single-qubit, two-qubit and multi-qubit gates
  * Gate queue execution
  * ASCII visualisation of the pending circuit

#### Quantum Algorithms

Algorithms implemented using the same gate primitives:

* Quantum Fourier Transform on arbitrary qubit lists
* Grover's Search with oracle integration, multi-controlled Z, and diffusion operator
* All logic written in terms of your gates so circuits remain visible and editable

### Documentation

The QuantumSuperposition project maintains focused documentation files that expand on each subsystem. These include examples, explanations of entanglement behaviour, complex number support, gate references and functional programming patterns.

[Full README and documentation](QuantumSuperposition/readme.md)

---

## PositronicVariables

[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

*Reversible temporal logic built atop QuBit<T>.*

PositronicVariables is a higher-level framework that uses the generic superposition layer from QuantumSuperposition to simulate variables that evolve across hypothetical timelines. It does not deal in physical qubits or gates. Instead it focuses on:

* Feedback loops
* Converging nodes
* Recursive dataflow networks
* Multi-state unification
* Time-reversible computation

A PositronicVariable represents a value that may change across iterative passes of a simulation until all dependent values agree. The system automatically detects cycles and merges divergent states into consistent outcomes.

### Key Features

* Creation and linking of PositronicVariables that hold superpositions
* Automatic detection of cycles across the computation graph
* Multi-pass evaluation of recursive or self-referential definitions
* Convergence loops with deterministic stopping conditions
* Rewinding and re-evaluating timelines until the system stabilises
* Ideal for modelling distributed agreement, logical paradoxes, or non-linear recursion

It is not a quantum simulator. It is a logic engine built on top of the generic superposition semantics from QuantumSuperposition.

[Full README](PositronicVariables/readme.md)

---

## Example

```csharp
var x = new PositronicVariable<int>(0);
var y = new PositronicVariable<int>(1);

var node = new NeuralNodule<int>(inputs =>
{
    var sum = inputs.Sum();
    return new QuBit<int>(new[] { sum % 5, (sum + 1) % 5 });
});

node.Inputs.Add(x);
node.Inputs.Add(y);

NeuralNodule<int>.ConvergeNetwork(node);

Console.WriteLine($"Final Output: {node.Output}");
```

---

# Development

* C# 10 and .NET 8 or newer
* Fully NuGet compatible
* Tests included across quantum math, superpositions, entanglement handling, and positronic timeline logic

---

# Philosophy

This project exists to explore the intersection of uncertainty, functional programming and reversible computation. If you have ever wanted your variables to represent parallel possibilities, or your algorithms to evaluate across multiple timelines before settling on a final answer, this playground is for you.

A few principles:

* Uncertainty is a valid state
* A variable should be allowed to have several opinions
* Time should be reversible when it is useful
* Collapse is a programming tool, not a mystical event

---

# Contributing

Pull requests are welcome. If your changes spawn an infinite recursion loop or entangle two unrelated projects, please document the phenomenon before submitting.

Issues are also welcome, especially reports regarding:

* Quantum state inconsistencies
* Convergence failures
* Timeline deadlocks
* Undefined behaviour that was previously specified

---

# License

Released under the Unlicense.
You may use, modify and distribute the code freely.
If your compiler attains consciousness, that is between you and it.

---

# Final Thoughts

This repository is suited for developers who:

* Enjoy recursion as a pastime
* Want LINQ to operate over superposed values
* See time as a suggestion rather than an obligation
* Appreciate strong typing but wish it came with a multiverse

If that is you, welcome.
