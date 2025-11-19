Here’s a proposed new README that keeps the humour but makes the “look how much this thing actually does” story a lot clearer.

Feel free to copy–paste and tweak, but I’ve written it so you can more or less drop it in as-is.

---

## QuantumSuperposition (.NET Library)

[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)
![Quantum Algorithms Inside](https://img.shields.io/badge/quantum--algorithms-included-blueviolet)

> .NET’s most confident way to say “maybe”

QuantumSuperposition is a .NET library that lets your variables live in several states at once, then collapse them when you finally make up your mind. Think Schrödinger’s cat, but with fewer ethical questions and more LINQ. 

It has two main personalities:

1. **Generic superposition engine** for `QuBit<T>` and `Eigenstates<T>`
   Use it to run arithmetic, comparisons and LINQ-style queries over many possible values at once, with complex weights, sampling, entanglement and non-observational operations. 

2. **Physics flavoured quantum system** with actual gates and algorithms
   Use `QuantumSystem`, `QuantumGate`, and `QuantumAlgorithms` to build circuits with Hadamards, CNOTs, QFT and Grover’s search. You get gate queues, ASCII circuit visualisation, partial observation and real multi-qubit tensor products. 

---

## TL;DR: What can it actually do?

### 1. Complex amplitudes and real tensor products

* `QuBit<T>` supports **complex amplitudes** with weight-aware equality, sampling and arithmetic.
* `QuantumMathUtility.TensorProduct` builds full multi-qubit joint states from `QuBit<int>` basis states.
* `PhysicsQubit` is a friendly `QuBit<int>` for computational basis {0, 1} with Bloch sphere constructors and `Zero` / `One` shortcuts. 

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;

// Two 1-qubit states in the computational basis
var q1 = new QuBit<int>(new[] { 0, 1 });
var q2 = new QuBit<int>(new[] { 0, 1 });

// Full 2-qubit joint state with complex amplitudes
var joint = QuantumMathUtility<int>.TensorProduct(q1, q2);
```

You get an actual tensor product over basis indices, not hand-wavy “we multiplied some lists and hoped”.

---

### 2. QuantumSystem: multi-qubit state, partial observation and scheduling

`QuantumSystem` is the “physics core” that tracks qubits by index, builds the joint state from tensor products and lets you peek at just part of the universe.

* Register qubits and build the full state with `SetFromTensorProduct(...)`.
* Partially observe selected qubits with `PartialObserve`, while leaving the rest in suspense.
* Schedule gates on specific qubit indices and process them as a queue.

```csharp
using QuantumSuperposition.Systems;
using QuantumSuperposition.Core;

// Create a system with three qubits
var system = new QuantumSystem();

// Build joint state from individual qubits
system.SetFromTensorProduct(autoNormalise: true,
    new QuBit<int>(system, new[] { 0 }),
    new QuBit<int>(system, new[] { 1 }),
    new QuBit<int>(system, new[] { 2 }));

// Peek only at qubits 0 and 2
var outcome = system.PartialObserve(new[] { 0, 2 });
```

---

### 3. Entanglement groups, collapse propagation and diagnostics

This is not just “make EPR pair, call Collapse” and go home.

QuantumSuperposition gives you a full **entanglement graph**:

* Entanglement groups with labels and versioning.
* Collapse propagation across a group when any member is observed.
* Partial collapse staging: observe some qubits now, others later.
* Locking / freezing: “Schrödinger’s do not disturb” for critical sections.
* Diagnostics: print entanglement stats and inspect graph structure. 

```csharp
using QuantumSuperposition.Systems;
using QuantumSuperposition.Entanglement;
using QuantumSuperposition.QuantumSoup;
using System.Numerics;

var system = new QuantumSystem();
var qA = new QuBit<int>(system, new[] { 0 });
var qB = new QuBit<int>(system, new[] { 1 });

qA.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
qB.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);

