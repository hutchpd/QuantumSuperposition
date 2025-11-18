# QuantumRegister

`QuantumRegister` is a lightweight value abstraction over one or more qubit indices inside a `QuantumSystem`.
It lets you treat a slice of the global wavefunction as a register you can:

- Construct from existing `QuBit<int>` (or `PhysicsQubit`) instances
- Construct from an integer value (basis state)
- Construct from an explicit complex amplitude vector
- Partially observe (collapse) just those qubits
- Decode integer values from bit ranges

## Constructors

```csharp
// From qubits already bound to a system
tvar system = new QuantumSystem();
var q0 = new QuBit<int>(system, new[] { 0 });
var q1 = new QuBit<int>(system, new[] { 1 });
var reg = new QuantumRegister(q0, q1);

// From integer value (populates amplitudes as a single basis state)
var regConst = QuantumRegister.FromInt(value: 3, bits: 2, system);

// From explicit amplitude vector (length must be power-of-two)
var regAmp = QuantumRegister.FromAmplitudes(new[] { Complex.One/Math.Sqrt(2), Complex.One/Math.Sqrt(2) }, system);
```

## Collapse

```csharp
// Collapses only this register's qubits
int[] measuredBits = reg.Collapse();
```

## Value Extraction

```csharp
// Decode full register value
int fullValue = reg.GetValue();
// Decode lower 3 bits (offset 0, length 3)
int low3 = reg.GetValue(offset: 0, length: 3);
// Decode bits [2..4)
int mid = reg.GetValue(offset: 2, length: 2);
```

Internally this uses `QuantumAlgorithms.BitsToIndex` and falls back to `PartialObserve` if the system has not yet fully collapsed.

## Notes

- Creating from amplitudes or integer value overwrites system amplitudes for the involved qubits.
- `Collapse()` locks underlying qubits to prevent accidental mutation after measurement.
- Supports disjoint index sets; indices are stored sorted.

