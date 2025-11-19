## Entanglement & Collapse Propagation

## Required Namespaces

Make sure to import the correct namespaces for these examples.

```csharp
using System.Numerics;
using QuantumSuperposition.Systems;
using QuantumSuperposition.Utilities;
using QuantumSuperposition.Entanglement;
using QuantumSuperposition.Core;
using QuantumSuperposition.QuantumSoup;
```

### Entangled Variable Linking
Entanglement links variables so the state of one directly affects the state of another. Think two variables that are "quantum married" with intertwined states.

#### Example
```csharp
var qubitA = new QuBit<int>(_system, new[] { 0 });
var qubitB = new QuBit<int>(_system, new[] { 1 });

_system.Entangle("BellPair_A", qubitA, qubitB);

// Confirm they are linked
Assert.NotNull(qubitA.EntanglementGroupId);
Assert.NotNull(qubitB.EntanglementGroupId);
Assert.AreEqual(qubitA.EntanglementGroupId.Value, qubitB.EntanglementGroupId.Value);

// Check the group registry
var allGroups = _manager.GetGroupsForReference(qubitA);
Assert.AreEqual(1, allGroups.Count);
```

### Collapse Propagation
Observing one member of an entangled group propagates collapse to the others so the group settles on a consistent state.

#### Example
```csharp
var qubitA = new QuBit<int>(_system, new[] { 0 });
var qubitB = new QuBit<int>(_system, new[] { 1 });

qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
qubitB.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
_system.Entangle("MyGroup", qubitA, qubitB);
_system.SetFromTensorProduct(true, qubitA, qubitB);
QuantumConfig.ForbidDefaultOnCollapse = false;

var observedA = qubitA.Observe();

Assert.IsTrue(qubitB.IsCollapsed);
Assert.IsTrue(qubitA.IsCollapsed);
Assert.NotNull(qubitB.GetObservedValue());
```

### Tensor Product Expansion
The tensor product of two qubits combines their states into a larger state space representing all combinations of their individual states.

#### Example
```csharp
var qubit1 = new QuBit<int>(new[] { (0, new Complex(1, 0)), (1, new Complex(1, 0)) });
var qubit2 = new QuBit<int>(new[] { (0, new Complex(2, 0)), (1, new Complex(0, 2)) });

var productDict = QuantumMathUtility<int>.TensorProduct(qubit1, qubit2);

Assert.AreEqual(4, productDict.Count);
var zeroZero = productDict.Keys.FirstOrDefault(arr => arr[0] == 0 && arr[1] == 0);
var amplitude00 = productDict[zeroZero];

Assert.AreEqual(new Complex(2, 0), amplitude00);
```

### Entangled Group Mutation Propagation
Mutating one qubit in an entangled group propagates the change so the group remains consistent.

#### Example
```csharp
var qubitA = new QuBit<int>(_system, new[] { 0 })
    .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalise: true);
var qubitB = new QuBit<int>(_system, new[] { 1 })
    .WithWeights(new Dictionary<int, Complex> { { 0, 2.0 }, { 1, 3.0 } }, autoNormalise: true);

_system.Entangle("MutationGroup", qubitA, qubitB);

qubitA = qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 5.0 }, { 1, 1.0 } }, autoNormalise: true);

Assert.IsFalse(qubitA.IsActuallyCollapsed);
Assert.IsFalse(qubitB.IsActuallyCollapsed);
Assert.AreEqual(qubitA.EntanglementGroupId, qubitB.EntanglementGroupId);
```

### Entanglement Group Versioning
Version groups to track changes over time and inspect history.

#### Example
```csharp
var qubitA = new QuBit<int>(_system, new[] { 0 });
var qubitB = new QuBit<int>(_system, new[] { 1 });
var qubitC = new QuBit<int>(_system, new[] { 2 });

var groupId = _manager.Link("FirstPair", qubitA, qubitB);

Assert.AreEqual("FirstPair", _manager.GetGroupLabel(groupId));
var groupMembers = _manager.GetGroup(groupId);
Assert.AreEqual(2, groupMembers.Count);
```

