# Noise, error mitigation, and other forms of polite reality editing

QuantumSuperposition is happy to simulate an ideal universe.
It is also happy to simulate a universe where your gates are confident, slightly wrong, and occasionally lying about measurements.

This page covers the `QuantumSuperposition.NoiseProperties` and `QuantumSuperposition.Systems` additions:

- `NoiseModel`
- `NoisyQuantumSystem`
- Readout error mitigation (single qubit and multi-qubit)
- Zero Noise Extrapolation (ZNE)
- Probabilistic Error Cancellation (PEC)

## 1) `NoiseModel`: define the enemy

`NoiseModel` is the passive description of how things go wrong.
You can use it, ignore it, or keep it around as a reminder that physics has a sense of humor.

Available knobs:

- `SingleQubitErrorRate`: probability of injecting a single-qubit error after a single-qubit gate
- `TwoQubitErrorRate`: probability of injecting an error after a two-qubit gate
- `ReadoutErrorMatrix`: a 2x2 confusion matrix for measurement assignment errors
- `ThermalRelaxation`: optional `T1` and `T2` time constants

## 2) `NoisyQuantumSystem`: a decorator that misbehaves on purpose

Instead of baking noise into `QuantumSystem`, you can opt in via `NoisyQuantumSystem`.
It wraps an internal `QuantumSystem` and injects probabilistic errors when you apply gates.

```csharp
using QuantumSuperposition.Core;
using QuantumSuperposition.NoiseProperties;
using QuantumSuperposition.Systems;

var noise = new NoiseModel(
    singleQubitErrorRate: 0.01,
    twoQubitErrorRate: 0.05,
    readoutErrorMatrix: ReadoutErrorMatrix.FromFlipProbabilities(p01: 0.02, p10: 0.03));

var noisy = new NoisyQuantumSystem(noise);

// Apply a gate to qubit 0.
// With probability p it queues an X or Z right after.
noisy.Apply(QuantumGates.Hadamard, qubitIndex: 0);

// Process scheduled gates.
noisy.ProcessGateQueue();
```

Measurement helpers:

- `NoisyQuantumSystem.ObserveGlobal(...)` and `PartialObserve(...)` return a noisy readout using `ReadoutErrorMatrix`.
- The underlying collapse still happens in the inner system.
  The noise is in the reported classical bits, which is exactly the kind of betrayal real hardware specializes in.

## 3) Readout error mitigation

Readout noise is the gateway drug of error mitigation.
It is simple, visible, and it teaches you to stop trusting your measurements without getting into the deep end of channel inverses.

### Single qubit: `ReadoutMitigator`

`ReadoutMitigator.Calibrate(...)` prepares `|0⟩` and `|1⟩` and estimates the confusion matrix.
`ReadoutMitigator.Mitigate(...)` applies the inverse to measurement counts.

The current `Mitigate(...)` API supports single-qubit counts keyed by:

- `"0"`
- `"1"`

### Multi-qubit: `TensoredReadoutMitigator`

`TensoredReadoutMitigator` calibrates each qubit independently, then applies the tensored inverse without materializing a giant matrix.

Supported multi-qubit count dictionaries:

- string keys like `"00"`, `"01"`, `"10"`, `"11"`
- `Dictionary<int[], int>` keyed by bit arrays, using `IntArrayComparer`

It returns a `MitigationResult` so you can compare raw vs mitigated frequencies and quantify how much your reality shifted.

## 4) Zero Noise Extrapolation (ZNE)

ZNE is the visual one.
You crank the noise up on purpose, watch the results get worse, then extrapolate back to a noise scale of 0.

In this library, ZNE is implemented by scaling the parameters in `NoiseModel`.

```csharp
using QuantumSuperposition.NoiseProperties;

var zne = new ZeroNoiseExtrapolation();

var circuit = new ZneCircuit(
    BaseNoiseModel: new NoiseModel(0.02, 0.05),
    Run: sys =>
    {
        // Build and run your circuit using sys.
        // Return a scalar observable.
        return 0.123;
    });

double estimate = zne.Execute(
    circuit,
    noiseScales: new[] { 1.0, 2.0, 3.0 },
    extrapolation: ExtrapolationType.Polynomial);
```

## 5) Probabilistic Error Cancellation (PEC)

PEC is the brain-melting one.
You represent an ideal value as a linear combination of experiments that you can actually run.
The coefficients can be negative, which is the math equivalent of the universe rolling its eyes.

`PECSampler` takes a quasi-probability decomposition and performs Monte Carlo sampling:

- sample term `k` with probability proportional to `|Coefficient_k|`
- run that noisy reality
- re-weight the result so the average converges to the intended ideal value

PEC comes with overhead. The sampler reports the overhead `γ = Σ |η_k|`.
Higher overhead means more variance, which means you buy accuracy with more samples.

If you are reading this section for fun, you are exactly the target audience for this library.