system.Entangle("BellPair_A", qA, qB);
system.SetFromTensorProduct(true, qA, qB);

// Observe A; B collapses in solidarity
var observed = qA.Observe();
```

Compared to simpler libraries that give you one EPR pair and a prayer, this is a proper entanglement manager with tagging, locking, multi-party agreement and guardrails against nonsense like cross-system linking. 

---

### 4. Gate abstraction and full circuit scheduling

QuantumSuperposition comes with a **gate model**:

* `QuantumGate` for a single matrix.
* `QuantumGates` for built-ins: Root-NOT, Hadamard, Pauli-X, T, T†, parametric RX and more.
* `QuantumGateTools` for matrix inversion, comparison and helpers. 

You can compose, invert and schedule gates on a `QuantumSystem`, then visualise the gate queue as ASCII circuit diagrams.

```csharp
using QuantumSuperposition.Systems;
using QuantumSuperposition.Core;

var system = new QuantumSystem();

// Queue some gates
system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

// Visualise circuit before processing
var schedule = system.VisualizeGateSchedule(totalQubits: 2);
Console.WriteLine(schedule);

// Process queue
system.ProcessGateQueue();
```

Composition and inversion:

```csharp
var rootNot = new QuantumGate(QuantumGates.RootNot);
var doubleRoot = rootNot.Then(rootNot);     // Should become Pauli-X
var pauliX = new QuantumGate(QuantumGates.PauliX);

var invertedT = QuantumGateTools.InvertGate(QuantumGates.T);
var tDagger = new QuantumGate(QuantumGates.T_Dagger);
```

---

### 5. Quantum algorithms: QFT and Grover’s search

On top of gates and `QuantumSystem` you get real quantum algorithms:

* **Quantum Fourier Transform (QFT)** on any list of qubits.
* **Grover’s Search** with plug-in oracle, multi-controlled Z and diffusion operator. 

```csharp
using QuantumSuperposition.Systems;
using QuantumSuperposition.Core;

// QFT on three qubits
var system = new QuantumSystem();
int[] qftQubits = { 0, 1, 2 };
QuantumAlgorithms.QuantumFourierTransform(system, qftQubits);

// Grover search for a marked item on two qubits
int[] groverQubits = { 0, 1 };
Func<int[], bool> oracle = bits => bits[0] == 1 && bits[1] == 0;
QuantumAlgorithms.GroverSearch(system, groverQubits, oracle);
```

These algorithms are built in terms of the same gate primitives you can use yourself, so you can inspect, tweak and extend the circuits rather than treating them as mysterious black boxes. 

---

### 6. Generic superposition layer: QuBit<T> and Eigenstates<T>

All of this sits on top of a **generic superposition engine** that works for any `T`, not just physics bits. 

You get:

* `QuBit<T>`: a weighted collection of possible values with complex amplitudes.
* `Eigenstates<T>`: preserve original keys while you transform the derived values.
* LINQ-style operations like `.Select`, `.Where`, `.SelectMany`.
* `p_op` and `p_func` for conditional and functional transforms without collapse.
* Weighted sampling (`SampleWeighted`, `MostProbable`) and multi-basis observation.
* Collapse replay, mockable collapse and seeded randomness for deterministic tests. 

Basic example:

```csharp
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Core;

var qubit = new QuBit<int>(new[] { 1, 2, 3 });
var doubled = qubit.Select(x => x * 2);

