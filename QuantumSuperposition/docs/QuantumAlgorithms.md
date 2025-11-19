# Quantum Algorithms

QuantumSuperposition provides built in support for advanced quantum algorithms. This section covers the Quantum Fourier Transform (QFT) and Grover's Search Algorithm with examples mirroring internal tests.

---

## Required Namespaces

Make sure to import the appropriate namespaces before using these algorithms.

```csharp
using System;
using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Core;
```

---

## Quantum Fourier Transform (QFT)

The QFT is a core building block in many quantum algorithms. It reveals periodicity in a quantum state, turning time based signals into frequency like a quantum DJ mixing up the basis.

### Example: Applying QFT

```csharp
var system = new QuantumSystem();
int[] qubits = new[] { 0, 1, 2 };

QuantumAlgorithms.QuantumFourierTransform(system, qubits);
```

### What It Does

For 3 qubits the QFT:

- Applies a Hadamard to each qubit
- Applies controlled phase (CPhase) gates with decreasing angles
- Reverses qubit order using SWAPs

### Gate Breakdown

Expected gate sequence for 3 qubits:

1. H on qubit 0
2. CPhase(π/4) from qubit 1 -> 0
3. CPhase(π/8) from qubit 2 -> 0
4. H on qubit 1
5. CPhase(π/4) from qubit 2 -> 1
6. H on qubit 2
7. SWAP qubit 0 with qubit 2

### Notes

You can introspect the gate sequence with internal helpers if you need to debug or simulate the pipeline.

---

## Grover's Search Algorithm

Grover's algorithm finds a marked item in an unsorted quantum database quadratically faster than classical search.

### Example: Grover Search on 2 Qubits

```csharp
var system = new QuantumSystem();
int[] qubits = new[] { 0, 1 };

// Define the oracle to mark state [1, 0] with qubit 0 as MSB
Func<int[], bool> oracle = bits => bits[0] == 1 && bits[1] == 0;

QuantumAlgorithms.GroverSearch(system, qubits, oracle);
```

### Algorithm Steps

1. Initialisation: Hadamard gates create a uniform superposition
2. Oracle application: apply a -1 phase to the marked state(s)
3. Diffusion operator: reflect around the average amplitude

For 2 qubits the number of iterations is:
```
floor(π/4 × √(2^2)) = 1
```

### Diffusion Operator Details

```csharp
// Internally applied as part of GroverSearch:
foreach (var qubit in qubits)
    system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");

foreach (var qubit in qubits)
    system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");

system.ApplyMultiQubitGate(qubits, mcz, "MCZ");

foreach (var qubit in qubits)
    system.ApplySingleQubitGate(qubit, QuantumGates.PauliX, "X");

foreach (var qubit in qubits)
    system.ApplySingleQubitGate(qubit, QuantumGates.Hadamard, "H");
```

### Gate Highlights

- Oracle is a custom matrix gate that flips phase of matched state(s)
- MCZ is a multi controlled Z gate targeting |00...0⟩
- Pauli X gates surround the MCZ for inversion logic

---

## Helper Functions

```csharp
// MSB first bits
int[] bits = QuantumAlgorithms.IndexToBits(5, 4);  // [0, 1, 0, 1]

// Bits back to index
int index = QuantumAlgorithms.BitsToIndex(new[] { 0, 1, 0, 1 });  // 5

// Extract substate
int fullIndex = 10;  // 1010
int[] targetQubits = new[] { 0, 2 };
int extracted = QuantumAlgorithms.ExtractSubstate(fullIndex, targetQubits, totalQubits: 4);  // 3
```

---

## Summary

`QuantumAlgorithms` offers intuitive methods to:

- Apply the QFT
- Run Grover's Search with a custom oracle
- Work with multi qubit operations, controlled phases and reflection operators
- Extract and manipulate binary representations of quantum states

These high level functions hide matrix construction while keeping your quantum programming expressive and flexible.

```csharp
QuantumAlgorithms.QuantumFourierTransform(system, qubits);
QuantumAlgorithms.GroverSearch(system, qubits, oracle);
