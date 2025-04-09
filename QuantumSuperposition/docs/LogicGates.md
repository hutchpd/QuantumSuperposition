# Quantum Gate Examples

## Required Namespaces

To get started, ensure you include the following namespaces:

```csharp
using System.Numerics;
using QuantumSuperposition.Core;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Systems;
```

## Quantum Gate Composition
Composing quantum gates applies them sequentially.

### Example: Root-NOT Twice Equals Pauli-X
```csharp
QuantumGate rootNot = new QuantumGate(QuantumGates.RootNot);
QuantumGate composed = rootNot.Then(rootNot);
QuantumGate pauliX = new QuantumGate(QuantumGates.PauliX);

// Verifying composition equivalence
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(composed.Matrix, pauliX.Matrix));
```

## Quantum Gate Inversion
Quantum gates can be inverted; applying inversion twice returns the original gate.

### Example: Hadamard Double Inversion
```csharp
QuantumGate hadamard = QuantumGates.Hadamard;
Complex[,] inverted = QuantumGateTools.InvertGate(hadamard.Matrix);
Complex[,] doubleInverted = QuantumGateTools.InvertGate(inverted);

// Checking double inversion
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(doubleInverted, hadamard.Matrix));
```

## Parametric Quantum Gates
Parametric gates allow rotation about a specific axis by an angle.

### Example: RX Gate Calculation
```csharp
double theta = Math.PI / 4;
QuantumGate rxGate = QuantumGates.RX(theta);

Complex[,] expectedMatrix = new Complex[,]
{
    { Math.Cos(theta / 2), -Complex.ImaginaryOne * Math.Sin(theta / 2) },
    { -Complex.ImaginaryOne * Math.Sin(theta / 2), Math.Cos(theta / 2) }
};

// Confirm RX gate calculation
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(rxGate.Matrix, expectedMatrix));
```

## Chained Quantum Gate Operations
Quantum gates can be chained in sequence for complex operations.

### Example: Hadamard → RX → Root-NOT Chain
```csharp
QuantumGate hadamard = QuantumGates.Hadamard;
QuantumGate rxGate = QuantumGates.RX(Math.PI / 2);
QuantumGate rootNot = new QuantumGate(QuantumGates.RootNot);

QuantumGate chainedGate = hadamard.Then(rxGate).Then(rootNot);

// Validate chained composition
QuantumGate expectedChain = hadamard.Then(rxGate).Then(rootNot);
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(chainedGate.Matrix, expectedChain.Matrix));
```

## Quantum Gate Queue Visualization
Visualizing quantum gates scheduled on the system.

### Example: Visualizing Gate Schedule
```csharp
QuantumSystem system = new QuantumSystem();
system.ApplySingleQubitGate(0, QuantumGates.Hadamard, "Hadamard");
system.ApplyTwoQubitGate(0, 1, QuantumGates.CNOT.Matrix, "CNOT");

// Visualization before processing
string schedule = system.VisualizeGateSchedule(totalQubits: 2);
Assert.IsNotEmpty(schedule);

// Process queue
system.ProcessGateQueue();

// Visualization after processing
string postSchedule = system.VisualizeGateSchedule(totalQubits: 2);
Assert.IsTrue(postSchedule.Contains("no operations") || string.IsNullOrWhiteSpace(postSchedule));
```

## Gate Inversion: T and T-Dagger Gates
Inverting T gate yields T-dagger.

### Example: T Gate Inversion
```csharp
QuantumGate tGate = new QuantumGate(QuantumGates.T);
Complex[,] invertedT = QuantumGateTools.InvertGate(tGate.Matrix);
QuantumGate tDagger = new QuantumGate(QuantumGates.T_Dagger);

// Confirming T-Dagger inversion
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(invertedT, tDagger.Matrix));
```

## Custom Quantum Gates
Create and use custom-defined quantum gates.

### Example: Custom Phase Gate
```csharp
double theta = Math.PI / 3;
Complex[,] customPhaseMatrix = new Complex[,]
{
    { 1, 0 },
    { 0, Complex.Exp(Complex.ImaginaryOne * theta) }
};
QuantumGate customPhaseGate = new QuantumGate(customPhaseMatrix);
QuantumGate identityGate = new QuantumGate(QuantumGates.Identity);
QuantumGate composedGate = customPhaseGate.Then(identityGate);

// Validate custom gate composition with identity
Assert.IsTrue(QuantumGateTools.AreMatricesEqual(composedGate.Matrix, customPhaseGate.Matrix));
```

These examples illustrate how to leverage the quantum gate functionalities provided by the QuantumSuperposition library effectively.

