# Positronic .NET Playground

**QuantumSuperposition** and **PositronicVariables**
*Subtle multiverse engineering for .NET developers who should probably know better.*

Two complementary .NET libraries for modelling uncertainty, multi-state computation, quantum-style reasoning, reversible temporal logic and timeline convergence. One gives you strongly typed superpositions and actual quantum circuit tooling. The other lets your variables negotiate with their own futures until everyone agrees. [![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.17863969.svg)](https://doi.org/10.5281/zenodo.17863969)

---
## Overview

| Layer | Purpose | You Use It For |
| ----- | ------- | -------------- |
| QuantumSuperposition | Generic superpositions plus physics style multi qubit simulation | Probabilistic logic, gate level circuits, entanglement, QFT, Grover |
| PositronicVariables | Temporal convergence and causal feedback loops built atop `QuBit<T>` | Recursive definitions, paradox resolution, timeline stabilisation |

They are independent. Together they let you go from Maybe(x) arithmetic through to multi qubit algorithms and then wrap those results in time reversing variables that can refine themselves.

---
## Why Both Exist

- QuantumSuperposition makes uncertainty first class
- PositronicVariables makes temporal iteration and convergence declarative
- You can model value sets, entangled states and causal loops without abandoning strong typing
- They share the same superposition semantics so mental context switches are minimal

---
## Explainer Video
A ten minute overview with motivation and working examples:  
https://www.youtube.com/watch?v=bQ9JxqP5kBQ

---
# Included Libraries

## QuantumSuperposition
[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)

*A strongly typed multiverse engine and quantum simulation toolkit.*

QuantumSuperposition models values that exist in multiple possible states simultaneously. You get both generic, type safe superpositions and physics style multi qubit systems. The goal is to make multi valued logic, probabilistic reasoning and quantum inspired computation feel like ordinary C#.

### Generic Superposition Layer

- `QuBit<T>` and `Eigenstates<T>` with complex amplitude weighting
- Weighted, unweighted or arbitrary superpositions
- Arithmetic across states
- Comparisons, conditions, filtering
- LINQ style operations (`Select`, `Where`, `SelectMany`)
- Functional transforms via `p_func` and `p_op`
- Non destructive sampling and basis changes
- Collapse replay, seeded randomness, mockable collapse for deterministic tests

Write declarative logic that reasons over possibilities without forcing early collapse.

### Physics Style Quantum Engine

#### Complex Amplitudes and True Tensor Products
- Multi qubit joint states built from `QuBit<int>` basis states
- Full complex number weighting
- Normalisation and probability derivation
- Matrix algebra utilities

#### QuantumSystem
- Multi qubit state construction via `SetFromTensorProduct`
- Partial observation of specific qubit subsets
- Collapse propagation across entanglement groups
- Locking, diagnostics, controlled collapse behaviour
- Safe consistent management of entanglement networks

#### Entanglement Engine
- Group labels and version tracking
- Collapse event propagation
- Partial collapse staging
- Inspect, freeze and debug relationships

#### Gate Model and Scheduling
- `QuantumGate`, `QuantumGates`, `QuantumGateTools`
- Built in primitives: Hadamard, Pauli X, Root NOT, T, T dagger, parametric RX
- Multi qubit controlled gates
- Composition via `Then`
- Gate inversion and matrix equality checks
- Single, two and multi qubit gates
- Gate queue execution with ASCII visualisation

#### Quantum Algorithms
- Quantum Fourier Transform (QFT) on arbitrary qubit lists
- Grover search with oracle integration, multi controlled Z, diffusion operator
- Algorithms assembled from the same gate primitives you can manipulate

### Documentation
Focused documentation per subsystem: entanglement behaviour, complex number support, gate references, functional programming patterns.

[Full README and documentation](QuantumSuperposition/readme.md)

---
## PositronicVariables
[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

*Reversible temporal logic built atop `QuBit<T>`.*

A higher level framework using the generic superposition layer to simulate variables that evolve across hypothetical timelines. No physical qubits or gates. It focuses on:

- Feedback loops
- Converging nodes
- Recursive dataflow networks
- Multi state unification
- Time reversible computation

A `PositronicVariable` may change across iterative passes until all dependent values agree. Cycles are detected and divergent states merge into consistent outcomes.

### Key Features
- Creation and linking of variables holding superpositions
- Automatic cycle detection across the computation graph
- Multi pass evaluation of recursive or self referential definitions
- Deterministic convergence loops with clear stopping conditions
- Rewinding and replaying timelines until stability
- Ideal for distributed agreement models, logical paradoxes and non linear recursion

It is not a quantum simulator. It is a logic engine that reuses the same superposition semantics.

[Full README](PositronicVariables/readme.md)

### Architecture & Concurrency 
- Convergence runs on a single, very patient `ConvergenceCoordinator` thread. Everyone else takes a number and waits for tea.
- Timeline mutations pass through a single gate inside `PositronicVariable<T>`. Outside callers see an `IReadOnlyList<QuBit<T>>`, because we’ve learned to childproof reality.
- User code may update from many threads through transactions; writes apply during commit, not during dramatic monologues.
- Ledger entries are buffered per transaction and appended exactly once after commit (no time paradoxes due to duplicate regrets).
- The archivist only receives immutable snapshots. No one gets to share mutable lists with their past self.

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
- C# 10 and .NET 8 or newer (also tested on .NET 9 previews where available)
- Fully NuGet compatible
- Extensive tests: quantum math, superpositions, entanglement handling, positronic timeline logic

---
# Philosophy
Uncertainty is a valid state. A variable should be allowed to have several opinions. Time should be reversible when it helps. Collapse is a programming tool not a mystical event.

---
# Contributing
Pull requests welcome. If your changes spawn an infinite recursion loop or entangle two unrelated projects please document the phenomenon before submitting.

Issues especially welcome for:
- Quantum state inconsistencies
- Convergence failures
- Timeline deadlocks
- Undefined behaviour regressions

---
# License
Released under the Unlicense. Use, modify and distribute freely. If your compiler attains consciousness that is between you and it.

---
# Final Thoughts
Suited for developers who:
- Enjoy recursion as a pastime
- Want LINQ to operate over superposed values
- See time as a suggestion not an obligation
- Appreciate strong typing but wish it came with a multiverse

If that is you, welcome.
