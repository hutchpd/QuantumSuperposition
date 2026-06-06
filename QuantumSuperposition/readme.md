# QuantumSuperposition (.NET Library) - v1.9

[![NuGet](https://img.shields.io/nuget/v/QuantumSuperposition.svg)](https://www.nuget.org/packages/QuantumSuperposition)
![Quantum Algorithms Inside](https://img.shields.io/badge/quantum--algorithms-included-blueviolet)
[![Docs](https://img.shields.io/badge/docs-quantumsuperposition.hashnode.space-blue)](https://quantumsuperposition.hashnode.space)

> .NET's most confident way to say "maybe".

`QuantumSuperposition` is a .NET library for values that refuse to become one thing before the mathematics has finished with them.

It gives you two related machines:

1. A **generic superposition engine** for `QuBit<T>` and `Eigenstates<T>`, where ordinary typed values can exist in multiple weighted states and still be transformed, filtered, compared, sampled, and reasoned about.
2. A **physics-style quantum simulation layer** with `QuantumSystem`, `QuantumGate`, `QuantumRegister`, entanglement, gate scheduling, QFT, Grover search, noisy readout, mitigation, and enough complex amplitudes to make certainty look provincial.

This release is aligned with `PositronicVariables` v1.9 so the shared `QuBit<T>` model, package metadata, and documentation remain in step. No one wants two timelines disagreeing about the shape of uncertainty.

---

## What this library is

At the generic level, `QuantumSuperposition` lets you write code like this:

```csharp
var q = new QuBit<int>(new[] { 1, 2, 3 });

var doubled = q.Select(x => x * 2);
var possibleEvens = doubled.Where(x => x % 2 == 0);

var observed = possibleEvens.Observe();
```

The important part is that `q` is not an unknown integer. It is not a nullable value wearing a philosophical hat. It is a structured collection of possible `int` values, each with its own weight or amplitude.

You may transform the state without observing it.

You may combine it with other states.

You may defer collapse until the programme genuinely needs a single answer and not merely the comfort of one.

At the physics level, the same library also models actual multi-qubit state vectors, complex amplitudes, gate matrices, tensor products, entanglement groups, partial observation, and quantum algorithms.

So the library begins with typed ambiguity and ends, if provoked, in Hilbert space.

---

## Installation

```bash
dotnet add package QuantumSuperposition
```

---

## Namespaces

For generic typed superpositions:

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Operators;
```

For physics-style systems, gates, and complex amplitudes:

```csharp
using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
```

---

## The two layers

## 1. Generic superpositions

`QuBit<T>` and `Eigenstates<T>` are the general-purpose uncertainty layer.

They are useful when you want to model several possible typed values at once and perform ordinary-looking C# operations across all of them.

Features include:

* `QuBit<T>` and `Eigenstates<T>`
* Weighted and unweighted possible values
* Complex amplitude weighting
* Arithmetic across possible values
* Comparisons and conditional operations
* LINQ-style `.Select`, `.Where`, and `.SelectMany`
* Functional transforms through `p_func` and `p_op`
* Non-observational transformations
* Weighted sampling with `SampleWeighted`
* Most-probable value selection
* Collapse replay and seeded randomness
* Mockable collapse behaviour for deterministic tests

Example:

```csharp
var values = new QuBit<int>(new[] { 1, 2, 3 });

var doubled = values.Select(x => x * 2);
var sample = doubled.SampleWeighted();
```

This does not ask one value to win too early. It lets the possible values continue through the computation until observation becomes necessary.

A classical value is very proud of being one thing.

`QuBit<T>` is less easily impressed.

---

## 2. Physics-style quantum simulation

The physics layer models quantum systems as complex state vectors and gate matrices.

This includes:

* `PhysicsQubit`
* `QuantumSystem`
* `QuantumRegister`
* `QuantumGate`
* `QuantumGates`
* `QuantumAlgorithms`
* Real tensor product expansion
* Complex amplitude propagation
* Partial observation
* Entanglement groups
* Collapse propagation
* Gate scheduling
* Circuit visualisation
* QFT and Grover search
* Noise models and error mitigation

A minimal two-qubit system:

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;

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

The global wavefunction is built from the tensor product of the participating qubits. It is not a list multiplication wearing a costume. It is the full joint state space, where every basis combination gets its own amplitude and every extra qubit asks the room to grow exponentially.

---

## Complex amplitudes and tensor products

`QuBit<T>` supports complex amplitudes for weight-aware equality, sampling, arithmetic, and physics-style simulation.

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
using QuantumSuperposition.Utilities;

var q1 = new QuBit<int>(new[] { 0, 1 });
var q2 = new QuBit<int>(new[] { 0, 1 });

var joint = QuantumMathUtility<int>.TensorProduct(q1, q2);
```

`TensorProduct` builds a proper multi-qubit joint state over basis indices. If each qubit has two basis states, two qubits produce four joint basis states. Three produce eight. After that the numbers stop being friendly and start becoming architecture.

`PhysicsQubit` provides a specialised `QuBit<int>` constrained to the computational basis `{0, 1}`. It includes direct amplitude constructors, Bloch sphere constructors, and `Zero` / `One` helpers.

```csharp
var zero = PhysicsQubit.Zero;
var one = PhysicsQubit.One;

var balanced = new PhysicsQubit(
    theta: Math.PI / 2,
    phi: 0);
```

The physics wing likes its basis states tidy.

---

## QuantumSystem

`QuantumSystem` is the central state manager for multi-qubit simulation.

It can:

* Track qubits by index
* Build global amplitudes from tensor products
* Partially observe selected qubit subsets
* Propagate collapse through entanglement groups
* Queue and process gates
* Lock or freeze sensitive relationships
* Emit diagnostics for gate execution and collapse events

Example:

```csharp
var system = new QuantumSystem();

system.SetFromTensorProduct(
    propagateCollapse: true,
    new QuBit<int>(system, new[] { 0 }),
    new QuBit<int>(system, new[] { 1 }),
    new QuBit<int>(system, new[] { 2 }));

var outcome = system.PartialObserve(new[] { 0, 2 });
```

`PartialObserve` lets you collapse only the qubits you asked to inspect. The rest of the global state is adjusted consistently rather than interrogated out of impatience.

### Basis mapping

The global wavefunction uses integer basis coordinates. If your domain type is already `int`, `bool`, or an enum, default mappings are provided.

For richer domain types, pass a mapper:

```csharp
int MapBitPair(BitPair value) => (value.High << 1) | value.Low;

system.SetFromTensorProduct(
    propagateCollapse: true,
    mapToBasis: MapBitPair,
    qx,
    qy);
```

This is the practical indignity at the end of abstraction: eventually your lovely `T` must become an index.

That is not reduction. It is addressability.

### Registration and collapse propagation

Earlier versions had a subtle edge case where `SetFromTensorProduct` could create temporary local `QuBit<T>` objects that were not properly registered with the owning `QuantumSystem`. That meant collapse propagation, partial observation, and entanglement bookkeeping could drift out of agreement.

That has been corrected. Local qubits passed into `SetFromTensorProduct` are promoted to system-managed qubits and registered with their indices and entanglement wiring intact.

The paperwork now follows the particle.

---

## Entanglement

Entanglement links qubits so that observing or mutating one member can affect the rest of the group in a controlled, consistent way.

```csharp
var system = new QuantumSystem();

var qA = new QuBit<int>(system, new[] { 0 });
var qB = new QuBit<int>(system, new[] { 1 });

qA.WithWeights(new Dictionary<int, Complex>
{
    { 0, 1.0 },
    { 1, 1.0 }
}, autoNormalise: true);

qB.WithWeights(new Dictionary<int, Complex>
{
    { 0, 1.0 },
    { 1, 1.0 }
}, autoNormalise: true);

system.Entangle("BellPair_A", qA, qB);
system.SetFromTensorProduct(true, qA, qB);

var observed = qA.Observe();
```

The entanglement engine supports:

* Group labels
* Version tracking
* Collapse propagation
* Multi-party agreement
* Partial collapse staging
* Locking and freezing
* Diagnostics
* Guardrails against cross-system linking

Entanglement is not romance. It is the abolition of separate bookkeeping.

The library treats it accordingly.

---

## Gates and circuit scheduling

The gate layer models quantum gates as complex matrices.

It includes:

* `QuantumGate`
* `QuantumGates`
* `QuantumGateTools`
* Gate composition via `Then`
* Gate inversion
* Matrix equality checks
* Single-qubit gates
* Two-qubit gates
* Multi-qubit gates
* Gate queues
* ASCII circuit visualisation

Example:

```csharp
var system = new QuantumSystem();

system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "H");
system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

var schedule = system.VisualiseGateSchedule(totalQubits: 2);
Console.WriteLine(schedule);

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

Built-in gates and factories include:

* Identity
* Hadamard
* Pauli X, Y, Z
* Root NOT
* T and T dagger
* RX
* SWAP
* Square Root SWAP
* Toffoli
* Fredkin
* Phase
* Controlled gates
* Quantum Fourier Transform gates

Gates are where possibility stops drifting and starts being operated on.

---

## Quantum algorithms

`QuantumAlgorithms` provides higher-level routines built from the same gate primitives used elsewhere in the library.

Supported algorithms include:

* Quantum Fourier Transform
* Grover search

Example:

```csharp
var system = new QuantumSystem();

QuantumAlgorithms.QuantumFourierTransform(
    system,
    new[] { 0, 1, 2 });

Func<int[], bool> oracle = bits =>
    bits[0] == 1 && bits[1] == 0;

QuantumAlgorithms.GroverSearch(
    system,
    new[] { 0, 1 },
    oracle);
```

The algorithms are not black boxes. They are assembled from gates, so the circuits remain inspectable.

Grover does not discover the answer through inspiration. It bullies amplitude into consensus.

---

## QuantumRegister

`QuantumRegister` is a higher-level view over one or more qubit indices inside a `QuantumSystem`.

It lets you treat part of the global wavefunction as a coherent register.

Features include:

* Construct from existing qubits
* Construct from an integer basis value
* Construct from an explicit amplitude vector
* Collapse only the register's qubits
* Decode integer values from bit ranges
* Keep index sets sorted
* Lock collapsed qubits against accidental mutation

Example:

```csharp
var system = new QuantumSystem();

var q0 = new QuBit<int>(system, new[] { 0 });
var q1 = new QuBit<int>(system, new[] { 1 });

var reg = new QuantumRegister(q0, q1);

var encoded = QuantumRegister.FromInt(
    value: 3,
    bits: 2,
    system);

var amps = new[]
{
    Complex.One / Math.Sqrt(2),
    Complex.One / Math.Sqrt(2)
};

var regAmp = QuantumRegister.FromAmplitudes(amps, system);

int[] measured = reg.Collapse();
int full = reg.GetValue();
int low3 = reg.GetValue(0, 3);
```

A register is a local view of a global condition.

This is useful in much the same way a map is useful: it is not the territory, but it keeps you from having to perceive the entire territory at once.

See `docs/QuantumRegister.md`.

---

## Phase and interference

The physics layer is a linear algebra quantum simulator.

Gates are complex-valued unitary matrices.

States are complex-valued state vectors.

Phase is represented by the complex phase of amplitudes, and interference appears when amplitudes are multiplied and summed during gate application.

Concretely:

* Gates such as `Phase(θ)`, `CPhase(θ)`, `T`, and `RX(θ)` use complex matrix entries.
* Gate application is matrix-vector multiplication over complex numbers.
* `QuantumMathUtility.ApplyMatrix` and related helpers perform the complex multiplication and addition that produce interference.
* Measurement probabilities are derived as `|amplitude|²`.
* The system normalises amplitudes after gate batches so probabilities remain normalised.

Global phase is not observable. Relative phase is where the future hides its machinery.

Two states can have the same visible probabilities and still behave differently after later gates. That is not decoration. That is the point.

If you want a direct demonstration, see the Hadamard, Phase, Hadamard example in `docs/ComplexSupport.md`.

---

## Noise and error mitigation

The ideal simulator lives in a clean mathematical universe.

`NoisyQuantumSystem` is for the universe where the equipment has opinions.

Noise support includes:

* `NoiseModel`
* `NoisyQuantumSystem`
* Single-qubit error rates
* Two-qubit error rates
* Readout error matrices
* Thermal relaxation parameters
* Single-qubit readout mitigation
* Tensored multi-qubit readout mitigation
* Zero Noise Extrapolation
* Probabilistic Error Cancellation

Example:

```csharp
var noise = new NoiseModel(
    singleQubitErrorRate: 0.01,
    twoQubitErrorRate: 0.05,
    readoutErrorMatrix: ReadoutErrorMatrix.FromFlipProbabilities(
        p01: 0.02,
        p10: 0.03));

var noisy = new NoisyQuantumSystem(noise);

noisy.Apply(QuantumGates.Hadamard, qubitIndex: 0);
noisy.ProcessGateQueue();
```

The underlying state may be clean. The reported classical bits may not be.

This is not betrayal.

This is engineering.

See `docs/Noise.md`.

---

## Performance and diagnostics

Recent versions include several performance and diagnostics improvements:

* Multi-qubit gate application avoids `string.Join` grouping per basis state.
* Structural pattern arrays with sentinels reduce transient allocations.
* Functional transforms such as `Select` and `SelectMany` use hand-rolled iterators to reduce LINQ allocation overhead.
* Entanglement collapse propagation works through `IQuantumReference` rather than hard-coded qubit generic types.
* `QuantumSystem.GateExecuted` and `QuantumSystem.GlobalCollapse` provide lightweight diagnostics hooks.

In short: the multiverse now allocates fewer tiny regrets.

---

## Documentation map

The README is the guided tour. The docs are the labelled doors.

* Functional Operations: `docs/FunctionalOps.md`
* Entanglement and Collapse: `docs/Entanglement.md`
* Complex Number Support: `docs/ComplexSupport.md`
* Basis Mapping: `docs/BasisMapping.md`
* Logic Gates and QuantumSystem: `docs/LogicGates.md`
* Quantum Algorithms: `docs/QuantumAlgorithms.md`
* PhysicsQubit: `docs/PhysicsQubit.md`
* QuantumRegister: `docs/QuantumRegister.md`
* Canonical Quantum States: `docs/CanonicalStates.md`
* Gate Register Sugar: `docs/GateRegisterSugar.md`
* Gate Catalogue Extensions: `docs/GateCatalogue.md`
* Equality and AlmostEquals: `docs/Equality.md`
* Noise and Error Mitigation: `docs/Noise.md`

---

## Contributing

Found a state inconsistency, a gate bug, a collapse problem, or an especially elegant way to make the universe smaller?

Open an issue or send a pull request.

Please include:

* a minimal reproduction;
* expected behaviour;
* actual behaviour;
* whether observation was involved;
* whether the bug disappears when reality is not being looked at directly.

---

## License

Unlicense.

Use it, modify it, publish it, teach with it, dismantle it for parts, or place it in a sealed box and infer its condition from the screams.

No warranty is provided.

---

## Contact

Questions, bugs, fan mail, or probability amplitudes with suspicious posture:

`support@findonsoftware.com`
