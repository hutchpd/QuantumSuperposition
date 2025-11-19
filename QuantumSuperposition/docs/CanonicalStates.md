# Canonical Quantum States

Built-in helpers for constructing common multi-qubit states directly on `QuantumRegister`.

## Factories

```csharp
public static QuantumRegister EPRPair(QuantumSystem system);
public static QuantumRegister WState(QuantumSystem system, int length = 3);
public static QuantumRegister GHZState(QuantumSystem system, int length = 3);
```

## EPR Pair (Bell State)
Creates a 2-qubit state: (|00> + |11>) / sqrt(2)

```csharp
var system = new QuantumSystem();
var bell = QuantumRegister.EPRPair(system);
```

## W State
Equal superposition of all basis states with Hamming weight 1.
For n qubits: (|100...0> + |010...0> + ... + |0...001>) / sqrt(n)

```csharp
var system = new QuantumSystem();
var w3 = QuantumRegister.WState(system); // length = 3
var w5 = QuantumRegister.WState(system, length: 5);
```

## GHZ State
n-qubit entangled state: (|00...0> + |11...1>) / sqrt(2)

```csharp
var system = new QuantumSystem();
var ghz = QuantumRegister.GHZState(system); // length = 3
var ghz4 = QuantumRegister.GHZState(system, length: 4);
```

## Entanglement Labels
Each factory links qubits into an entanglement group:
- `EPRPair_A`
- `WState_A`
- `GHZState_A`

Use `system.Entanglement` diagnostics to inspect linkage.

## Notes
- Length must be >= 2 for W and GHZ states.
- Factories overwrite current system amplitudes.
- Returned register spans indices [0..length-1] in creation order.
