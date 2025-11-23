# Basis mapping examples

This tiny page shows how to map non-primitive types into the computational basis when calling `QuantumSystem.SetFromTensorProduct`.

Keep in mind: the system's global wavefunction uses integer basis indices (0,1,2,...). If your domain type is not `int` or `bool` (or an `enum`), pass a `Func<T,int>` mapper that converts your value into the desired basis integer.

## Example: mapping a small struct to basis integers

Suppose you have a tiny struct representing two bits:

```csharp
public readonly struct BitPair
{
    public readonly int High; // 0 or 1
    public readonly int Low;  // 0 or 1
    public BitPair(int high, int low) { High = high; Low = low; }
}

// Build two local qubits of BitPair
var qx = new QuBit<BitPair>(new[] { new BitPair(0,0), new BitPair(0,1), new BitPair(1,0) });
var qy = new QuBit<BitPair>(new[] { new BitPair(0,0), new BitPair(1,1) });

// Mapper: convert BitPair to an integer (high<<1 | low)
int MapBitPair(BitPair bp) => (bp.High << 1) | bp.Low;

var system = new QuantumSystem();

// Use the mapper when creating the system wavefunction
system.SetFromTensorProduct(propagateCollapse: false, mapToBasis: MapBitPair, qx, qy);

// The system's Amplitudes keys are now integer arrays representing each mapped basis state.
foreach (var key in system.Amplitudes.Keys)
{
    // key is an int[] of mapped values (one per qubit)
    Console.WriteLine($"basis: [{string.Join(',', key)}]");
}
```

Notes
- The mapper is called for every element of each qubit's state when building the tensor product.
- Default mappers are provided for `int`, `bool`, and `enum` (enums use `Convert.ToInt32`).
- If you pass a custom mapper, make sure it maps to a small contiguous set of integers that suit your intended computational basis.

Have fun mapping tiny types to big quantum dreams.