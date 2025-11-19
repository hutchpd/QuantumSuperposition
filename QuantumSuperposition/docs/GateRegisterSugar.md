# Gate * Register Operator Sugar

Adds physics style application of gates directly to a `QuantumRegister` using the `*` operator.

```csharp
QuantumRegister reg = QuantumRegister.EPRPair(system);
reg = QuantumGates.Hadamard * reg; // apply H to each qubit if size == 1 or appropriate arity
```

## Behaviour

- Gate dimension inferred from matrix size (2^n)
- Gate arity must match `register.QubitIndices.Length`
- 1 qubit and 2 qubit gates are enqueued and processed through the existing queue (preserving visualisation tooling)
- Multi qubit (>2) gates applied immediately via `ApplyMultiQubitGate`
- Returned register is a new view referencing the same indices

## Examples

```csharp
var system = new QuantumSystem();
var reg = QuantumRegister.GHZState(system, length: 3);
var custom3QGate = new QuantumGate(new Complex[,] {
    {1,0,0,0,0,0,0,0},
    {0,1,0,0,0,0,0,0},
    {0,0,1,0,0,0,0,0},
    {0,0,0,1,0,0,0,0},
    {0,0,0,0,1,0,0,0},
    {0,0,0,0,0,1,0,0},
    {0,0,0,0,0,0,1,0},
    {0,0,0,0,0,0,0,1}
});
reg = custom3QGate * reg; // multi qubit application
```

## Notes
- Gate must be square and power of two dimension
- Throws if arity mismatch
- Does not auto normalise. Relies on existing system normalisation
