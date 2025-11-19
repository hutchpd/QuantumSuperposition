# Gate Catalogue Extensions

New gates and factories exposed via `QuantumGates` for parity with common algorithm needs.

## Added Gates / Factories

- Identity / `IdentityOfLength(n)`
- `HadamardOfLength(n)` (tensor power of H)
- Pauli-Y, Pauli-Z
- SWAP, SquareRootSwap
- Toffoli (CCNOT), Fredkin (CSWAP)
- `Phase(theta)` / `PhaseShift(theta)` alias
- `Controlled(inner)` generic controlled gate constructor
- `QuantumFourierTransformGate(registerLength)` builds full QFT unitary

## Examples

```csharp
// Multi-qubit identity
var id4 = QuantumGates.IdentityOfLength(4);

// 3-qubit Hadamard (H?H?H)
var h3 = QuantumGates.HadamardOfLength(3);

// Controlled arbitrary inner gate
var cz = QuantumGates.Controlled(QuantumGates.PauliZ);

// QFT gate for a 3-qubit register
var qft3 = QuantumGates.QuantumFourierTransformGate(3);

// Apply with sugar
var system = new QuantumSystem();
var reg = QuantumRegister.GHZState(system, 3);
reg = qft3 * reg; // multi-qubit gate application
```

## Notes
- `Controlled(inner)` constructs block matrix diag(I, inner). Caller handles qubit ordering.
- `QuantumFourierTransformGate` returns full NxN matrix (N = 2^registerLength) using canonical definition.
- Advanced optimised decompositions still available via algorithmic APIs; this factory surfaces monolithic unitary.
