# Equality and AlmostEquals

This library provides tolerant equality checks for registers and eigenstates.

## QuantumRegister.AlmostEquals

Compare two `QuantumRegister` instances by projecting the underlying system amplitudes onto each register's subspace and checking element-wise closeness within a tolerance.

```csharp
bool approx = regA.AlmostEquals(regB, tolerance: 1e-9);
bool exact = regA.Equals(regB); // zero-tolerance
```

Notes:
- Registers must span the same number of qubits.
- Amplitudes are aggregated in the order of indices as stored in each register.

## Eigenstates<int>.AlmostEquals

Compare eigenstates by probability mass per value, tolerant to small complex differences.

```csharp
var e1 = new Eigenstates<int>(new[]{1,2,3}).WithWeights(new(){ {1, 1.0}, {2, 1.0}, {3, 0.0} });
var e2 = new Eigenstates<int>(new[]{1,2,3}).WithWeights(new(){ {1, 1.0}, {2, 1.0}, {3, 1e-11} });
Assert.IsTrue(e1.AlmostEquals(e2, 1e-9));
```

The comparison:
- Compares the value sets for equality.
- Compares probability mass |amp|^2 per value within the tolerance.
