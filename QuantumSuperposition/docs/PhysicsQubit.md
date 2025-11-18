# PhysicsQubit — Bloch Sphere Constructors and |0>, |1> Shortcuts

`PhysicsQubit` is a specialised `QuBit<int>` constrained to the computational basis {0, 1}. It provides convenient constructors to initialise amplitudes directly or via Bloch sphere parameters, and static shortcuts for the basis states.

- Amplitude constructor: `PhysicsQubit(Complex alpha, Complex beta)`
- Component constructor: `PhysicsQubit(double aRe, double aIm, double bRe, double bIm)`
- Bloch sphere constructor: `PhysicsQubit(double theta, double phi)` which maps
  |ψ> = cos(θ/2)|0> + e^{iφ} sin(θ/2)|1>
- Shortcuts: `PhysicsQubit.Zero`, `PhysicsQubit.One`

Internally, weights are set to `{ 0 -> alpha, 1 -> beta }` and normalised using the existing weight logic.

## Example

```csharp
using QuantumSuperposition.QuantumSoup;
using System.Numerics;

// Direct amplitudes
var q1 = new PhysicsQubit(new Complex(1, 0), Complex.Zero); // |0>

// From components
var q2 = new PhysicsQubit(0, 0, 1, 0); // |1>

// Bloch sphere (theta, phi)
var qs = new PhysicsQubit(Math.PI/2, 0); // (|0> + |1>)/sqrt(2)

// Shortcuts
var z = PhysicsQubit.Zero;
var o = PhysicsQubit.One;
```