### Entanglement Guardrails
Prevent invalid operations such as linking qubits from different systems or linking a qubit to itself.

#### Example
```csharp
var secondSystem = new QuantumSystem();

var qubitInFirst = new QuBit<bool>(_system, new[] { 0 });
var qubitInSecond = new QuBit<bool>(secondSystem, new[] { 0 });

Assert.Throws<InvalidOperationException>(() =>
{
    _manager.Link("ContradictionGroup", qubitInFirst, qubitInSecond);
}, "Linking across timelines is forbidden.");

var qubit = new QuBit<int>(_system, new[] { 0 });

Assert.DoesNotThrow(() =>
{
    _manager.Link("SelfLink?", qubit);
});
```

### Multi Party Collapse Agreement
In multi party agreements all qubits in the group agree on the observed value when collapsed.

#### Example
```csharp
var qubitX = new QuBit<int>(_system, new[] { 0 })
    .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);
var qubitY = new QuBit<int>(_system, new[] { 1 })
    .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalise: true);

_system.Entangle("MultiPartyGroup", qubitX, qubitY);
_system.SetFromTensorProduct(true, qubitX, qubitY);

var valueX = qubitX.Observe(1234);
var valueY = qubitY.GetObservedValue();

Assert.NotNull(valueY);
Assert.IsTrue(qubitY.IsCollapsed);
```

### Entanglement Locking and Freezing
Lock a qubit during critical operations to keep state consistent.

#### Example
```csharp
var qubitA = new QuBit<int>(_system, new[] { 0 });
var groupId = _manager.Link("LockTestGroup", qubitA);

qubitA.Lock();

if (qubitA.IsLocked)
{
    Assert.Throws<InvalidOperationException>(() =>
    {
        qubitA.Append(42);
    });
}
else
{
    Assert.DoesNotThrow(() => qubitA.Append(42));
}
```

### Entanglement Group Tagging and Naming
Assign labels or tags for easier identification and management.

#### Example
```csharp
var qubitA = new QuBit<bool>(_system, new[] { 0 });
var qubitB = new QuBit<bool>(_system, new[] { 1 });

var groupId = _manager.Link("BellPair_A", qubitA, qubitB);

var label = _manager.GetGroupLabel(groupId);
Assert.AreEqual("BellPair_A", label);
```

### Partial Collapse Staging
Observe one qubit now and another later and keep observations consistent.

#### Example
```csharp
var qubit1 = new QuBit<bool>(_system, new[] { 0 })
    .WithWeights(new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } }, autoNormalise: true);
var qubit2 = new QuBit<bool>(_system, new[] { 1 })
    .WithWeights(new Dictionary<bool, Complex> { { false, 2.0 }, { true, 1.0 } }, autoNormalise: true);

_system.Entangle("PartialCollapse", qubit1, qubit2);
_system.SetFromTensorProduct(false, qubit1, qubit2);

var outcomeQ1 = _system.PartialObserve(new[] { 0 }, new Random(42));
bool observedQ1 = outcomeQ1[0] != 0;

Assert.True(qubit1.IsCollapsed);
Assert.AreEqual(observedQ1, (bool)qubit1.GetObservedValue());

Assert.False(qubit2.IsCollapsed);
Assert.IsNull(qubit2.GetObservedValue());

var observedQ2 = qubit2.Observe(100);
Assert.True(qubit2.IsCollapsed);
```

### Entanglement Graph Diagnostics
Diagnostics provide insights into the structure and state of entangled groups including sizes, circular references and other metrics.

#### Example
```csharp
var qA = new QuBit<int>(_system, new[] { 0 });
var qB = new QuBit<int>(_system, new[] { 1 });
var qC = new QuBit<int>(_system, new[] { 2 });

_manager.Link("GroupA", qA);
_manager.Link("GroupB", qB, qC);

Assert.DoesNotThrow(() =>
{
    _manager.PrintEntanglementStats();
});
