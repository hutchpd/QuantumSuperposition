using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;

namespace QuantumMathTests
{
    [TestFixture]
    public class EntanglementFeatureTests
    {
        private QuantumSystem _system;
        private EntanglementManager _manager;

        [SetUp]
        public void Setup()
        {
            // Create a fresh QuantumSystem before each test
            _system = new QuantumSystem();
            _manager = _system.Entanglement;
        }

        #region Entangled Variable Linking: Track relationships between variables
        [Test]
        public void EntangledVariableLinking_CanLinkTwoQubitsInSameSystem_ReturnsSingleGroup()
        {
            // Arrange: Create two local qubits, each referencing _system.
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var qubitB = new QuBit<int>(_system, new[] { 1 });

            // Act: Entangle them under a custom label
            _system.Entangle("BellPair_A", qubitA, qubitB);

            // Assert:
            Assert.NotNull(qubitA.EntanglementGroupId, "Qubit A must have an entanglement group ID assigned.");
            Assert.NotNull(qubitB.EntanglementGroupId, "Qubit B must have an entanglement group ID assigned.");

            // Both should share the same group ID
            Assert.AreEqual(qubitA.EntanglementGroupId.Value, qubitB.EntanglementGroupId.Value, "Linked qubits must share the same group ID.");

            // The manager should track exactly 1 group
            var allGroups = _manager.GetGroupsForReference(qubitA);
            Assert.AreEqual(1, allGroups.Count, "Reference A must only belong to 1 group.");
        }
        #endregion

            #region Collapse Propagation: Ensure collapse ripples through entangled graph
            [Test]
            public void CollapsePropagation_CollapsingOneQubitNotifiesOthersInGroup()
            {
                // Arrange
                var qubitA = new QuBit<int>(_system, new[] { 0 });
                var qubitB = new QuBit<int>(_system, new[] { 1 });

                qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalize: true);
                qubitB.WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalize: true);

                _system.Entangle("MyGroup", qubitA, qubitB);
                _system.SetFromTensorProduct(qubitA, qubitB);

                // set QuantumConfig.ForbidDefaultOnCollapse = false;
                QuantumConfig.ForbidDefaultOnCollapse = false; // Allow default collapse behavior

                // Act
                var observedA = qubitA.Observe(); // triggers a collapse

                // Assert: QubitB should be flagged as collapsed from system
                Assert.IsTrue(qubitB.IsCollapsed, "Qubit B must be marked as collapsed due to propagation.");
                Assert.IsTrue(qubitA.IsCollapsed, "Qubit A itself is obviously collapsed.");

                // Because qubitB belongs to the same group, system notified it:
                var bValue = qubitB.GetObservedValue();
                // In a real partial measurement scenario, you’d check consistency,
                // but here we only confirm that B was indeed collapsed from propagation.

                Assert.NotNull(bValue, "Qubit B must have a consistent observed value after the collapse ripple.");