// Sample according to current weights
var sample = doubled.SampleWeighted();
```

More interesting: ask the multiverse which numbers are prime, or which values evenly divide a target, without writing your own loops. 

---

## QuantumRegister

A compact, high-level abstraction for treating one or more qubit indices as a coherent register inside a `QuantumSystem`. It provides a convenient value view over part of the global wavefunction.

QuantumRegister is designed for scenarios where you want to work with qubit groups as meaningful units, for example decoding integers, extracting structured values, or collapsing only a subset of the system.

Key features include:

* Construction from `QuBit<int>` or `PhysicsQubit` instances
* Construction from integer values, creating a single basis-state register
* Construction from explicit complex amplitude vectors
* Partial collapse of only the qubits in the register
* Integer decoding over arbitrary bit ranges
* Safe interaction with `QuantumSystem` using sorted index sets and collapse locking

```csharp
// From qubits already in the system
var system = new QuantumSystem();
var q0 = new QuBit<int>(system, new[] { 0 });
var q1 = new QuBit<int>(system, new[] { 1 });
var reg = new QuantumRegister(q0, q1);

// From a basis integer
var encoded = QuantumRegister.FromInt(value: 3, bits: 2, system);

// From explicit amplitudes
var amps = new[] { Complex.One / Math.Sqrt(2), Complex.One / Math.Sqrt(2) };
var regAmp = QuantumRegister.FromAmplitudes(amps, system);

// Collapse only this register
int[] measured = reg.Collapse();

// Decode values
int full = reg.GetValue();
int low3 = reg.GetValue(offset: 0, length: 3);
```

For full documentation see `docs/QuantumRegister.md`. 

---

## Quick start

### Installation

Using .NET CLI:

```bash
dotnet add package QuantumSuperposition
```

Using NuGet Package Manager Console:

```powershell
Install-Package QuantumSuperposition
```

### Namespaces

For generic superpositions:

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
```

For physics-style gates and systems:

```csharp
using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
```

---

## Documentation map (so you know where the good stuff is)

Think of the main README as the trailer. The full movie lives in the docs:

## Documentation Map

A tour of the full library, from functional superpositions to full quantum circuits:

* **Functional Operations**
  High-level LINQ-style transforms, `p_op`, `p_func`, non-observational arithmetic, and functional pipelines.
   `docs/FunctionalOps.md`

* **Entanglement & Collapse**
  Entanglement groups, collapse propagation, partial collapse, locking, and diagnostic tooling.
   `docs/Entanglement.md`

* **Complex Number Support**
  Working with complex amplitudes, projections, probability magnitudes, and complex superpositions.
   `docs/ComplexSupport.md`

* **Logic Gates & QuantumSystem**
  Built-in gates (H, X, T, Root-NOT, RX…), gate composition/inversion, scheduling, and ASCII circuit diagrams.
   `docs/LogicGates.md`

* **Quantum Algorithms**
  Quantum Fourier Transform (QFT), Grover’s Search, diffusion operators, controlled-phase tools, and algorithm scaffolding.
   `docs/QuantumAlgorithms.md`

* **PhysicsQubit - Bloch Sphere & Basis Shortcuts**
  A specialised computational-basis qubit with amplitude constructors, Bloch-sphere initialisation, and `Zero` / `One` helpers.
   `docs/PhysicsQubit.md`

* **QuantumRegister - Coherent Qubit Groups**
  A compact, high-level abstraction for treating one or more qubit indices as a coherent register inside a `QuantumSystem`.
   `docs/QuantumRegister.md`

* **Canonical Quantum States - Factories for Common States**
  Built-in factories for common multi-qubit states: EPR pairs, W states, GHZ states.
   `docs/CanonicalStates.md`

* **Gate Register Sugar**
  Extension methods for applying gates directly on `QuantumRegister` instances.
   `docs/GateRegisterSugar.md`

* **Gate Catalogue Extensions**
  New gates and factories exposed via `QuantumGates` for parity with common algorithm needs.
   `docs/GateCatalogue.md`

---

## Contributing

Bug spotted in the multiverse, or a new gate you want to summon?

* Open an issue
* Send a pull request
* Bring your own obscure quantum joke

---

## License

Released under the **Unlicense**: do whatever you like with it, short of blaming us if you accidentally open a portal.

---

## Contact

Questions, fan mail, or a probability amplitude shaped like a duck:

`support@findonsoftware.com`

---
