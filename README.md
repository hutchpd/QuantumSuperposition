# Positronic .NET Playground

**QuantumSuperposition** and **PositronicVariables**

*Two .NET libraries for developers who looked at ordinary state management and quietly wondered whether reality was trying hard enough.*

This repository contains a pair of related experiments in typed uncertainty, quantum-style computation, temporal convergence, recursive state, and variables with more inner life than is strictly healthy.

`QuantumSuperposition` models possible values.

`PositronicVariables` models possible histories.

The first lets a value remain many things until observation becomes necessary. The second lets later assignments travel backwards through replay, revising earlier assumptions until they meet ordinary forward execution in a stable loop.

That meeting point is where the interesting damage happens.

One library lets values exist in multiple possible states and manipulate those possibilities without collapsing them too early. The other lets variables revise themselves across iterative timelines until the contradiction either settles down, becomes useful, or has the decency to admit it is a paradox.

It is not trying to be the sensible answer to a normal software problem.

It is, however, trying to be a well-built answer to a beautifully unreasonable one.

[Read the paper](https://doi.org/10.5281/zenodo.17863968)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.17863968.svg)](https://doi.org/10.5281/zenodo.17863968)

---

## What lives here

| Library                | What it is                                                    | What it is for                                                                                                                                    |
| ---------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `QuantumSuperposition` | A strongly typed superposition and quantum simulation toolkit | Typed uncertainty, probabilistic logic, LINQ over possible values, entanglement, gates, QFT, Grover search, noisy quantum systems                 |
| `PositronicVariables`  | A temporal convergence engine built on top of `QuBit<T>`      | Recursive state, feedback loops, paradox handling, transactional timeline updates, replay, convergence, and other forms of polite causality abuse |

They can be used separately.

Together, they form a small computational weather system where values may be many things, histories may require revision, and the compiler is asked to remain calm.

---

## The short version

`QuantumSuperposition` makes uncertainty explicit.

A `QuBit<T>` is not a value that forgot to make up its mind. It is a formal set of possible typed values, each with its own weight or amplitude. You can transform it, filter it, combine it, sample it, or collapse it when the paperwork finally becomes unavoidable.

`PositronicVariables` then asks a more troubling question:

What if variables could carry their future backwards?

A `PositronicVariable<T>` does not merely update as execution moves forwards. It can take a later assignment and propagate that state backwards into the execution trace, forcing earlier reads to be reconsidered on the next pass.

Ordinary code moves forwards.

Positronic state moves backwards.

The loop forms where those two directions meet.

A `PositronicVariable<T>` can therefore move through repeated evaluation passes, revising its state as dependent values change, until the system converges on something stable. If it cannot become one thing, it may become several. Sometimes that is failure. Sometimes that is the answer being honest.

---

## Installation

Install the packages independently depending on which species of unreality you require.

```bash
dotnet add package QuantumSuperposition
dotnet add package PositronicVariables
```

---

## QuantumSuperposition

[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)

`.NET's most confident way to say "maybe".`

`QuantumSuperposition` is the lower-level library. It has two main layers.

### 1. Generic typed superpositions

This is the part where ordinary C# values are allowed to stop being so aggressively singular.

```csharp
var q = new QuBit<int>(new[] { 1, 2, 3 });

var doubled = q.Select(x => x * 2);
var even = doubled.Where(x => x % 2 == 0);

var observed = even.Observe();
```

The important part is not that the value is vague. It is not vague. It is structured.

You can work with possible values without immediately collapsing them into one value. That means arithmetic, filtering, comparison, LINQ-style transforms, weighted sampling, and deterministic test hooks can all happen before observation barges in with a clipboard.

Features include:

* `QuBit<T>` and `Eigenstates<T>`
* Weighted and unweighted superpositions
* Complex amplitude support
* LINQ-style `Select`, `Where`, and `SelectMany`
* Functional transforms via `p_func` and `p_op`
* Arithmetic and comparisons across possible values
* Non-destructive sampling
* Collapse replay and seeded randomness for deterministic tests
* Mockable collapse behaviour for code that dislikes surprises

### 2. Physics-style quantum simulation

This is the part where the abstraction puts on a lab coat and starts using complex numbers responsibly.

`QuantumSuperposition` also includes a gate-based quantum simulation layer with real tensor products, complex amplitudes, partial observation, entanglement management, gate scheduling, quantum algorithms, and optional noise.

Features include:

* `QuantumSystem`
* `PhysicsQubit`
* `QuantumRegister`
* Complex-valued gates and state vectors
* Full tensor product expansion
* Partial observation of qubit subsets
* Collapse propagation across entanglement groups
* Gate scheduling and ASCII circuit visualisation
* Built-in gates including Hadamard, Pauli X/Y/Z, Root NOT, T, T dagger, RX, SWAP, Toffoli, Fredkin, and controlled gates
* Quantum Fourier Transform
* Grover search
* Canonical states such as EPR, W, and GHZ
* Noisy quantum systems
* Readout mitigation
* Zero Noise Extrapolation
* Probabilistic Error Cancellation

Example:

```csharp
var system = new QuantumSystem();

var q0 = new QuBit<int>(system, new[] { 0, 1 });
var q1 = new QuBit<int>(system, new[] { 0, 1 });

system.SetFromTensorProduct(
    propagateCollapse: true,
    q0,
    q1);

system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

system.ProcessGateQueue();

var observed = system.PartialObserve(new[] { 0, 1 });
```

A global wavefunction is not impressed by your domain model. Eventually, every lovely typed value must present itself at the counter and become addressable. For that, `SetFromTensorProduct` supports basis mapping, including custom `Func<T, int>` mappers for domain types that need to become computational basis indices.

---

## PositronicVariables

[![NuGet](https://img.shields.io/nuget/v/PositronicVariables.svg)](https://www.nuget.org/packages/PositronicVariables)

`PositronicVariables` is the higher-level temporal machinery.

It uses the generic superposition model from `QuantumSuperposition`, but it is not a quantum simulator. There are no physical qubits here, no gates, no circuits, no Bloch sphere with delusions of grandeur.

Instead, it models variables that can evolve across repeated passes until a network stabilises.

The core mechanic is directional:

```text
Forward execution:
    read value -> calculate result -> assign new value

Backward positronic propagation:
    assignment -> revise earlier state -> replay forward expression
```

A normal variable is carried forwards by the programme.

A positronic variable carries consequences backwards through the timeline.

The convergence loop appears when those two directions touch. The engine walks around that loop until the values stop changing, or until the smallest honest answer is a superposition of the histories that could not be reduced to one.

```csharp
var x = PositronicVariable<int>.GetOrCreate("x", 1);
var y = PositronicVariable<int>.GetOrCreate("y", 2);

TransactionV2.RunWithRetry(tx =>
{
    tx.RecordRead(x);
    tx.RecordRead(y);

    var next = x.GetCurrentQBit() + y.GetCurrentQBit();

    tx.StageWrite(x, next);
});
```

This is useful when state is recursive, relational, self-referential, or otherwise behaving like it has read too much science fiction.

Features include:

* `PositronicVariable<T>`
* Timeline-style state evolution
* Multi-pass convergence
* Recursive dependency handling
* Feedback loops
* `NeuralNodule<T>` computation nodes
* STM-backed transactional updates
* Read-only transaction fast paths
* Commit, retry, abort, validation, and lock telemetry
* Snapshot export and restore
* File-backed append-only ledger audit sink
* Debug checks for mutation outside the approved gateways

A simple paradox:

```csharp
var antival = PositronicVariable<int>.GetOrCreate("antival", -1);

var val = -1 * antival;

antival.State = val;
```

After convergence, this does not have to become one clean integer. It can become the honest shape of the contradiction:

```text
antival = any(-1, 1)
val     = any(1, -1)
```

Going forwards, `val` is calculated from `antival`.

Going backwards, `antival.State = val` tells `antival` what it must become.

If `antival` is `-1`, `val` becomes `1`, and the assignment sends `1` backwards.

If `antival` is `1`, `val` becomes `-1`, and the assignment sends `-1` backwards.

The two directions cannot settle on a single integer, so the stable shape of the programme is a two-state paradox.

Some systems resolve by choosing.

Some resolve by admitting the choice was the wrong question.

---

## Why these libraries belong together

`QuantumSuperposition` gives the project its mathematics of possibility.

`PositronicVariables` gives it a theory of consequence.

The first says:

> A value may have several possible states, and those states can be transformed with precision.

The second says:

> A variable may have a history, and later state may need to travel backwards through replay until the past and present stop contradicting one another.

That shared idea is the spine of the repository: uncertainty should not be mush, and neither should causality. Both should be typed, inspectable, transformable, testable, and occasionally allowed to be funny.

---

## Documentation

Start here, then choose your rabbit hole.

### Main library documents

* `QuantumSuperposition/readme.md`
* `PositronicVariables/readme.md`

### QuantumSuperposition docs

* `QuantumSuperposition/docs/UsageExamples.md`
* `QuantumSuperposition/docs/FunctionalOps.md`
* `QuantumSuperposition/docs/ComplexSupport.md`
* `QuantumSuperposition/docs/BasisMapping.md`
* `QuantumSuperposition/docs/Entanglement.md`
* `QuantumSuperposition/docs/PhysicsQubit.md`
* `QuantumSuperposition/docs/QuantumRegister.md`
* `QuantumSuperposition/docs/CanonicalStates.md`
* `QuantumSuperposition/docs/LogicGates.md`
* `QuantumSuperposition/docs/GateCatalogue.md`
* `QuantumSuperposition/docs/GateRegisterSugar.md`
* `QuantumSuperposition/docs/QuantumAlgorithms.md`
* `QuantumSuperposition/docs/Noise.md`
* `QuantumSuperposition/docs/Equality.md`

---

## Development

The project targets modern .NET and is designed to be usable as normal NuGet packages, despite the fact that it is also quite clearly the result of someone letting a metaphor escape into the type system.

General expectations:

* C# 10 or later
* .NET 8 or newer
* NuGet-compatible package structure
* Unit tests for the mathematical, transactional, and convergence behaviour
* Deterministic testing where randomness would otherwise get ideas above its station

---

## Project philosophy

This project is built around a few questionable but persistent beliefs:

* Uncertainty is a valid state.
* Collapse should be explicit.
* A variable can have several opinions and still be useful.
* Strong typing makes absurdity safer.
* Time is not always a line. Sometimes it is a negotiation between forward execution and backward consequence.
* A contradiction that converges is not necessarily a bug.
* A joke in the documentation is allowed, provided it is carrying its own technical weight.

The aim is not to hide the strangeness.

The aim is to make the strangeness precise.

---

## Useful for

This repository may be of interest if you:

* enjoy C# generics slightly more than is medically recommended;
* want LINQ-style operations over possible values;
* are interested in quantum-inspired programming models;
* want a toy quantum simulator that still respects the mathematics;
* like recursive state systems and convergence problems;
* need a playground for paradoxes, timelines, amplitudes, gates, and typed uncertainty;
* believe a README should occasionally look back at the reader.

It may be less useful if you want:

* a production quantum computing framework;
* a conventional state management library;
* a serious enterprise dependency with a reassuring diagram of boxes;
* software that avoids eye contact.

---

## Contributing

Issues and pull requests are welcome.

Useful contributions include:

* correcting quantum behaviour;
* improving convergence logic;
* tightening documentation;
* adding examples;
* improving deterministic tests;
* finding places where the multiverse leaks through the floorboards.

If a change introduces a paradox, please include reproduction steps.

If it resolves a paradox, please include witnesses.

---

## License

Released under the Unlicense.

Use it, fork it, modify it, ship it, rename it, misunderstand it, or place it gently in a drawer until the future is ready.

No warranty is provided. If your compiler achieves self-awareness, consult local regulations before feeding it additional source code.

---

## Final note

The project is an art piece with unit tests.

That does not mean it is unserious.

It means the seriousness has been pointed at something unreasonable on purpose.