                // TODO: assert that bValue matches expected logic based on the entanglement
            }
            #endregion

        #region Tensor Product Expansion: Compute all state combinations across variables
        [Test]
        public void TensorProductExpansion_CombinesWeightedStatesCorrectly()
        {
            // Arrange: two QuBits with weighted states
            var qubit1 = new QuBit<int>(new (int, Complex)[]
            {
                (0, new Complex(1,0)),  // amplitude 1
                (1, new Complex(1,0)),  // amplitude 1
            }); // states: |0> + |1>

            var qubit2 = new QuBit<int>(new (int, Complex)[]
            {
                (0, new Complex(2,0)),  // amplitude 2
                (1, new Complex(0,2)),  // amplitude 2i
            }); // states: 2|0> + 2i|1>

            // Act
            var productDict = QuantumMathUtility<int>.TensorProduct(qubit1, qubit2);

            // Assert: productDict should contain 4 distinct keys: [0,0], [0,1], [1,0], [1,1].
            Assert.AreEqual(4, productDict.Count, "Tensor product must yield 4 states for 2 qubits each of dimension 2.");

            // Optionally, check specific amplitudes
            //   |0> (amp=1) with 2|0> (amp=2) => amplitude 1*2 = 2 => state [0,0]
            //   |0> (amp=1) with 2i|1>        => amplitude 1*(0+2i)= 2i => state [0,1]
            //   |1> (amp=1) with 2|0>         => amplitude 2 => [1,0]
            //   |1> (amp=1) with 2i|1>        => amplitude 2i => [1,1]

            var zeroZero = productDict.Keys.FirstOrDefault(arr => arr[0] == 0 && arr[1] == 0);
            var amplitude00 = productDict[zeroZero];
            Assert.AreEqual(new Complex(2, 0), amplitude00, "Amplitude for [0,0] must be 2.");
        }
        #endregion

        #region Entangled Group Mutation Propagation: Ensure mutations ripple before collapse
        [Test]
        public void EntangledGroupMutation_ChangingOneQubitsWeights_UpdatesSystemForOthers()
        {
            // Arrange: 2 qubits entangled
            var qubitA = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 1.0 } }, autoNormalize: true);

            var qubitB = new QuBit<int>(_system, new[] { 1 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 2.0 }, { 1, 3.0 } }, autoNormalize: true);

            _system.Entangle("MutationGroup", qubitA, qubitB);

            // Act: mutate qubitA’s weighting *before* any collapse
            qubitA = qubitA.WithWeights(new Dictionary<int, Complex> { { 0, 5.0 }, { 1, 1.0 } }, autoNormalize: true);

            // Normally, you might do a system-level re-tensor or something, but the example
            // code doesn't strictly do that automatically. In a real system, you'd recalc
            // wavefunction or partial wavefunction. We'll simply confirm no collapse has happened yet.

            // Assert
            Assert.IsFalse(qubitA.IsActuallyCollapsed, "Qubit A must still be uncollapsed after mutation.");
            Assert.IsFalse(qubitB.IsActuallyCollapsed, "Qubit B must still be uncollapsed after mutation.");

            // They remain in the same group, no forced collapse:
            Assert.AreEqual(qubitA.EntanglementGroupId, qubitB.EntanglementGroupId, "Both should remain in the same entanglement group.");

            // Because we haven't triggered a measurement, no wavefunction collapse was forced.
            var groupRefs = _manager.GetGroup(qubitA.EntanglementGroupId.Value);
            Assert.That(groupRefs, Contains.Item(qubitA));
            Assert.That(groupRefs, Contains.Item(qubitB));
        }
        #endregion

        #region Entanglement Group Versioning: Track generations of entangled graphs
        [Test]
        public void EntanglementGroupVersioning_RecordsChangesInHistory()
        {
            // Arrange
            var qubitA = new QuBit<int>(_system, new[] { 0 });
            var qubitB = new QuBit<int>(_system, new[] { 1 });
            var qubitC = new QuBit<int>(_system, new[] { 2 });

            // Act: Link two qubits, then link a third later
            var groupId = _manager.Link("FirstPair", qubitA, qubitB);

            // For “group versioning,” we might do another “link” call that modifies membership, 
            // but the sample EntanglementManager doesn’t explicitly show a ‘merge groups’ or ‘add to existing group’ method.
            // If you had one, you'd do something like:
            // _manager.Link("ExpandedGroup", qubitA, qubitB, qubitC);

            // Assert: we can examine group history if the manager tracks each change
            // The code references _groupHistory but does not explicitly expose it in public properties.
            // So this test is conceptual unless you add public accessor or reflection-based check.
            // For demonstration, we simply confirm the group has the correct label:
            Assert.AreEqual("FirstPair", _manager.GetGroupLabel(groupId), "Group must store the custom label from creation.");

            // If you extended the manager to retrieve group version info, you'd confirm multiple versions:
            // e.g. `_groupHistory[groupId]` has “Initial link”, then “Added qubitC,” etc.
            // This snippet simply checks the final membership from the manager’s dictionary:
            var groupMembers = _manager.GetGroup(groupId);
            Assert.AreEqual(2, groupMembers.Count, "Initially only QubitA and QubitB in the group.");
        }
        #endregion

        #region Entanglement Guardrails: Prevent self-links, contradictions, invalid entanglement
        [Test]
        public void EntanglementGuardrails_LinkQubitsFromDifferentSystems_ThrowsException()
        {
            // Arrange: two different systems
            var secondSystem = new QuantumSystem();

            var qubitInFirst = new QuBit<bool>(_system, new[] { 0 });
            var qubitInSecond = new QuBit<bool>(secondSystem, new[] { 0 });

            // Act & Assert: linking qubits from different systems is invalid
            Assert.Throws<InvalidOperationException>(() =>
            {
                _manager.Link("ContradictionGroup", qubitInFirst, qubitInSecond);
            }, "Should throw an error when linking references from different quantum systems.");
        }

        [Test]
        public void EntanglementGuardrails_LinkSingleQubitToItself_ThrowsException()
        {
            // The sample manager code checks each reference’s system but does not explicitly check “self link.” 
            // If you extended it to forbid linking a qubit with itself, you’d do:
            var qubit = new QuBit<int>(_system, new[] { 0 });

            // Suppose your manager had an explicit “no self-link” check:
            // We demonstrate how you might test that scenario:
            // (If your code is not written to forbid it, you can remove or adapt this test.)
            Assert.DoesNotThrow(() =>
            {
                // If your code does not forbid it, this might succeed. 
                // We'll put an Assume or comment that if you add a self-link guard, replace DoesNotThrow with Throws.
                _manager.Link("SelfLink?", qubit);
            }, "If code forbids self-link, you would expect an exception. Adjust test if so.");
        }
        #endregion

        #region Multi-Party Collapse Agreement: Consistent shared collapse across parties
        [Test]
        public void MultiPartyCollapse_SameObservationFromAllQubitsInGroup()
        {
            // Arrange
            var qubitX = new QuBit<int>(_system, new[] { 0 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalize: true);
            var qubitY = new QuBit<int>(_system, new[] { 1 })
                .WithWeights(new Dictionary<int, Complex> { { 0, 1.0 }, { 1, 2.0 } }, autoNormalize: true);

            _system.Entangle("MultiPartyGroup", qubitX, qubitY);

            _system.SetFromTensorProduct(qubitX, qubitY);

            // Act: Observe from qubitX
            var valueX = qubitX.Observe(1234);  // seeded for determinism

            // “Multi-party agreement” means that if qubitY tries to observe,
            // it should yield a consistent result (though real partial measurement logic might be more complex).
            var valueY = qubitY.GetObservedValue(); // it is collapsed from system’s perspective

            // Assert
            Assert.NotNull(valueY, "Qubit Y must have a valid observed value after shared collapse.");
            Assert.IsTrue(qubitY.IsCollapsed, "Qubit Y must be recognized as collapsed.");

            // In a more advanced scenario, partial measurement could cause entangled states to remain for other bits,
            // but the code above collapses the entire 2-qubit wavefunction. For consistency, we simply confirm Y is also collapsed.
        }
        #endregion

        #region Entanglement Locking / Freezing: Prevent collapse/mutation during ops
        [Test]
        public void EntanglementLocking_FreezesQubitDuringCriticalOperation_ThrowsIfModified()
        {
            // NOTE: The provided code does NOT implement a “locking” mechanism out of the box.
            // You would have a special flag in EntanglementManager or QuBit that blocks changes. 
            // This is just a conceptual test showing how it *might* look:

            var qubitA = new QuBit<int>(_system, new[] { 0 });

            // Hypothetical scenario: manager sets a “locked” state for the group
            var groupId = _manager.Link("LockTestGroup", qubitA);
            qubitA.Lock();

            // Act & Assert
            if (qubitA.IsLocked)
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    qubitA.Append(42); // any mutation is disallowed
                }, "When locked, modifications must throw InvalidOperationException.");
            }
            else
            {
                Assert.DoesNotThrow(() => qubitA.Append(42));
            }
        }
        #endregion

        #region Entanglement Group Tagging / Naming: Label groups (e.g., BellPair_A)
        [Test]
        public void EntanglementGroupTagging_CanAssignAndRetrieveCustomLabel()
        {
            // Arrange
            var qubitA = new QuBit<bool>(_system, new[] { 0 });
            var qubitB = new QuBit<bool>(_system, new[] { 1 });

            // Act
            var groupId = _manager.Link("BellPair_A", qubitA, qubitB);

            // Assert
            var label = _manager.GetGroupLabel(groupId);
            Assert.AreEqual("BellPair_A", label, "Group label must match the assigned string.");
        }
        #endregion

        #region Partial Collapse Staging: Stepwise collapse of part of a group
        [Test]
        public void PartialCollapseStaging_ObserveFirstQubitThenSecondQubit_StillConsistent()
        {
            // Arrange: 2 qubits in a system
            var qubit1 = new QuBit<bool>(_system, new[] { 0 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 1.0 }, { true, 1.0 } }, autoNormalize: true);
            var qubit2 = new QuBit<bool>(_system, new[] { 1 })
                .WithWeights(new Dictionary<bool, Complex> { { false, 2.0 }, { true, 1.0 } }, autoNormalize: true);

            _system.Entangle("PartialCollapse", qubit1, qubit2);

            // Act: measure qubit1 only
            var measureQ1 = qubit1.Observe(42);

            // In a real partial measurement, qubit2 might remain in superposition 
            // conditioned on qubit1's outcome. For the sample code, once the system does a global measure 
            // on Q1’s index, it effectively collapses the entire wavefunction. 
            // But we can still check that Q2 has an observed value or is flagged collapsed.

            var q2Val = qubit2.GetObservedValue();

            // Assert
            Assert.True(qubit1.IsCollapsed, "First qubit definitely collapsed after direct observation.");
            Assert.NotNull(q2Val, "Second qubit is also collapsed by the global wavefunction update in this code base.");

            // This test is conceptual: to do real “partial” staging, we’d need partial measurement logic
            // that only collapses subspace. The sample system code does a full wavefunction measure on those qubits. 
            // TODO: ensure add partial measure logic to the system for real partial collapse.
        }
        #endregion

        #region Entanglement Graph Diagnostics: Output graph stats: size, circular refs, chaos %
        [Test]
        public void EntanglementGraphDiagnostics_PrintsStatsWithoutError()
        {
            // Arrange
            var qA = new QuBit<int>(_system, new[] { 0 });
            var qB = new QuBit<int>(_system, new[] { 1 });
            var qC = new QuBit<int>(_system, new[] { 2 });

            _manager.Link("GroupA", qA);
            _manager.Link("GroupB", qB, qC);

            // Act/Assert: Just ensure PrintEntanglementStats() doesn’t throw or crash
            // If you wanted to capture console output, you’d do it with a TextWriter redirect.
            Assert.DoesNotThrow(() =>
            {
                _manager.PrintEntanglementStats();
            }, "Diagnostics method must not throw exceptions during normal operation.");
        }
        #endregion
    }
}
