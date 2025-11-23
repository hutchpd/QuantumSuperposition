Here’s a proposed new README that keeps the humour but makes the “look how much this thing actually does” story a lot clearer.

Feel free to copy–paste and tweak, but I’ve written it so you can more or less drop it in as-is.

---

## QuantumSuperposition (.NET Library) — v1.7.5

[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)
![Quantum Algorithms Inside](https://img.shields.io/badge/quantum--algorithms-included-blueviolet)

> .NET's most confident way to say "maybe"

QuantumSuperposition lets your variables live in several states at once then collapse them when you finally make up your mind. Think Schrödinger's cat but with fewer ethical questions and more LINQ.

Two main personalities:

1. **Generic superposition engine** for `QuBit<T>` and `Eigenstates<T>`: arithmetic, comparisons and LINQ style queries over many possible values at once with complex weights, sampling, entanglement and non observational operations.
2. **Physics flavoured quantum system**: `QuantumSystem`, `QuantumGate`, `QuantumAlgorithms` for circuits with Hadamards, CNOTs, QFT and Grover search. Includes gate queues, ASCII circuit visualisation, partial observation and real multi qubit tensor products.

---

## TL;DR What It Can Do

### 1. Complex Amplitudes and Real Tensor Products

- `QuBit<T>` supports complex amplitudes with weight aware equality, sampling and arithmetic
- `QuantumMathUtility.TensorProduct` builds full multi qubit joint states
- `PhysicsQubit`: computational basis {0,1} with Bloch sphere constructors and `Zero` / `One` helpers

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

Actual tensor product over basis indices not hand wavy list multiplication.

### 2. QuantumSystem: Multi Qubit State, Partial Observation, Scheduling

- Track qubits by index and build joint state via tensor products
- Partially observe subsets with `PartialObserve`
- Schedule gates then process as a queue

  ```csharp
  var system = new QuantumSystem();

  // Build joint state from individual qubits
  system.SetFromTensorProduct(true,
      new QuBit<int>(system, new[] { 0 }),
      new QuBit<int>(system, new[] { 1 }),
      new QuBit<int>(system, new[] { 2 }));

  // Peek only at qubits 0 and 2
  var outcome = system.PartialObserve(new[] { 0, 2 });
  ```

Note: In the v1.7.5 release we fixed a subtle bug where `SetFromTensorProduct` created temporary local `QuBit<T>` objects that weren't registered with the `QuantumSystem`. That meant collapse propagation, partial observation and entanglement bookkeeping could get confused like a cat chasing two red dots. Now local qubits passed into `SetFromTensorProduct` are promoted to system-managed `QuBit<T>` instances (constructed via `QuBit(QuantumSystem, int[])`) and registered so they keep their indices and entanglement wiring intact.

Also new: you can pass an optional `Func<T,int>` basis mapper to `SetFromTensorProduct` for custom domains (enums, tiny structs pretending to be bits). Defaults for `int`, `bool`, and any enum via `Convert.ToInt32` are provided. Build states your way; just keep them in the computational basis.

### 3. Entanglement Graph, Collapse Propagation, Diagnostics

- Group labels and versioning
- Collapse propagation when any member observed
- Partial collapse staging
- Locking / freezing for critical sections
- Diagnostics: print stats and inspect graph structure

  ```csharp
  var system = new QuantumSystem();
  var qA = new QuBit<int>(system, new[] { 0 });
  var qB = new QuBit<int>(system, new[] { 1 });

  qA.WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);
  qB.WithWeights(new Dictionary<int, Complex>{{0,1.0},{1,1.0}}, true);

  system.Entangle("BellPair_A", qA, qB);
  system.SetFromTensorProduct(true, qA, qB);

  // Observe A; B collapses in solidarity
  var observed = qA.Observe(); 
  ```

Proper entanglement manager with tagging, locking, multi party agreement and guardrails against cross system linking.

### 4. Gate Abstraction and Circuit Scheduling

- `QuantumGate` matrices
- `QuantumGates` built ins: Root NOT, Hadamard, Pauli X, T, T dagger, RX, more
- `QuantumGateTools` for inversion and comparison
- Compose, invert, schedule, visualise

  ```csharp
  var system = new QuantumSystem();

  // Queue some gates
  system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
  system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

  // Visualise circuit before processing
  var schedule = system.VisualiseGateSchedule(totalQubits: 2);
  Console.WriteLine(schedule);

  // Process queue
  system.ProcessGateQueue();
  ```

Composition and inversion:

```csharp
var rootNot = new QuantumGate(QuantumGates.RootNot);
var doubleRoot = rootNot.Then(rootNot);
var pauliX = new QuantumGate(QuantumGates.PauliX);
var invertedT = QuantumGateTools.InvertGate(QuantumGates.T);
var tDagger = new QuantumGate(QuantumGates.T_Dagger);
```

### 5. Quantum Algorithms: QFT and Grover Search

- QFT on arbitrary qubit lists
- Grover search with pluggable oracle, multi controlled Z, diffusion operator

  ```csharp
  var system = new QuantumSystem();
  QuantumAlgorithms.QuantumFourierTransform(system, new[]{0,1,2});
  Func<int[], bool> oracle = bits => bits[0] == 1 && bits[1] == 0;
  QuantumAlgorithms.GroverSearch(system, new[]{0,1}, oracle);
  ```

Algorithms use the same gate primitives so circuits are inspectable not mysterious black boxes.

### 6. Generic Superposition Layer: `QuBit<T>` and `Eigenstates<T>`

- Weighted collections of possible values with complex amplitudes
- Preserve original keys while transforming derived values
- LINQ style `.Select`, `.Where`, `.SelectMany`
- `p_op` and `p_func` for conditional and functional transforms without collapse
- Weighted sampling (`SampleWeighted`, `MostProbable`)
- Collapse replay, mockable collapse, seeded randomness

  ```csharp
  var qubit = new QuBit<int>(new[] { 1, 2, 3 });
  var doubled = qubit.Select(x => x * 2);
  var sample = doubled.SampleWeighted();
  ```

Ask the multiverse which numbers are prime or which values divide a target without manual loops.

---

## QuantumRegister

High level abstraction treating one or more qubit indices as a coherent register inside a `QuantumSystem`. Value view over part of the global wavefunction.

Features:

- Construct from qubits or basis integer
- Construct from explicit amplitude vector
- Collapse only those qubits
- Integer decoding over arbitrary bit ranges
- Sorted index sets and collapse locking

  ```csharp
  var system = new QuantumSystem();
  var q0 = new QuBit<int>(system, new[]{0});
  var q1 = new QuBit<int>(system, new[]{1});
  var reg = new QuantumRegister(q0, q1);

  var encoded = QuantumRegister.FromInt(3, 2, system);

  var amps = new[]{ Complex.One/Math.Sqrt(2), Complex.One/Math.Sqrt(2) };
  var regAmp = QuantumRegister.FromAmplitudes(amps, system);

  int[] measured = reg.Collapse();
  int full = reg.GetValue();
  int low3 = reg.GetValue(0, 3);
  ```

See `docs/QuantumRegister.md` for details.

---

## Quick Start

### Installation

```bash
dotnet add package QuantumSuperposition
```

### Namespaces

Generic superpositions:

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
```

Physics style gates and systems:

```csharp
using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
```

---

## Documentation Map

Trailer above. Full movie lives in the docs:

- Functional Operations: `docs/FunctionalOps.md`
- Entanglement and Collapse: `docs/Entanglement.md`
- Complex Number Support: `docs/ComplexSupport.md`
- Logic Gates and QuantumSystem: `docs/LogicGates.md`
- Quantum Algorithms: `docs/QuantumAlgorithms.md`
- PhysicsQubit: `docs/PhysicsQubit.md`
- QuantumRegister: `docs/QuantumRegister.md`
- Canonical Quantum States: `docs/CanonicalStates.md`
- Gate Register Sugar: `docs/GateRegisterSugar.md`
- Gate Catalogue Extensions: `docs/GateCatalogue.md`
- Equality and AlmostEquals: `docs/Equality.md`

---

## Contributing

Bug in the multiverse or a new gate to summon? Open an issue, send a pull request, bring your own obscure quantum joke.

---

## License

Unlicense: do what you like short of blaming us if you open a portal.

---

## Contact

Questions, fan mail or a probability amplitude shaped like a duck: `support@findonsoftware.com`
